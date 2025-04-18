// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Refresh;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:TestApp"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "*"
    };

    [Fact]
    public async Task HandleRefreshRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<RefreshEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<RefreshEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>(AppSettings);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var handler = new RefreshEndpointHandler(endpointOptionsMonitor, configurationRoot, NullLoggerFactory.Instance);
        var middleware = new RefreshEndpointMiddleware(handler, managementOptions, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("POST", "/refresh");
        await middleware.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string? json = await reader.ReadLineAsync(TestContext.Current.CancellationToken);

        json.Should().BeJson("""
            [
              "management",
              "management:endpoints",
              "management:endpoints:enabled",
              "management:endpoints:actuator",
              "management:endpoints:actuator:exposure",
              "management:endpoints:actuator:exposure:include",
              "management:endpoints:actuator:exposure:include:0",
              "Logging",
              "Logging:LogLevel",
              "Logging:LogLevel:TestApp",
              "Logging:LogLevel:Steeltoe",
              "Logging:LogLevel:Default",
              "Logging:Console",
              "Logging:Console:IncludeScopes"
            ]
            """);
    }

    [Fact]
    public async Task RefreshActuator_ReturnsExpectedData()
    {
        using var scope = new EnvironmentVariableScope("ASPNETCORE_ENVIRONMENT", null);

        var appSettings = new Dictionary<string, string?>(AppSettings);

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.PostAsync(new Uri("http://localhost/actuator/refresh"), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        json.Should().BeJson("""
            [
              "urls",
              "management",
              "management:endpoints",
              "management:endpoints:enabled",
              "management:endpoints:actuator",
              "management:endpoints:actuator:exposure",
              "management:endpoints:actuator:exposure:include",
              "management:endpoints:actuator:exposure:include:0",
              "Logging",
              "Logging:LogLevel",
              "Logging:LogLevel:TestApp",
              "Logging:LogLevel:Steeltoe",
              "Logging:LogLevel:Default",
              "Logging:Console",
              "Logging:Console:IncludeScopes",
              "environment",
              "applicationName"
            ]
            """);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<RefreshEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/refresh", endpointOptions.GetPathMatchPattern(managementOptions.Path));
        Assert.Equal("/cloudfoundryapplication/refresh", endpointOptions.GetPathMatchPattern(ConfigureManagementOptions.DefaultCloudFoundryPath));
        Assert.Contains("Post", endpointOptions.AllowedVerbs);
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
