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
            var settings = new Dictionary<string, string?>
            {
                {"ConnectionStrings:SqlServer",_sqlContainer.GetConnectionString() },
                {"ConnectionStrings:Redis",_redisContainer.GetConnectionString() }
            };

            foreach (var (key, value) in GetAdditionalConfiguration())
                settings[key] = value;

            config.AddInMemoryCollection(settings);
        });
    }

    // Hook for subclasses that need to tweak app configuration beyond the container
    // connection strings (e.g. relaxing rate-limit thresholds for tests that aren't
    // themselves testing rate limiting).
    protected virtual IDictionary<string, string?> GetAdditionalConfiguration() => new Dictionary<string, string?>();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(_sqlContainer.StopAsync(), _redisContainer.StopAsync());
    }
}

// The "auth-fixed" rate-limit policy is a single global, non-partitioned bucket shared
// by every request the app instance handles. Functional auth-flow tests (login/refresh
// rotation/permission checks) each make a handful of real HTTP calls against /api/v1/auth/*
// and would otherwise intermittently collide with that shared bucket. Rate-limiting
// behavior itself is covered separately by RateLimitingIntegrationTests against the
// production-configured CustomWebApplicationFactory.
public sealed class RelaxedAuthRateLimitWebApplicationFactory : CustomWebApplicationFactory
{
    protected override IDictionary<string, string?> GetAdditionalConfiguration() => new Dictionary<string, string?>
    {
        { "RateLimiting:AuthFixed:PermitLimit", "1000" },
        { "RateLimiting:AuthFixed:WindowSeconds", "1" }
    };
}