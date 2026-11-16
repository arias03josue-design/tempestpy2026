using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tempest.Infrastructure.Persistence;

namespace Tempest.Infrastructure.Health;

internal sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("SQLite reachable")
            : HealthCheckResult.Unhealthy("SQLite unreachable");
}
