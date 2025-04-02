// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["management:endpoints:heapDump:heapDumpType"] = "gcdump",
        ["management:endpoints:actuator:exposure:include:0"] = "heapdump"
    };

    [Fact]
    public async Task HeapDumpActuator_ReturnsExpectedData()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));
        using IWebHost app = builder.Build();

        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/heapdump"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.Should().ContainKey("Content-Type").WhoseValue.Should().HaveCount(1).And.Contain("application/octet-stream");
        response.Content.Headers.Should().ContainKey("Content-Disposition");

        using var memoryStream = new MemoryStream();

        await using (Stream responseStream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken))
        {
            await using var zipStream = new GZipStream(responseStream, CompressionMode.Decompress);
            await zipStream.CopyToAsync(memoryStream, TestContext.Current.CancellationToken);

            responseStream.Length.Should().BeLessThan(memoryStream.Length);
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        int firstByteInDump = memoryStream.ReadByte();
        firstByteInDump.Should().NotBe(-1).And.NotBe(0);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<HeapDumpEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/heapdump", endpointOptions.GetPathMatchPattern(managementOptions.Path));
        Assert.Equal("/cloudfoundryapplication/heapdump", endpointOptions.GetPathMatchPattern(ConfigureManagementOptions.DefaultCloudFoundryPath));
        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }
}
