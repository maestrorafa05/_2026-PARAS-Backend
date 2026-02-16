using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using PARAS.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using PARAS.Api.Data;
using PARAS.Api.Options;
using PARAS.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// open api untuk dokumentasi API
builder.Services.AddOpenApi();

// problem details untuk error handling
builder.Services.AddProblemDetails();

// cors untuk mengizinkan akses dari frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", PolicyEnforcement =>
        PolicyEnforcement.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// db context untuk akses database
builder.Services.AddDbContext<ParasDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// konfigurasi options untuk aturan peminjaman
builder.Services.Configure<BookingRulesOptions>(
    builder.Configuration.GetSection("BookingRules")
);

builder.Services.AddScoped<LoanRulesValidator>();

// HTTP logging untuk mempermudah debug request dari Scalar/frontend
builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.RequestQuery |
        HttpLoggingFields.RequestHeaders |
        HttpLoggingFields.ResponseStatusCode;
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddDbContextCheck<ParasDbContext>("db");

var app = builder.Build();

// global error handling menggunakan problem details
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var problem = new ProblemDetails
        {
            Title = "Unexpected error",
            Detail = app.Environment.IsDevelopment() ? error?.Message : "An error occured.",
            Status = StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = problem.Status.Value;
        await context.Response.WriteAsJsonAsync(problem);
    });
});

// middleware untuk logging HTTP request/response
app.UseHttpLogging();

// middleware untuk mengaktifkan https, cors, dan dokumentasi API
app.UseHttpsRedirection();
app.UseCors("Frontend");

// hanya tampilkan dokumentasi API di lingkungan development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApiEndpoints();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Name == "self"
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
});

if (app.Environment.IsDevelopment()){
    await DbSeeder.SeedAsync(app.Services);
}

// mapping endpoint untuk setiap fitur
app.MapSystemEndpoints();
app.MapRoomEndpoints();
app.MapLoanEndpoints();
app.MapRoomAvailabilityEndpoints();


app.Run();
