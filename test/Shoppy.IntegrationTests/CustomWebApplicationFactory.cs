using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shoppy.DataAccess.Context;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace Shoppy.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sqlContainer.StartAsync(), _redisContainer.StartAsync());

        // run migrations to setup database schema
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:SqlServer",_sqlContainer.GetConnectionString() },
                {"ConnectionStrings:Redis",_redisContainer.GetConnectionString() }
            });
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(_sqlContainer.StopAsync(), _redisContainer.StopAsync());
    }
}