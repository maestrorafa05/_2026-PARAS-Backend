using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Scalar.AspNetCore;
using PARAS.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using PARAS.Api.Data;
using PARAS.Api.Options;
using PARAS.Api.Services;
using PARAS.Api.Auth;
using PARAS.Api.Services.Auth;
using PARAS.Api.Data.Seeding;


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

// data protection untuk enkripsi data sensitif 
builder.Services.AddDataProtection();

// Identity Core (tanpa cookie)
builder.Services.AddIdentityCore<AppUser>(options =>
{   
    options.User.RequireUniqueEmail = false; 
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<AppRole>()
.AddEntityFrameworkStores<ParasDbContext>()
.AddDefaultTokenProviders()
.AddSignInManager();


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

// konfigurasi options untuk JWT
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt")
);

// konfigurasi JWT authentication
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("Jwt config is missing. Use user-secrets: Jwt:Issuer, Jwt:Audience, Jwt:Key");
if (string.IsNullOrWhiteSpace(jwt.Issuer) ||
    string.IsNullOrWhiteSpace(jwt.Audience) ||
    string.IsNullOrWhiteSpace(jwt.Key))
{
    throw new InvalidOperationException("Jwt config is incomplete. Ensure Jwt:Issuer, Jwt:Audience, and Jwt:Key are set.");
}
if (Encoding.UTF8.GetByteCount(jwt.Key) < 16)
{
    throw new InvalidOperationException("Jwt:Key must be at least 16 bytes for HS256.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // dev only
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) // toleransi kecil
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

builder.Services.AddScoped<JwtTokenService>();


builder.Services.Configure<AdminSeedOptions>(
    builder.Configuration.GetSection("SeedAdmin")
);

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

// middleware untuk autentikasi dan otorisasi
app.UseAuthentication();
app.UseAuthorization();

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

if (app.Environment.IsDevelopment())
{
    await AppDataSeeder.SeedAsync(app.Services);
    await DbSeeder.SeedAsync(app.Services);
}

// mapping endpoint untuk setiap fitur
app.MapSystemEndpoints();
app.MapRoomEndpoints();
app.MapLoanEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapRoomAvailabilityEndpoints();


app.Run();
