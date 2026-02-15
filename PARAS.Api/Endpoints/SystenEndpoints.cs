using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PARAS.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        // endpoint root untuk memeriksa status layanan
        app.MapGet("/", () => Results.Ok(new {service = "PARAS.Api", status = "up"})).WithTags("System");
        // endpoint health check untuk memantau kesehatan aplikasi
        app.MapHealthChecks("/health").WithTags("System");
        return app;
    }
}
