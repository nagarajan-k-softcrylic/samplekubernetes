using System.Reflection;
using EmployeeApi.Models;

namespace EmployeeApi.Services;

/// <summary>
/// Extracts pod, node, and runtime diagnostic metadata from the hosting environment.
/// Specifically configured to read Kubernetes Downward API environment variables in AKS.
/// </summary>
public sealed class KubernetesInfoService : IKubernetesInfoService
{
    private readonly IHostEnvironment _environment;
    private readonly string _version;

    public KubernetesInfoService(IHostEnvironment environment)
    {
        _environment = environment;

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        _version = !string.IsNullOrWhiteSpace(informationalVersion)
            ? informationalVersion.Split('+')[0] // Strip git commit hash if present
            : assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    }

    public PodInfoResponse GetPodInfo()
    {
        // 1. Read pod name from Kubernetes Downward API environment variable (POD_NAME).
        // In containerized Linux environments (like AKS), HOSTNAME is also populated with the Pod name by default.
        var podName = Environment.GetEnvironmentVariable("POD_NAME")
            ?? Environment.GetEnvironmentVariable("HOSTNAME")
            ?? Environment.MachineName;

        // 2. Read Kubernetes node name (e.g. aks-nodepool1-...) injected via Downward API (NODE_NAME).
        // Fallback to MachineName if not executing on Kubernetes.
        var machineName = Environment.GetEnvironmentVariable("NODE_NAME")
            ?? Environment.MachineName;

        // 3. Current hosting environment (e.g., Development, Staging, Production)
        var environmentName = _environment.EnvironmentName;

        // 4. Current UTC ISO 8601 timestamp
        var currentTime = DateTime.UtcNow.ToString("o");

        return new PodInfoResponse(
            PodName: podName,
            MachineName: machineName,
            Environment: environmentName,
            Time: currentTime,
            Version: _version
        );
    }
}

