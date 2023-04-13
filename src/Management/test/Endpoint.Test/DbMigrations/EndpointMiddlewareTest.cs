// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.DbMigrations;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "dbmigrations"
    };

    [Fact]
    public async Task HandleEntityFrameworkRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<DbMigrationsEndpointOptions> opts = GetOptionsMonitorFromSettings<DbMigrationsEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        managementOptions.Get(ActuatorContext.Name).EndpointOptions.Add(opts.CurrentValue);
        var container = new ServiceCollection();
        container.AddScoped<MockDbContext>();
        var helper = Substitute.For<DbMigrationsEndpoint.DbMigrationsEndpointHelper>();
        helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);

        helper.GetPendingMigrations(Arg.Any<DbContext>()).Returns(new[]
        {
            "pending"
        });

        helper.GetAppliedMigrations(Arg.Any<DbContext>()).Returns(new[]
        {
            "applied"
        });

        var ep = new DbMigrationsEndpoint(opts, container.BuildServiceProvider(), helper, NullLogger<DbMigrationsEndpoint>.Instance);

        var middle = new DbMigrationsEndpointMiddleware(ep, managementOptions, NullLogger<DbMigrationsEndpointMiddleware>.Instance);

        HttpContext context = CreateRequest("GET", "/dbmigrations");
        await middle.HandleEntityFrameworkRequestAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string json = await reader.ReadToEndAsync();
        var descriptor = new DbMigrationsDescriptor();
        descriptor.AppliedMigrations.Add("applied");
        descriptor.PendingMigrations.Add("pending");

        string expected = Serialize(new Dictionary<string, DbMigrationsDescriptor>
        {
            {
                nameof(MockDbContext), descriptor
            }
        });

        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task EntityFrameworkActuator_ReturnsExpectedData()
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
        HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/dbmigrations"));
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        var descriptor = new DbMigrationsDescriptor();
        descriptor.AppliedMigrations.Add("applied");
        descriptor.PendingMigrations.Add("pending");
        string expected = Serialize(new Dictionary<string, DbMigrationsDescriptor>
        {
            {
                nameof(MockDbContext), descriptor
            }
        });

        Assert.Equal(expected, json);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<DbMigrationsEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> mgmtOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        ManagementEndpointOptions actuatorMgmtOptions = mgmtOptions.Get(ActuatorContext.Name);
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/dbmigrations", options.GetContextPath(actuatorMgmtOptions));

        ManagementEndpointOptions cfMgmtOptions = mgmtOptions.Get(CFContext.Name);
        Assert.Equal("/cloudfoundryapplication/dbmigrations", options.GetContextPath(cfMgmtOptions));
        Assert.Contains("Get", options.AllowedVerbs);
    }

    private HttpContext CreateRequest(string method, string path)
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Response.Body = new MemoryStream();
        context.Request.Method = method;
        context.Request.Path = new PathString(path);
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        return context;
    }
}
