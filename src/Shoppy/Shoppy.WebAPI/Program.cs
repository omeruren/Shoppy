using Carter;
using Scalar.AspNetCore;
using Shoppy.Business;
using Shoppy.Business.Options;
using Shoppy.DataAccess;
using Shoppy.WebAPI.Handlers;

var builder = WebApplication.CreateBuilder(args);


// register services
builder.Services.AddDataAccess(builder.Configuration).AddBusiness();


// Carter
builder.Services.AddCarter();

// OPEN API
builder.Services.AddOpenApi();

// CUSTOM EXCEPTION HANDLER
builder.Services.AddExceptionHandler<ExceptionHandler>().AddProblemDetails();


// Jwt Options

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// Jwt Options setup

builder.Services.ConfigureOptions<JwtOptionsSetup>();

// Authentication
builder.Services.AddAuthentication().AddJwtBearer();

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi();

app.MapScalarApiReference();

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.Run();
