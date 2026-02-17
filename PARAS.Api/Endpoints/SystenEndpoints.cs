using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PARAS.Api.Data;

namespace PARAS.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        // endpoint root untuk memeriksa status layanan
        app.MapGet("/", () => Results.Ok(new {service = "PARAS.Api", status = "up"})).WithTags("System");
        // endpoint health check untuk memantau kesehatan aplikasi
        app.MapHealthChecks("/health").WithTags("System");
        // endpoint untuk memeriksa koneksi ke database
        app.MapGet("/db-ping", async (ParasDbContext db) =>
        {
            var canConnect = await db.Database.CanConnectAsync();
            return Results.Ok(new {canConnect});
        }).WithTags("System");

        // endpoint untuk cek siapa user yang sedang login
        app.MapGet("/system/whoami", (HttpContext ctx) =>
        {
            var user = ctx.User;

            return Results.Ok(new
            {
                isAuthenticated = user.Identity?.IsAuthenticated ?? false,
                name = user.Identity?.Name,
                claims = user.Claims.Select(c => new { c.Type, c.Value })
            });
        })
        .WithTags("System")
        .RequireAuthorization();

        return app;
    }
}
