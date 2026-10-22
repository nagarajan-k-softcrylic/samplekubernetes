using EmployeeApi.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmployeeApi.HealthChecks;

/// <summary>
/// Health check that verifies the employee data store is initialized and operational before accepting traffic.
/// </summary>
public sealed class ReadinessHealthCheck : IHealthCheck
{
    private readonly IEmployeeRepository _repository;

    public ReadinessHealthCheck(IEmployeeRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var employees = await _repository.GetAllAsync(cancellationToken);
            var data = new Dictionary<string, object>
            {
                { "recordCount", employees.Count },
                { "storageType", "InMemoryConcurrentDictionary" }
            };

            return HealthCheckResult.Healthy("Data repository is operational and ready to serve requests.", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Data repository is not ready.", ex);
        }
    }
}

