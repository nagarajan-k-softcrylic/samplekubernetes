namespace EmployeeApi.Models;

/// <summary>
/// Response payload for Kubernetes diagnostic and learning endpoint (/api/info).
/// Displays Pod and Node identification to observe traffic distribution across replicas in AKS.
/// </summary>
public sealed record PodInfoResponse(
    string PodName,
    string MachineName,
    string Environment,
    string Time,
    string Version
);

