using Carter;
using Shoppy.Business;
using Shoppy.DataAccess;

var builder = WebApplication.CreateBuilder(args);


// register services
builder.Services.AddDataAccess(builder.Configuration).AddBusiness();


// Carter
builder.Services.AddCarter();

var app = builder.Build();


app.MapCarter();

app.Run();
