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
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:heapDump:enabled"] = "true",
        ["management:endpoints:heapDump:heapDumpType"] = "gcdump",
        ["management:endpoints:actuator:exposure:include:0"] = "heapdump"
    };

    [Fact]
    public async Task HandleHeapDumpRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<HeapDumpEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<HeapDumpEndpointOptions>(AppSettings);
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>(AppSettings);

        IServiceCollection services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

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
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/heapdump"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(response.Content.Headers.Contains("Content-Type"));
        IEnumerable<string> contentType = response.Content.Headers.GetValues("Content-Type");
        Assert.Equal("application/octet-stream", contentType.Single());
        Assert.True(response.Content.Headers.Contains("Content-Disposition"));

        using var tempFile = new TempFile();

        await using (Stream responseStream = await response.Content.ReadAsStreamAsync())
        {
            await using var writeStream = new FileStream(tempFile.FullPath, FileMode.Create);
            await responseStream.CopyToAsync(writeStream);
        }

        await using FileStream readStream = File.Open(tempFile.FullPath, FileMode.Open);
        Assert.NotEqual(0, readStream.Length);
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
