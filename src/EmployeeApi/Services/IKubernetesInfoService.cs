using EmployeeApi.Models;

namespace EmployeeApi.Services;

/// <summary>
/// Service to query runtime environment and Kubernetes pod metadata.
/// </summary>
public interface IKubernetesInfoService
{
    /// <summary>
    /// Gets pod and host diagnostic information.
    /// </summary>
    PodInfoResponse GetPodInfo();
}

