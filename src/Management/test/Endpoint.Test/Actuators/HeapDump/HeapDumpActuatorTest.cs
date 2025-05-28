// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Compression;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

public sealed class HeapDumpActuatorTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "heapdump"
    };

    [Fact]
    public async Task Registers_required_services()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddHeapDumpActuator();
        await using WebApplication host = builder.Build();

        var heapDumper = host.Services.GetService<IHeapDumper>();

        heapDumper.Should().BeOfType<HeapDumper>();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddHeapDumpActuator();
        await using WebApplication host = builder.Build();

        HeapDumpEndpointOptions options = host.Services.GetRequiredService<IOptions<HeapDumpEndpointOptions>>().Value;

        options.HeapDumpType.Should().Be(Platform.IsOSX ? HeapDumpType.GCDump : HeapDumpType.Full);
        options.GCDumpTimeoutInSeconds.Should().Be(30);
        options.Enabled.Should().BeNull();
        options.Id.Should().Be("heapdump");
        options.Path.Should().Be("heapdump");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/heapdump");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:HeapDump:HeapDumpType"] = "mini",
            ["Management:Endpoints:HeapDump:GCDumpTimeoutInSeconds"] = "0",
            ["Management:Endpoints:HeapDump:Enabled"] = "true",
            ["Management:Endpoints:HeapDump:Id"] = "test-actuator-id",
            ["Management:Endpoints:HeapDump:Path"] = "test-actuator-path",
            ["Management:Endpoints:HeapDump:RequiredPermissions"] = "full",
            ["Management:Endpoints:HeapDump:AllowedVerbs:0"] = "post"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHeapDumpActuator();
        await using WebApplication host = builder.Build();

        HeapDumpEndpointOptions options = host.Services.GetRequiredService<IOptions<HeapDumpEndpointOptions>>().Value;

        options.HeapDumpType.Should().Be(HeapDumpType.Mini);
        options.GCDumpTimeoutInSeconds.Should().Be(int.MaxValue);
        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/alt-actuators").Should().Be("/alt-actuators/test-actuator-path");
    }

    [Theory]
    [InlineData(HostBuilderType.Host, HeapDumpType.Full)]
    [InlineData(HostBuilderType.Host, HeapDumpType.Heap)]
    [InlineData(HostBuilderType.Host, HeapDumpType.Mini)]
    [InlineData(HostBuilderType.Host, HeapDumpType.Triage)]
    [InlineData(HostBuilderType.Host, HeapDumpType.GCDump)]
    [InlineData(HostBuilderType.WebHost, HeapDumpType.Full)]
    [InlineData(HostBuilderType.WebHost, HeapDumpType.Heap)]
    [InlineData(HostBuilderType.WebHost, HeapDumpType.Mini)]
    [InlineData(HostBuilderType.WebHost, HeapDumpType.Triage)]
    [InlineData(HostBuilderType.WebHost, HeapDumpType.GCDump)]
    [InlineData(HostBuilderType.WebApplication, HeapDumpType.Full)]
    [InlineData(HostBuilderType.WebApplication, HeapDumpType.Heap)]
    [InlineData(HostBuilderType.WebApplication, HeapDumpType.Mini)]
    [InlineData(HostBuilderType.WebApplication, HeapDumpType.Triage)]
    [InlineData(HostBuilderType.WebApplication, HeapDumpType.GCDump)]
    public async Task Endpoint_returns_expected_data(HostBuilderType hostBuilderType, HeapDumpType heapDumpType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:HeapDump:HeapDumpType"] = heapDumpType.ToString()
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHeapDumper, FakeHeapDumper>();
                services.AddHeapDumpActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/heapdump"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/octet-stream");
        response.Content.Headers.ContentDisposition.Should().NotBeNull().And.Subject.ToString().Should().NotBeEmpty();

        using var memoryStream = new MemoryStream();

        await using (Stream responseStream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken))
        {
            await using var zipStream = new GZipStream(responseStream, CompressionMode.Decompress);
            await zipStream.CopyToAsync(memoryStream, TestContext.Current.CancellationToken);

            responseStream.Length.Should().BeLessThan(memoryStream.Length);
        }

        memoryStream.Length.Should().Be(FakeHeapDumper.FakeFileContent.Length);
        memoryStream.GetBuffer().Should().BeEquivalentTo(FakeHeapDumper.FakeFileContent);
    }
}
