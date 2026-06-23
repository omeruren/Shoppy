using Carter;
using Scalar.AspNetCore;
using Shoppy.Business;
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

var app = builder.Build();

app.MapOpenApi();

app.MapScalarApiReference();

app.MapCarter();

app.Run();
