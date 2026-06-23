using Shoppy.Business;
using Shoppy.DataAccess;

var builder = WebApplication.CreateBuilder(args);


// register services
builder.Services.AddDataAccess(builder.Configuration).AddBusiness();


var app = builder.Build();



app.MapGet("/", () => "Hello World!");



app.Run();
