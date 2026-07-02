using Carter;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Serilog;
using Shoppy.Business;
using Shoppy.Business.Options;
using Shoppy.DataAccess;
using Shoppy.WebAPI.Handlers;
using Shoppy.WebAPI.MiddleWares;

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

// OPEN API
builder.Services.AddOpenApi();

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
builder.Services.AddAuthorization(conf =>
{
    conf.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();


// Correlation Id Middleware (must be early in pipeline)
app.UseMiddleware<CorrelationMiddleware>();

// serilog request logging
app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.MapOpenApi();

app.UseCors(c => c
                    .AllowAnyMethod()
                    .WithOrigins(
                                "http://localhost:3000",
                                "http://localhost:5176",
                                "http://localhost:5226"
                                )
                    .AllowAnyHeader()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10))
);

app.UseRateLimiter();



app.MapScalarApiReference();

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.Run();
