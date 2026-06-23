using Shoppy.DataAccess;

var builder = WebApplication.CreateBuilder(args);


// register services
builder.Services.AddDataAccess(builder.Configuration);


var app = builder.Build();



app.MapGet("/", () => "Hello World!");



app.Run();
