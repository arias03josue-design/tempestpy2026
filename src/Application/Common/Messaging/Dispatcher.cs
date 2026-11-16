using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Tempest.Application.Common.Security;

namespace Tempest.Application.Common.Messaging;

/// <summary>
/// Lightweight in-process mediator (no third-party dependency).
/// <para>
/// Every request runs inside its <b>own DI scope</b>, so each command/query gets a fresh DbContext.
/// This is essential in Blazor Server, where the circuit scope can live for hours: a shared
/// DbContext would be used concurrently by parallel events and would keep stale tracked entities.
/// </para>
/// </summary>
internal sealed class Dispatcher(IServiceScopeFactory scopeFactory, ICurrentUserProvider currentUserProvider) : ISender
{
    private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The caller identity lives in the caller's scope (circuit or HTTP request): capture it first.
        var currentUser = await currentUserProvider.GetCurrentUserAsync(cancellationToken);

        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CurrentUserAccessor>().User = currentUser;

        var wrapper = (RequestHandlerWrapper<TResponse>)Wrappers.GetOrAdd(
            request.GetType(),
            static (requestType, responseType) => Activator.CreateInstance(
                typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType))!,
            typeof(TResponse));

        return await wrapper.Handle(request, scope.ServiceProvider, cancellationToken);
    }

    private abstract class RequestHandlerWrapper<TResponse>
    {
        public abstract Task<TResponse> Handle(
            IRequest<TResponse> request,
            IServiceProvider services,
            CancellationToken cancellationToken);
    }

    private sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(
            IRequest<TResponse> request,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;
            var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
            var behaviors = services.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

            RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(typedRequest, cancellationToken);

            // Wrap from the inside out so the first registered behavior is the outermost one.
            for (var i = behaviors.Length - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var next = pipeline;
                pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
            }

            return pipeline();
        }
    }
}
