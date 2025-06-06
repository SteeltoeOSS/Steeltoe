// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;

namespace Steeltoe.Management.Endpoint.Test.Actuators.DbMigrations;

public sealed class DbMigrationsActuatorTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "dbmigrations"
    };

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddDbMigrationsActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Func<DbMigrationsEndpointMiddleware> action = serviceProvider.GetRequiredService<DbMigrationsEndpointMiddleware>;
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddDbMigrationsActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        DbMigrationsEndpointOptions options = serviceProvider.GetRequiredService<IOptions<DbMigrationsEndpointOptions>>().Value;

        options.Enabled.Should().BeNull();
        options.Id.Should().Be("dbmigrations");
        options.Path.Should().Be("dbmigrations");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/dbmigrations");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:DbMigrations:Enabled"] = "true",
            ["Management:Endpoints:DbMigrations:Id"] = "test-actuator-id",
            ["Management:Endpoints:DbMigrations:Path"] = "test-actuator-path",
            ["Management:Endpoints:DbMigrations:RequiredPermissions"] = "full",
            ["Management:Endpoints:DbMigrations:AllowedVerbs:0"] = "post"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddDbMigrationsActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        DbMigrationsEndpointOptions options = serviceProvider.GetRequiredService<IOptions<DbMigrationsEndpointOptions>>().Value;

        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/alt-actuators").Should().Be("/alt-actuators/test-actuator-path");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_expected_data(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddEntityFrameworkInMemoryDatabase();
                services.AddDbContext<TestDbContext>();
                services.AddSingleton<IDatabaseMigrationScanner, FakeDatabaseMigrationScanner>();
                services.AddDbMigrationsActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/dbmigrations"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "TestDbContext": {
                "pendingMigrations": [
                  "pending"
                ],
                "appliedMigrations": [
                  "applied"
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Endpoint_returns_empty_when_no_DbContext_is_registered()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddSingleton<IDatabaseMigrationScanner, FakeDatabaseMigrationScanner>();
        builder.Services.AddDbMigrationsActuator();
        WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/dbmigrations"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("{}");
    }

    [Fact]
    public async Task Endpoint_returns_all_migrations_when_pending_migrations_are_unavailable()
    {
        var throwingScanner = new FakeDatabaseMigrationScanner
        {
            ThrowOnGetPendingMigrations = true
        };

        using var loggerProvider = new CapturingLoggerProvider();

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Logging.AddProvider(loggerProvider);
        builder.Services.AddDbContext<TestDbContext>();
        builder.Services.AddSingleton<IDatabaseMigrationScanner>(throwingScanner);
        builder.Services.AddDbMigrationsActuator();
        WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/dbmigrations"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "TestDbContext": {
                "pendingMigrations": [
                  "migration"
                ],
                "appliedMigrations": []
              }
            }
            """);

        IList<string> logLines = loggerProvider.GetAll();
        logLines.Should().Contain($"WARN {typeof(DbMigrationsEndpointHandler).FullName}: Failed to load pending/applied migrations, returning all migrations.");
    }
}
