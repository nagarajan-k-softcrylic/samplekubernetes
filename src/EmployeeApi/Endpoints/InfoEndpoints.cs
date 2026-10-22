using EmployeeApi.Models;
using EmployeeApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EmployeeApi.Endpoints;

public static class InfoEndpoints
{
    public static RouteGroupBuilder MapInfoEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/info")
            .WithTags("Diagnostics & Kubernetes");

        group.MapGet("/", GetPodInfo)
            .WithName("GetPodInfo")
            .WithSummary("Get Pod & AKS diagnostic metadata")
            .WithDescription("Returns Pod name, Machine/Node name, Environment, Server Time, and API Version to observe request distribution across AKS replicas.")
            .Produces<PodInfoResponse>(StatusCodes.Status200OK);

        return group;
    }

    private static Ok<PodInfoResponse> GetPodInfo(IKubernetesInfoService infoService)
    {
        var info = infoService.GetPodInfo();
        return TypedResults.Ok(info);
    }
}

