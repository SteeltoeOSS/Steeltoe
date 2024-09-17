// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ThreadDump;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:dump:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "threaddump",
        ["management:endpoints:actuator:exposure:include:1"] = "dump"
    };

    [Fact]
    public async Task HandleThreadDumpRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<ThreadDumpEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<ThreadDumpEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>(AppSettings);

        var threadDumper = new EventPipeThreadDumper(endpointOptionsMonitor, NullLogger<EventPipeThreadDumper>.Instance);
        var handler = new ThreadDumpEndpointHandler(endpointOptionsMonitor, threadDumper, NullLoggerFactory.Instance);
        var middleware = new ThreadDumpEndpointMiddleware(handler, managementOptions, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest("GET", "/dump");
        await middleware.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        string json = await reader.ReadToEndAsync();
        Assert.NotNull(json);
        Assert.NotEqual("[]", json);
        Assert.StartsWith("[", json, StringComparison.Ordinal);
        Assert.EndsWith("]", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ThreadDumpActuator_ReturnsExpectedData()
    {
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<StartupV1>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/dump"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
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
            IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
            builder.UseStartup<Startup>();
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));

            using IWebHost host = builder.Build();
            await host.StartAsync();

            using HttpClient client = host.GetTestClient();
            HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/threaddump"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string json = await response.Content.ReadAsStringAsync();
            Assert.NotNull(json);
            Assert.NotEqual("{}", json);
            Assert.StartsWith("{", json, StringComparison.Ordinal);
            Assert.EndsWith("}", json, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RoutesByPathAndVerb_V1()
    {
        ThreadDumpEndpointOptions endpointOptions = GetOptionsFromSettings<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptionsV1>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/dump", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));

        Assert.Equal("/cloudfoundryapplication/dump",
            endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));

        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<ThreadDumpEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/threaddump", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));

        Assert.Equal("/cloudfoundryapplication/threaddump",
            endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));

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
