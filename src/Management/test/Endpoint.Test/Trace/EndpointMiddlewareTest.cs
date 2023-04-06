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
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Trace;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Trace;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:trace:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "httptrace"
    };

    [Fact]
    public async Task HandleTraceRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> opts = GetOptionsMonitorFromSettings<TraceEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();

        var obs = new TraceDiagnosticObserver(opts, NullLogger<TraceDiagnosticObserver>.Instance);
        var ep = new TestTraceEndpoint(opts, obs, NullLogger<TraceEndpoint>.Instance);
        var middle = new TraceEndpointMiddleware(ep, managementOptions, NullLogger<TraceEndpointMiddleware>.Instance);
        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/httptrace");
        await middle.HandleTraceRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        string json = await rdr.ReadToEndAsync();
        Assert.Equal("[]", json);
    }

    [Fact]
    public async Task HandleTraceRequestAsync_OtherPathReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> opts = GetOptionsMonitorFromSettings<TraceEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();

        var obs = new TraceDiagnosticObserver(opts, NullLogger<TraceDiagnosticObserver>.Instance);
        var ep = new TestTraceEndpoint(opts, obs, NullLogger<TraceEndpoint>.Instance);
        var middle = new TraceEndpointMiddleware(ep, managementOptions, NullLogger<TraceEndpointMiddleware>.Instance);
        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/trace");
        await middle.HandleTraceRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        string json = await rdr.ReadToEndAsync();
        Assert.Equal("[]", json);
    }

    [Fact]
    public async Task TraceActuator_ReturnsExpectedData()
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
        HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/httptrace"));
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<TraceEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/httptrace", options.GetContextPath(managementOptions.Get(ActuatorContext.Name)));
        Assert.Equal("/cloudfoundryapplication/httptrace", options.GetContextPath(managementOptions.Get(CFContext.Name)));
        Assert.Contains("Get", options.AllowedVerbs);
    }

    [Fact]
    public void RoutesByPathAndVerbTrace()
    {
        TraceEndpointOptions options = GetOptionsMonitorFromSettings<TraceEndpointOptions>()
            .Get(ConfigureTraceEndpointOptions.TraceEndpointOptionNames.V1.ToString());

        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/trace", options.GetContextPath(managementOptions.Get(ActuatorContext.Name)));
        Assert.Equal("/cloudfoundryapplication/trace", options.GetContextPath(managementOptions.Get(CFContext.Name)));
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
