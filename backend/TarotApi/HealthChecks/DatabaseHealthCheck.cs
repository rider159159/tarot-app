using Microsoft.Extensions.Diagnostics.HealthChecks;
using TarotApi.Data;

namespace TarotApi.HealthChecks;

public sealed class DatabaseHealthCheck(TarotDbContext dbContext) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(Timeout);

        try
        {
            return await dbContext.Database.CanConnectAsync(timeoutSource.Token)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
