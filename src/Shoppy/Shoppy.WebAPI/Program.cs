using Asp.Versioning;
using Carter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Shoppy.Business;
using Shoppy.Business.Options;
using Shoppy.Business.Permissions;
using Shoppy.DataAccess;
using Shoppy.WebAPI.Handlers;
using Shoppy.WebAPI.MiddleWares;
using Shoppy.WebAPI.Seed;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);


// SERILOG
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

// register services
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataAccess(builder.Configuration).AddBusiness();


// Carter
builder.Services.AddCarter();


// API VERSIONING
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("api-version"));
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});


// OPEN API
builder.Services.AddOpenApi("v1");

// CUSTOM EXCEPTION HANDLER
builder.Services.AddExceptionHandler<ExceptionHandler>().AddProblemDetails();


// CORS Policy
builder.Services.AddCors();

// RESPONSE COMPRESSION
builder.Services.AddResponseCompression(x => x.EnableForHttps = true);

// RATE Limiter
builder.Services.AddRateLimiter(x =>
{
    x.AddFixedWindowLimiter("fixed", cfr =>
    {
        cfr.PermitLimit = 50;
        cfr.Window = TimeSpan.FromSeconds(5);
        cfr.QueueLimit = 50;
        cfr.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddRateLimiter(x =>
{
    x.AddFixedWindowLimiter("auth-fixed", cfr =>
    {
        cfr.PermitLimit = 5;
        cfr.Window = TimeSpan.FromSeconds(1);
        cfr.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
    });
});

// Jwt Options

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// Email Options
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
// Jwt Options setup

builder.Services.ConfigureOptions<JwtOptionsSetup>();

// Authentication
builder.Services.AddAuthentication().AddJwtBearer();

// Authorization
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(conf =>
{
    conf.AddPolicy("Admin", policy => policy.RequireRole("Admin"));

    foreach (var permission in Permissions.GetAll())
        conf.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
});

// OpenTelemetry Configuration
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Shoppy.WebAPI"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// Health Checks
builder.Services.AddHealthChecks().AddSqlServer(
    builder.Configuration.GetConnectionString("SqlServer")!,
    name: "sqlserver",
    tags: ["db", "sql"]);

var app = builder.Build();

// Seed built-in Admin/Customer roles + their permissions
await RolePermissionSeeder.SeedAsync(app.Services);


// Correlation Id Middleware (must be early in pipeline)
app.UseMiddleware<CorrelationMiddleware>();

// serilog request logging
app.UseSerilogRequestLogging();



app.MapOpenApi();

app.UseCors(c => c
                    .AllowAnyMethod()
                    .WithOrigins(
                                "http://localhost:3000",
                                "http://localhost:5176",
                                "http://localhost:5226"
                                )
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10))
);


app.MapScalarApiReference();

app.UseResponseCompression();

app.UseExceptionHandler();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();


// Health Check Endpoint
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds + "ms"
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds + "ms"
        };
        await context.Response.WriteAsJsonAsync(result, new JsonSerializerOptions { WriteIndented = true });
    }
});

app.MapCarter();

app.Run();
public partial class Program { }
