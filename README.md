# EmployeeApi - .NET 8 Web API for Azure Kubernetes Service (AKS)

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0%20(LTS)-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-AKS-326CE5?logo=kubernetes)](https://azure.microsoft.com/en-us/products/kubernetes-service)
[![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?logo=docker)](https://www.docker.com/)
[![OpenAPI](https://img.shields.io/badge/OpenAPI-Swagger-85EA2D?logo=swagger)](https://swagger.io/)

A high-performance, containerized .NET 8 Minimal Web API architected specifically for cloud-native deployment to **Azure Kubernetes Service (AKS)**.

This solution demonstrates enterprise patterns including **Kubernetes Downward API metadata injection**, **Liveness and Readiness health probes**, **thread-safe in-memory state management**, **structured diagnostics logging**, and **multi-replica round-robin load distribution**.

---

## Architecture Overview

```
                           Azure Public Load Balancer / Ingress
                                            │
                                            ▼
                               [ employee-api-service ]
                               (Target Port: 8080 TCP)
                                            │
                    ┌───────────────────────┼───────────────────────┐
                    │ (Round-Robin)         │ (Round-Robin)         │ (Round-Robin)
                    ▼                       ▼                       ▼
            ┌───────────────┐       ┌───────────────┐       ┌───────────────┐
            │     Pod 1     │       │     Pod 2     │       │     Pod 3     │
            │ employee-api- │       │ employee-api- │       │ employee-api- │
            │ 57f6c-pod-a1  │       │ 57f6c-pod-b2  │       │ 57f6c-pod-c3  │
            ├───────────────┤       ├───────────────┤       ├───────────────┤
            │  Kestrel:8080 │       │  Kestrel:8080 │       │  Kestrel:8080 │
            │  Non-root:app │       │  Non-root:app │       │  Non-root:app │
            │  /health (Live│       │  /health (Live│       │  /health (Live│
            │  /ready (Ready│       │  /ready (Ready│       │  /ready (Ready│
            │  /api/info    │       │  /api/info    │       │  /api/info    │
            └───────────────┘       └───────────────┘       └───────────────┘
```

### Key Architectural Highlights
1. **.NET 8 Minimal API Architecture**: Lightweight memory footprint (~35MB working set per pod), high throughput, low latency.
2. **Kubernetes Downward API Integration**: Pod and Node names are dynamically injected from Kubernetes metadata into container environment variables (`POD_NAME`, `NODE_NAME`).
3. **Dual Health Probes**:
   - `/health` (Liveness): Validates that the application runtime is alive and responsive.
   - `/ready` (Readiness): Validates that internal storage and dependencies are initialized before receiving customer traffic.
4. **Hardened Container Configuration**: Uses `.NET 8` native non-root user (`app`, UID 1654) and default container port `8080`.
5. **Thread-Safe In-Memory Storage**: Uses `ConcurrentDictionary<int, Employee>` with atomic `Interlocked.Increment` for lock-free multi-request execution.
6. **RFC 9110 / RFC 7807 Compliance**: Comprehensive validation using `IValidatableObject` and `HttpValidationProblemDetails`.

---

## Solution Structure

```
SampleKubernetes/
├── src/
│   └── EmployeeApi/
│       ├── Endpoints/
│       │   ├── EmployeeEndpoints.cs         # Minimal API CRUD endpoints & model validation
│       │   └── InfoEndpoints.cs             # GET /api/info Kubernetes pod diagnostic endpoint
│       ├── HealthChecks/
│       │   ├── HealthCheckResponseWriter.cs # Formats health reports to RFC-compliant JSON
│       │   └── ReadinessHealthCheck.cs      # Validates repository availability
│       ├── Middleware/
│       │   └── RequestLoggingMiddleware.cs  # Structured ILogger middleware capturing Pod context
│       ├── Models/
│       │   ├── Employee.cs                  # Core Employee domain model
│       │   ├── CreateEmployeeRequest.cs     # Input DTO with validation attributes & logic
│       │   ├── UpdateEmployeeRequest.cs     # Update DTO with validation attributes & logic
│       │   └── PodInfoResponse.cs           # Diagnostic payload for /api/info
│       ├── Repositories/
│       │   ├── IEmployeeRepository.cs       # Asynchronous repository interface
│       │   └── InMemoryEmployeeRepository.cs# Thread-safe ConcurrentDictionary implementation
│       ├── Services/
│       │   ├── IKubernetesInfoService.cs    # Diagnostic service abstraction
│       │   └── KubernetesInfoService.cs     # Resolves PodName, NodeName, and environment
│       ├── Properties/
│       │   └── launchSettings.json          # Development launch configurations
│       ├── appsettings.json                 # Production configuration & logging format
│       ├── appsettings.Development.json     # Local development configuration
│       ├── Dockerfile                       # Multi-stage container build definition
│       ├── .dockerignore                    # Build artifact exclusions
│       ├── EmployeeApi.csproj               # Target net8.0 with OpenAPI dependencies
│       ├── EmployeeApi.http                 # Executable HTTP test suite for VS / VS Code
│       └── Program.cs                       # Application bootstrapping & middleware pipeline
├── k8s/
│   ├── deployment.yaml                      # 3 replicas, resource limits, probes & Downward API
│   ├── service.yaml                         # Azure LoadBalancer exposing port 80 -> 8080
│   ├── hpa.yaml                             # Horizontal Pod Autoscaler (2 to 10 pods)
│   └── kustomization.yaml                   # Kustomize deployment manifest
├── Dockerfile                               # Root multi-stage container build definition
├── .dockerignore                            # Root Docker exclusions
├── EmployeeApi.sln                          # Visual Studio solution file
└── README.md                                # Comprehensive architectural documentation
```

---

## API Specifications

| Method | Endpoint | Description | Status Codes |
|---|---|---|---|
| `GET` | `/health` | Kubernetes Liveness Probe | `200 OK`, `503 Service Unavailable` |
| `GET` | `/ready` | Kubernetes Readiness Probe | `200 OK`, `503 Service Unavailable` |
| `GET` | `/api/info` | AKS Pod Identification Learning Endpoint | `200 OK` |
| `GET` | `/api/employees` | Retrieve all employees | `200 OK` |
| `GET` | `/api/employees/{id}` | Retrieve employee by numeric ID | `200 OK`, `404 Not Found` |
| `POST` | `/api/employees` | Create a new employee | `201 Created`, `400 Bad Request` |
| `PUT` | `/api/employees/{id}` | Update existing employee | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `DELETE` | `/api/employees/{id}` | Delete employee by ID | `204 No Content`, `404 Not Found` |
| `GET` | `/swagger` | Interactive Swagger OpenAPI UI | `200 OK` |

---

## Quick Start: Local Execution

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. Clone & Build
```powershell
# Restore dependencies and build
dotnet build
```

### 2. Run the API Locally
```powershell
# Run with local port 8080 and simulated pod environment variables
dotnet run --project src/EmployeeApi --launch-profile http-8080
```

The service starts on `http://localhost:8080` with Swagger UI available at `http://localhost:8080/swagger`.

---

## Docker Containerization

The project uses a production-hardened, multi-stage `Dockerfile`:
- Stage 1: Builds and compiles binaries with `mcr.microsoft.com/dotnet/sdk:8.0`.
- Stage 2: Runs the final binary using `mcr.microsoft.com/dotnet/aspnet:8.0`.
- Executes under the unprivileged `USER $APP_UID` (UID 1654).
- Default listening port: `8080` (`ASPNETCORE_HTTP_PORTS=8080`).

### Build & Run Container Locally
```bash
# Build the Docker image
docker build -t employee-api:1.0.0 .

# Run container mapping port 8080 to host
docker run -d -p 8080:8080 \
  -e POD_NAME="employee-api-local-container-01" \
  -e NODE_NAME="docker-desktop-node" \
  --name employee-api \
  employee-api:1.0.0

# Verify container logs
docker logs employee-api
```

---

## Deployment to Azure Kubernetes Service (AKS)

Follow these step-by-step instructions to deploy `EmployeeApi` to AKS.

### Step 1: Define Azure Environment Variables
```bash
export RESOURCE_GROUP="rg-employee-api-prod"
export LOCATION="eastus"
export ACR_NAME="acremployeeapi$RANDOM"
export AKS_CLUSTER_NAME="aks-employee-cluster"
```
*(On Windows PowerShell, use `$env:RESOURCE_GROUP = "rg-employee-api-prod"`, etc.)*

### Step 2: Create Resource Group and Azure Container Registry (ACR)
```bash
# Create Resource Group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Azure Container Registry with Standard SKU
az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME --sku Standard
```

### Step 3: Create AKS Cluster with ACR Integration
```bash
# Create AKS cluster attached to ACR (avoids manual image pull secrets)
az aks create \
  --resource-group $RESOURCE_GROUP \
  --name $AKS_CLUSTER_NAME \
  --node-count 3 \
  --node-vm-size Standard_D2s_v5 \
  --attach-acr $ACR_NAME \
  --enable-managed-identity \
  --generate-ssh-keys

# Download cluster credentials to your local kubeconfig
az aks get-credentials --resource-group $RESOURCE_GROUP --name $AKS_CLUSTER_NAME
```

### Step 4: Build and Push Image using ACR Cloud Build
You can build directly in Azure without requiring local Docker:
```bash
# Submit build task to Azure Container Registry
az acr build --registry $ACR_NAME --image employeeapi:1.0.0 .
```

### Step 5: Update Manifests and Deploy to AKS
Update the image reference in `k8s/deployment.yaml` with your ACR login server:
```bash
# Get ACR login server
ACR_LOGIN_SERVER=$(az acr show --name $ACR_NAME --query loginServer --output tsv)

# Apply Kubernetes manifests
kubectl apply -k ./k8s
```
*(Or set the image dynamically: `kubectl set image deployment/employee-api employee-api=$ACR_LOGIN_SERVER/employeeapi:1.0.0`)*

### Step 6: Verify Deployment and Obtain Public IP
```bash
# Check Pod status (verify all 3 replicas are Running and Ready)
kubectl get pods -l app=employee-api -o wide

# Check Service status to obtain external LoadBalancer IP
kubectl get svc employee-api-service -w
```

Example output:
```
NAME                   TYPE           CLUSTER-IP     EXTERNAL-IP      PORT(S)        AGE
employee-api-service   LoadBalancer   10.0.124.52    20.84.152.100    80:31245/TCP   90s
```

---

## Kubernetes Learning Demonstration: Multi-Replica Pod Identification

When `EmployeeApi` is deployed with 3 replicas on AKS, the Azure Load Balancer routes incoming HTTP requests across all 3 running pods.

By calling `GET /api/info`, you can inspect which pod handled your request in real time.

### Response Schema:
```json
{
  "podName": "employee-api-57f6c7c7db-abc12",
  "machineName": "aks-nodepool1-34284729-vmss000000",
  "environment": "Production",
  "time": "2026-10-22T10:00:00.1234567Z",
  "version": "1.0.0"
}
```

### How the Pod Name is Injected
In `k8s/deployment.yaml`, the Kubernetes **Downward API** injects metadata:
```yaml
env:
  - name: POD_NAME
    valueFrom:
      fieldRef:
        fieldPath: metadata.name
  - name: NODE_NAME
    valueFrom:
      fieldRef:
        fieldPath: spec.nodeName
```
`KubernetesInfoService.cs` reads `Environment.GetEnvironmentVariable("POD_NAME")`.

### Observing Traffic Distribution Across Replicas

Execute 10 consecutive requests to see different pod replicas answering:

#### Bash / Linux / macOS:
```bash
EXTERNAL_IP="20.84.152.100"  # Replace with your external IP

for i in {1..10}; do
  curl -s http://$EXTERNAL_IP/api/info | jq -r '"Request #'"$i"' -> Pod: " + .podName + " on Node: " + .machineName'
done
```

#### Windows PowerShell:
```powershell
$ExternalIp = "20.84.152.100"  # Replace with your external IP

1..10 | ForEach-Object {
    $info = Invoke-RestMethod -Uri "http://$ExternalIp/api/info"
    Write-Host ("Request #{0} -> Pod: {1} on Node: {2}" -f $_, $info.podName, $info.machineName) -ForegroundColor Cyan
}
```

#### Sample Output:
```text
Request #1  -> Pod: employee-api-6878b6689d-7g4xk on Node: aks-nodepool1-12345678-vmss000000
Request #2  -> Pod: employee-api-6878b6689d-m4d2p on Node: aks-nodepool1-12345678-vmss000001
Request #3  -> Pod: employee-api-6878b6689d-v9x8z on Node: aks-nodepool1-12345678-vmss000002
Request #4  -> Pod: employee-api-6878b6689d-7g4xk on Node: aks-nodepool1-12345678-vmss000000
Request #5  -> Pod: employee-api-6878b6689d-m4d2p on Node: aks-nodepool1-12345678-vmss000001
Request #6  -> Pod: employee-api-6878b6689d-v9x8z on Node: aks-nodepool1-12345678-vmss000002
Request #7  -> Pod: employee-api-6878b6689d-7g4xk on Node: aks-nodepool1-12345678-vmss000000
Request #8  -> Pod: employee-api-6878b6689d-m4d2p on Node: aks-nodepool1-12345678-vmss000001
Request #9  -> Pod: employee-api-6878b6689d-v9x8z on Node: aks-nodepool1-12345678-vmss000002
Request #10 -> Pod: employee-api-6878b6689d-7g4xk on Node: aks-nodepool1-12345678-vmss000000
```

---

## Complete cURL Command Reference

Replace `http://localhost:8080` with `http://<YOUR_AKS_EXTERNAL_IP>` when testing against AKS.

### 1. Kubernetes Learning Endpoint
```bash
curl -i -X GET http://localhost:8080/api/info \
  -H "Accept: application/json"
```

### 2. Kubernetes Liveness Probe
```bash
curl -i -X GET http://localhost:8080/health \
  -H "Accept: application/json"
```

### 3. Kubernetes Readiness Probe
```bash
curl -i -X GET http://localhost:8080/ready \
  -H "Accept: application/json"
```

### 4. Get All Employees
```bash
curl -i -X GET http://localhost:8080/api/employees \
  -H "Accept: application/json"
```

### 5. Get Employee by ID
```bash
curl -i -X GET http://localhost:8080/api/employees/1 \
  -H "Accept: application/json"
```

### 6. Create New Employee
```bash
curl -i -X POST http://localhost:8080/api/employees \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sarah Connor",
    "email": "sarah.connor@contoso.com",
    "department": "Security Operations"
  }'
```

### 7. Update Employee
```bash
curl -i -X PUT http://localhost:8080/api/employees/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Alice Johnson",
    "email": "alice.j@contoso.com",
    "department": "Principal Cloud Architect"
  }'
```

### 8. Delete Employee
```bash
curl -i -X DELETE http://localhost:8080/api/employees/4
```

### 9. Validation Test (Invalid Request)
```bash
curl -i -X POST http://localhost:8080/api/employees \
  -H "Content-Type: application/json" \
  -d '{
    "name": "",
    "email": "invalid-email-format",
    "department": ""
  }'
```

---

## Senior Solution Architect: AKS Deployment Considerations

When promoting this solution to production enterprise environments, the following architectural guidelines must be applied:

### 1. Zero-Downtime Rolling Updates
In `k8s/deployment.yaml`, the rolling update strategy is configured as:
```yaml
strategy:
  type: RollingUpdate
  rollingUpdate:
    maxSurge: 1
    maxUnavailable: 0
```
- `maxSurge: 1`: Kubernetes spins up 1 new pod before tearing down old pods.
- `maxUnavailable: 0`: No pod is taken offline until the replacement pod passes its `/ready` readiness probe.

### 2. Probe Optimization
- **Liveness Probe (`/health`)**: Checked every 10 seconds. Should only fail if the thread pool or internal runtime is completely unresponsive. Avoid checking external databases in the liveness probe to prevent cascade restarts.
- **Readiness Probe (`/ready`)**: Checked every 5 seconds. If in-memory state or downstream databases become unavailable, the pod is temporarily removed from service endpoints without being restarted.

### 3. Production State Persistence
In this architecture, storage is in-memory for educational, containerization, and prototyping purposes. In a multi-pod production environment:
- Data created in Pod A is not automatically replicated to Pod B unless backed by an external distributed database (such as **Azure Cosmos DB**, **Azure Database for PostgreSQL**, or **Azure SQL Database**).
- For distributed state, integrate **Entity Framework Core** with managed identities.

### 4. Pod Disruption Budgets (PDB)
Prevent voluntary disruptions (node maintenance, cluster upgrades) from taking down all replicas simultaneously:
```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: employee-api-pdb
spec:
  minAvailable: 2
  selector:
    matchLabels:
      app: employee-api
```

### 5. Ingress & TLS Termination
While the demo uses a `LoadBalancer` service, production workloads should sit behind:
- **Azure Application Gateway Ingress Controller (AGIC)** or **AKS Web App Routing** with Let's Encrypt / Azure Key Vault TLS certificates.
- **Azure Front Door** with Web Application Firewall (WAF) for DDoS protection and global routing.

### 6. Workload Identity & Secret Management
- Replace hardcoded credentials with **Azure AD Workload Identity** (federated token exchange).
- Store secrets in **Azure Key Vault** and project them directly into containers via the **Secrets Store CSI Driver**.

### 7. Structured Observability
- All logs emitted by `RequestLoggingMiddleware.cs` are captured directly by **Azure Monitor Container Insights** via `stdout`/`stderr`.
- Logs include `TraceId`, `PodName`, HTTP status codes, and execution latency for tracing queries in Log Analytics workspaces.

