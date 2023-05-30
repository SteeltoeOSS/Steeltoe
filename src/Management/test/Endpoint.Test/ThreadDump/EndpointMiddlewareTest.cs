// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.ThreadDump;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:dump:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "threaddump",
        ["management:endpoints:actuator:exposure:include:1"] = "dump"
    };

    [Fact]
    public async Task HandleThreadDumpRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<ThreadDumpEndpointOptions> opts = GetOptionsMonitorFromSettings<ThreadDumpEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>(AppSettings);

        var obs = new ThreadDumperEp(opts, NullLogger<ThreadDumperEp>.Instance);
        var ep = new ThreadDumpEndpointHandler(opts, obs, NullLoggerFactory.Instance);
        var middle = new ThreadDumpEndpointMiddleware(ep, managementOptions, NullLogger<ThreadDumpEndpointMiddleware>.Instance);
        HttpContext context = CreateRequest("GET", "/dump");
        await middle.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        string json = await rdr.ReadToEndAsync();
        Assert.NotNull(json);
        Assert.NotEqual("[]", json);
        Assert.StartsWith("[", json, StringComparison.Ordinal);
        Assert.EndsWith("]", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ThreadDumpActuator_ReturnsExpectedData()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<StartupV1>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/dump"));
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);
        Assert.NotEqual("[]", json);
        Assert.StartsWith("[", json, StringComparison.Ordinal);
        Assert.EndsWith("]", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ThreadDumpActuatorV2_ReturnsExpectedData()
    {
        if (Platform.IsWindows)
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
            HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/threaddump"));
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            string json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);
            Assert.NotEqual("{}", json);
            Assert.StartsWith("{", json, StringComparison.Ordinal);
            Assert.EndsWith("}", json, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RoutesByPathAndVerb_V1()
    {
        ThreadDumpEndpointOptions options = GetOptionsFromSettings<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptionsV1>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/dump", options.GetContextPath(managementOptions.Get(ActuatorContext.Name)));
        Assert.Equal("/cloudfoundryapplication/dump", options.GetContextPath(managementOptions.Get(CFContext.Name)));
        Assert.Contains("Get", options.AllowedVerbs);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<ThreadDumpEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/threaddump", options.GetContextPath(managementOptions.Get(ActuatorContext.Name)));
        Assert.Equal("/cloudfoundryapplication/threaddump", options.GetContextPath(managementOptions.Get(CFContext.Name)));
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
