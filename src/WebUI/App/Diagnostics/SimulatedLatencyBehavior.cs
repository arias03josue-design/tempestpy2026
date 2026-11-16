using Tempest.Application.Common.Messaging;

namespace Tempest.WebUI.App.Diagnostics;

/// <summary>
/// Development aid, registered only when <c>Diagnostics:SimulatedQueryLatencyMs</c> &gt; 0.
/// Local SQLite answers in about a millisecond, so without it the loading skeletons are never visible.
/// </summary>
internal sealed class SimulatedLatencyBehavior<TRequest, TResponse>(IConfiguration configuration)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly bool IsQuery = typeof(TRequest)
        .GetInterfaces()
        .Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IQuery<>));

    private readonly int _delayMs = configuration.GetValue("Diagnostics:SimulatedQueryLatencyMs", 0);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (IsQuery && _delayMs > 0)
        {
            await Task.Delay(_delayMs, cancellationToken);
        }

        return await next();
    }
}
