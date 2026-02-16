using Microsoft.AspNetCore.Diagnostics;
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

// health checks untuk memantau kesehatan aplikasi
builder.Services.AddHealthChecks();

// konfigurasi options untuk aturan peminjaman
builder.Services.Configure<BookingRulesOptions>(
    builder.Configuration.GetSection("BookingRules")
);

builder.Services.AddScoped<LoanRulesValidator>();

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

// middleware untuk mengaktifkan https, cors, dan dokumentasi API
app.UseHttpsRedirection();
app.UseCors("Frontend");

// hanya tampilkan dokumentasi API di lingkungan development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApiEndpoints();
}

// mapping endpoint untuk sistem dan ruangan
app.MapSystemEndpoints();
app.MapRoomEndpoints();
app.MapLoanEndpoints();
app.MapRoomAvailabilityEndpoints();

app.Run();
