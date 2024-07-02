// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Test.DbMigrations;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "dbmigrations"
    };

    [Fact]
    public async Task HandleEntityFrameworkCoreRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<DbMigrationsEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<DbMigrationsEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>(AppSettings);

        var container = new ServiceCollection();
        container.AddScoped<MockDbContext>();

        var handler = new DbMigrationsEndpointHandler(endpointOptionsMonitor, container.BuildServiceProvider(true), new TestDatabaseMigrationScanner(),
            NullLoggerFactory.Instance);

        var middleware = new DbMigrationsEndpointMiddleware(handler, managementOptions, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/dbmigrations");
        await middleware.InvokeAsync(context, null);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string json = await reader.ReadToEndAsync();
        var descriptor = new DbMigrationsDescriptor();
        descriptor.AppliedMigrations.Add("applied");
        descriptor.PendingMigrations.Add("pending");

        string expected = Serialize(new Dictionary<string, DbMigrationsDescriptor>
        {
            { nameof(MockDbContext), descriptor }
        });

        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task EntityFrameworkCoreActuator_ReturnsExpectedData()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/dbmigrations"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();
        var descriptor = new DbMigrationsDescriptor();
        descriptor.AppliedMigrations.Add("applied");
        descriptor.PendingMigrations.Add("pending");

        string expected = Serialize(new Dictionary<string, DbMigrationsDescriptor>
        {
            { nameof(MockDbContext), descriptor }
        });

        Assert.Equal(expected, json);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<DbMigrationsEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/dbmigrations", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));
    }

    private HttpContext CreateRequest(string method, string path)
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Response.Body = new MemoryStream();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        return context;
    }
}
