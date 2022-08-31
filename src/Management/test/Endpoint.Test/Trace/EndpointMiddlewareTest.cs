// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Trace.Test;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new()
    {
        ["Logging:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:trace:enabled"] = "true"
    };

    [Fact]
    public async Task HandleTraceRequestAsync_ReturnsExpected()
    {
        var opts = new TraceEndpointOptions();
        var managementOptions = new CloudFoundryManagementOptions();
        managementOptions.EndpointOptions.Add(opts);

        var obs = new TraceDiagnosticObserver(opts);
        var ep = new TestTraceEndpoint(opts, obs);
        var middle = new TraceEndpointMiddleware(null, ep, managementOptions);
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
        var opts = new TraceEndpointOptions();
        var managementOptions = new CloudFoundryManagementOptions();
        managementOptions.EndpointOptions.Add(opts);

        var obs = new TraceDiagnosticObserver(opts);
        var ep = new TestTraceEndpoint(opts, obs);
        var middle = new TraceEndpointMiddleware(null, ep, managementOptions);
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
        HttpResponseMessage result = await client.GetAsync("http://localhost/cloudfoundryapplication/httptrace");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = new HttpTraceEndpointOptions();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/httptrace", options.GetContextPath(new ActuatorManagementOptions()));
        Assert.Equal("/cloudfoundryapplication/httptrace", options.GetContextPath(new CloudFoundryManagementOptions()));
        Assert.Null(options.AllowedVerbs);
    }

    [Fact]
    public void RoutesByPathAndVerbTrace()
    {
        var options = new TraceEndpointOptions();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/trace", options.GetContextPath(new ActuatorManagementOptions()));
        Assert.Equal("/cloudfoundryapplication/trace", options.GetContextPath(new CloudFoundryManagementOptions()));
        Assert.Null(options.AllowedVerbs);
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
