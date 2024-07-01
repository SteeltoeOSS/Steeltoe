// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Test.HeapDump;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:heapdump:enabled"] = "true",
        ["management:endpoints:heapdump:heapdumptype"] = "gcdump",
        ["management:endpoints:actuator:exposure:include:0"] = "heapdump"
    };

    [Fact]
    public async Task HandleHeapDumpRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<HeapDumpEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<HeapDumpEndpointOptions>(AppSettings);
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>(AppSettings);

        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
        var loggerFactory = serviceCollection.BuildServiceProvider(true).GetRequiredService<ILoggerFactory>();

        ILogger<HeapDumper> logger1 = loggerFactory.CreateLogger<HeapDumper>();

        var heapDumper = new HeapDumper(endpointOptionsMonitor, logger1);

        var handler = new HeapDumpEndpointHandler(endpointOptionsMonitor, heapDumper, loggerFactory);
        var middleware = new HeapDumpEndpointMiddleware(handler, managementOptionsMonitor, loggerFactory);
        HttpContext context = CreateRequest("GET", "/heapdump");
        await middleware.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        byte[] buffer = new byte[1024];
        _ = await context.Response.Body.ReadAsync(buffer);
        Assert.NotEqual(0, buffer[0]);
    }

    [Fact]
    public async Task HeapDumpActuator_ReturnsExpectedData()
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
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/heapdump"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(response.Content.Headers.Contains("Content-Type"));
        IEnumerable<string> contentType = response.Content.Headers.GetValues("Content-Type");
        Assert.Equal("application/octet-stream", contentType.Single());
        Assert.True(response.Content.Headers.Contains("Content-Disposition"));

        var tempFile = new TempFile();
        var stream1 = new FileStream(tempFile.FullPath, FileMode.Create);
        Stream input = await response.Content.ReadAsStreamAsync();
        await input.CopyToAsync(stream1);
        stream1.Close();

        FileStream stream2 = File.Open(tempFile.FullPath, FileMode.Open);
        Assert.NotEqual(0, stream2.Length);
        stream2.Close();
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<HeapDumpEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/heapdump", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));

        Assert.Equal("/cloudfoundryapplication/heapdump",
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
