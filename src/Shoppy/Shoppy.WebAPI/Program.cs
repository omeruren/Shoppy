using Carter;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Shoppy.Business;
using Shoppy.Business.Options;
using Shoppy.DataAccess;
using Shoppy.WebAPI.Handlers;

var builder = WebApplication.CreateBuilder(args);


// register services
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataAccess(builder.Configuration).AddBusiness();


// Carter
builder.Services.AddCarter();

// OPEN API
builder.Services.AddOpenApi();

// CUSTOM EXCEPTION HANDLER
builder.Services.AddExceptionHandler<ExceptionHandler>().AddProblemDetails();


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

app.UseExceptionHandler();

app.UseRateLimiter();

app.MapOpenApi();

app.MapScalarApiReference();

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.Run();
