using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        // Migrate against a standalone context BEFORE the host starts: accessing the
        // WebApplicationFactory's Services property boots the full Program.cs pipeline,
        // which runs RolePermissionSeeder against the database on startup — that seeder
        // needs the schema to already exist, so migration can't happen through Services.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        await using var migrationContext = new ApplicationDbContext(options, new HttpContextAccessor());
        await migrationContext.Database.MigrateAsync();
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