using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PublicSafetyLab.Infrastructure.Persistence;

namespace PublicSafetyLab.Infrastructure.HealthChecks;

public sealed class PostgreSqlHealthCheck(PublicSafetyDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connected = await dbContext.Database.CanConnectAsync(cancellationToken);
            return connected
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("PostgreSQL connection check failed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to reach PostgreSQL.", ex);
        }
    }
}
