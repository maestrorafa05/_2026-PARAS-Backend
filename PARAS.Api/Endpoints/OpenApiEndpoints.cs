using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Scalar.AspNetCore;

namespace PARAS.Api.Endpoints;

public static class OpenApiEndpoints
{
    public static IEndpointRouteBuilder MapOpenApiEndpoints(this IEndpointRouteBuilder app)
    {
        // endpoint untuk dokumentasi API
        app.MapOpenApi();
        // endpoint untuk referensi API Scalar
        app.MapScalarApiReference();
        return app;
    }
}