// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Environment;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "*"
    };

    [Fact]
    public async Task HandleEnvironmentRequestAsync_ReturnsExpected()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IOptionsMonitor<EnvironmentEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<EnvironmentEndpointOptions, ConfigureEnvironmentEndpointOptions>();

        IOptionsMonitor<ManagementOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>(AppSettings);

        IHostEnvironment hostEnvironment = TestHostEnvironmentFactory.Create();

        var handler = new EnvironmentEndpointHandler(optionsMonitor, configurationRoot, hostEnvironment, NullLoggerFactory.Instance);
        var middleware = new EnvironmentEndpointMiddleware(handler, managementOptions, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/env");
        await middleware.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string? json = await reader.ReadLineAsync();

        const string expected =
            "{\"activeProfiles\":[\"Test\"],\"propertySources\":[{\"name\":\"MemoryConfigurationProvider\",\"properties\":{\"Logging:Console:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:actuator:exposure:include:0\":{\"value\":\"*\"},\"management:endpoints:enabled\":{\"value\":\"true\"}}}]}";

        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task EnvironmentActuator_ReturnsExpectedData()
    {
        // Some developers set ASPNETCORE_ENVIRONMENT in their environment, which will break this test if we don't un-set it
        using var scope = new EnvironmentVariableScope("ASPNETCORE_ENVIRONMENT", null);

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using var server = new TestServer(builder);

        HttpClient client = server.CreateClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/env"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();

        const string expected =
            "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"name\":\"ChainedConfigurationProvider\",\"properties\":{\"applicationName\":{\"value\":\"Steeltoe.Management.Endpoint.Test\"}}},{\"name\":\"MemoryConfigurationProvider\",\"properties\":{\"Logging:Console:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:actuator:exposure:include:0\":{\"value\":\"*\"},\"management:endpoints:enabled\":{\"value\":\"true\"}}}]}";

        Assert.Equal(expected, json);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<EnvironmentEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/env", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path!));

        Assert.Equal("/cloudfoundryapplication/env",
            endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));

        Assert.Single(endpointOptions.AllowedVerbs);
        Assert.Contains("Get", endpointOptions.AllowedVerbs);
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
