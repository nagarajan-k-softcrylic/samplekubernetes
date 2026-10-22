using EmployeeApi.Endpoints;
using EmployeeApi.HealthChecks;
using EmployeeApi.Middleware;
using EmployeeApi.Repositories;
using EmployeeApi.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Employee Management API (AKS Ready)",
        Version = "v1",
        Description = "Enterprise .NET 8 Minimal API microservice containerized for Azure Kubernetes Service (AKS). Includes Kubernetes health probes and pod identification learning endpoints.",
        Contact = new OpenApiContact
        {
            Name = "Cloud Architecture Team",
            Email = "architecture@contoso.com"
        }
    });
});

builder.Services.AddProblemDetails();

// In-Memory Data Storage (Thread-safe Singleton across requests)
builder.Services.AddSingleton<IEmployeeRepository, InMemoryEmployeeRepository>();

// Kubernetes Diagnostic Info Service
builder.Services.AddSingleton<IKubernetesInfoService, KubernetesInfoService>();

// Health Checks for Kubernetes Liveness and Readiness Probes
builder.Services.AddHealthChecks()
    .AddCheck("liveness_self", () => HealthCheckResult.Healthy("Liveness probe passed: process is alive and responsive."), tags: new[] { "live" })
    .AddCheck<ReadinessHealthCheck>("readiness_repository", tags: new[] { "ready" });

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();

// Request logging middleware for structured audit and diagnostics
app.UseRequestLogging();

// Enable Swagger UI (both in Development and in AKS test/learning environments)
var enableSwaggerInProd = app.Configuration.GetValue<bool>("EnableSwaggerInProduction", true);
if (app.Environment.IsDevelopment() || enableSwaggerInProd)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Management API v1");
        c.RoutePrefix = "swagger";
    });
}

// Kubernetes Health Checks
// Liveness Probe (/health)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).WithTags("Health & Diagnostics");

// Readiness Probe (/ready)
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).WithTags("Health & Diagnostics");

// Map Domain & Learning Endpoints
app.MapEmployeeEndpoints();
app.MapInfoEndpoints();

// Root redirect to Swagger for developer convenience
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

// Application startup structured logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var infoService = app.Services.GetRequiredService<IKubernetesInfoService>();
var podInfo = infoService.GetPodInfo();

logger.LogInformation("================================================================================");
logger.LogInformation("EmployeeApi Service Initialized Successfully");
logger.LogInformation("Runtime Framework : .NET 8.0 (LTS)");
logger.LogInformation("Environment       : {Environment}", podInfo.Environment);
logger.LogInformation("Pod Name          : {PodName}", podInfo.PodName);
logger.LogInformation("Machine/Node Name : {MachineName}", podInfo.MachineName);
logger.LogInformation("Service Version   : {Version}", podInfo.Version);
logger.LogInformation("Liveness Probe    : GET /health");
logger.LogInformation("Readiness Probe   : GET /ready");
logger.LogInformation("AKS Learning API  : GET /api/info");
logger.LogInformation("CRUD API Base     : GET/POST /api/employees");
logger.LogInformation("================================================================================");

app.Run();
