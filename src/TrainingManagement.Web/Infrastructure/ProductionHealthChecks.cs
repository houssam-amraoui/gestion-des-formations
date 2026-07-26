using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TrainingManagement.Infrastructure.Certificates;
using TrainingManagement.Infrastructure.Persistence;

namespace TrainingManagement.Web.Infrastructure;

public sealed class DatabaseReadinessHealthCheck(
    IServiceScopeFactory scopes) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }
        catch { return HealthCheckResult.Unhealthy(); }
    }
}

public sealed class WritablePathHealthCheck(
    IWebHostEnvironment environment,
    IOptions<CertificateStorageOptions> certificates,
    IOptions<DataProtectionStorageOptions> dataProtection) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Verify(certificates.Value.BasePath);
            Verify(dataProtection.Value.KeysPath);
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch { return Task.FromResult(HealthCheckResult.Unhealthy()); }
    }

    private void Verify(string configured)
    {
        var path = Path.GetFullPath(Path.IsPathRooted(configured)
            ? configured : Path.Combine(environment.ContentRootPath, configured));
        Directory.CreateDirectory(path);
        var probe = Path.Combine(path, $".health-{Guid.NewGuid():N}");
        using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                   1, FileOptions.DeleteOnClose)) { }
    }
}
