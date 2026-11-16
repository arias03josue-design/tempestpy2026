using Microsoft.Extensions.Logging;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;
using Tempest.Domain.Common;

namespace Tempest.Application.Common.Behaviors;

/// <summary>
/// Outermost behavior: turns domain rule violations into validation results and unexpected
/// exceptions into a generic error, so a failing use case never tears down the Blazor circuit.
/// </summary>
internal sealed class ExceptionHandlingBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResultFactory<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Domain rule {Code} rejected {Request}: {Message}", ex.Code, typeof(TRequest).Name, ex.Message);
            return TResponse.FromError(new Error(ex.Code, ex.Message, ErrorType.Validation));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while handling {Request}", typeof(TRequest).Name);
            return TResponse.FromError(Error.Unexpected);
        }
    }
}
