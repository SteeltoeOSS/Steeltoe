// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Refresh;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Refresh;

public class EndpointMiddlewareTest : BaseTest
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

    [Fact]
    public async Task HandleRefreshRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<RefreshEndpointOptions> opts = GetOptionsMonitorFromSettings<RefreshEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>(AppSettings);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var ep = new RefreshEndpointHandler(opts, configurationRoot, NullLoggerFactory.Instance);
        var middle = new RefreshEndpointMiddleware(ep, managementOptions, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/refresh");
        await middle.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string json = await reader.ReadLineAsync();

        const string expected =
            "[\"management\",\"management:endpoints\",\"management:endpoints:enabled\",\"management:endpoints:actuator\",\"management:endpoints:actuator:exposure\",\"management:endpoints:actuator:exposure:include\",\"management:endpoints:actuator:exposure:include:0\",\"Logging\",\"Logging:LogLevel\",\"Logging:LogLevel:Steeltoe\",\"Logging:LogLevel:Pivotal\",\"Logging:LogLevel:Default\",\"Logging:Console\",\"Logging:Console:IncludeScopes\"]";

        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task RefreshActuator_ReturnsExpectedData()
    {
        string aspNetCoreEnvironment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        System.Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

        var appSettings = new Dictionary<string, string>(AppSettings);

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage result = await client.PostAsync(new Uri("http://localhost/actuator/refresh"), null);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            string json = await result.Content.ReadAsStringAsync();

            const string expected =
                "[\"urls\",\"management\",\"management:endpoints\",\"management:endpoints:enabled\",\"management:endpoints:actuator\",\"management:endpoints:actuator:exposure\",\"management:endpoints:actuator:exposure:include\",\"management:endpoints:actuator:exposure:include:0\",\"Logging\",\"Logging:LogLevel\",\"Logging:LogLevel:Steeltoe\",\"Logging:LogLevel:Pivotal\",\"Logging:LogLevel:Default\",\"Logging:Console\",\"Logging:Console:IncludeScopes\",\"environment\",\"applicationName\"]";

            Assert.Equal(expected, json);
        }

        System.Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspNetCoreEnvironment);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<RefreshEndpointOptions>();
        ManagementEndpointOptions mgmtOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>().CurrentValue;
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/refresh", options.GetPathMatchPattern(mgmtOptions.Path, mgmtOptions));
        Assert.Equal("/cloudfoundryapplication/refresh", options.GetPathMatchPattern(ConfigureManagementEndpointOptions.DefaultCFPath, mgmtOptions));
        Assert.Contains("Post", options.AllowedVerbs);
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
