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
using Steeltoe.Management.Endpoint.Environment;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Environment;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "*"
    };

    private readonly IHostEnvironment _host = HostingHelpers.GetHostingEnvironment();

    [Fact]
    public async Task HandleEnvironmentRequestAsync_ReturnsExpected()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IOptionsMonitor<EnvironmentEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<EnvironmentEndpointOptions, ConfigureEnvironmentEndpointOptions>();

        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>(AppSettings);

        var ep = new EnvironmentEndpointHandler(optionsMonitor, configurationRoot, _host, NullLoggerFactory.Instance);
        var middle = new EnvironmentEndpointMiddleware(ep, managementOptions, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/env");
        await middle.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string json = await reader.ReadLineAsync();

        const string expected =
            "{\"activeProfiles\":[\"EnvironmentName\"],\"propertySources\":[{\"name\":\"MemoryConfigurationProvider\",\"properties\":{\"Logging:Console:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:actuator:exposure:include:0\":{\"value\":\"*\"},\"management:endpoints:enabled\":{\"value\":\"true\"}}}]}";

        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task EnvironmentActuator_ReturnsExpectedData()
    {
        // Some developers set ASPNETCORE_ENVIRONMENT in their environment, which will break this test if we don't un-set it
        string aspNetCoreEnvironment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        System.Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/env"));
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            string json = await result.Content.ReadAsStringAsync();

            const string expected =
                "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"name\":\"ChainedConfigurationProvider\",\"properties\":{\"applicationName\":{\"value\":\"Steeltoe.Management.Endpoint.Test\"}}},{\"name\":\"MemoryConfigurationProvider\",\"properties\":{\"Logging:Console:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:actuator:exposure:include:0\":{\"value\":\"*\"},\"management:endpoints:enabled\":{\"value\":\"true\"}}}]}";

            Assert.Equal(expected, json);
        }

        System.Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspNetCoreEnvironment);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<EnvironmentEndpointOptions>();

        ManagementEndpointOptions managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>().CurrentValue;
        Assert.True(options.RequiresExactMatch());
        Assert.Equal("/actuator/env", options.GetPathMatchPattern(managementOptions.Path, managementOptions));

        Assert.Equal("/cloudfoundryapplication/env",
            options.GetPathMatchPattern(ConfigureManagementEndpointOptions.DefaultCloudFoundryPath, managementOptions));

        Assert.Single(options.AllowedVerbs);
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
