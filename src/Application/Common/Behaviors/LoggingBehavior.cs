using Microsoft.Extensions.Logging;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;

namespace Tempest.Application.Common.Behaviors;

/// <summary>Logs duration and failures. Only the request <i>name</i> is logged, never its payload (passwords).</summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly TimeSpan SlowThreshold = TimeSpan.FromMilliseconds(500);
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();
        var response = await next();
        var elapsed = timeProvider.GetElapsedTime(startedAt);

        if (response is Result { Error: { } error })
        {
            logger.LogInformation(
                "{Request} failed with {ErrorCode} in {ElapsedMs:0} ms",
                RequestName,
                error.Code,
                elapsed.TotalMilliseconds);
        }
        else if (elapsed > SlowThreshold)
        {
            logger.LogWarning("{Request} is slow: {ElapsedMs:0} ms", RequestName, elapsed.TotalMilliseconds);
        }
        else
        {
            logger.LogDebug("{Request} handled in {ElapsedMs:0} ms", RequestName, elapsed.TotalMilliseconds);
        }

        return response;
    }
}
