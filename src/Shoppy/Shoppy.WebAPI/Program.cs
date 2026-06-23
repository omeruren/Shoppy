using Carter;
using Scalar.AspNetCore;
using Shoppy.Business;
using Shoppy.DataAccess;

var builder = WebApplication.CreateBuilder(args);


// register services
builder.Services.AddDataAccess(builder.Configuration).AddBusiness();


// Carter
builder.Services.AddCarter();

// Open Api
builder.Services.AddOpenApi();


var app = builder.Build();

app.MapOpenApi();

app.MapScalarApiReference();

app.MapCarter();

app.Run();
