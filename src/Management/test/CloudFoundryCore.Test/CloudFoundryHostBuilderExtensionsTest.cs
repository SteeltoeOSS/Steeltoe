// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.ThreadDump;
using Xunit;

namespace Steeltoe.Management.CloudFoundry.Test;

[Obsolete("To be removed in the next major version.")]
public class CloudFoundryHostBuilderExtensionsTest
{
    private static readonly Dictionary<string, string> ManagementSettings = new()
    {
        ["management:endpoints:path"] = "/testing"
    };

    private readonly Action<IWebHostBuilder> _testServerWithRouting = builder =>
        builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());

    [Fact]
    public void AddCloudFoundryActuators_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ManagementSettings)).Configure(
            _ =>
            {
            });

        IWebHost host = hostBuilder.AddCloudFoundryActuators().Build();
        IEnumerable<IManagementOptions> managementOptions = host.Services.GetServices<IManagementOptions>();

        IEnumerable<IStartupFilter> filters = host.Services.GetServices<IStartupFilter>();

        Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));

        Assert.Single(host.Services.GetServices<ThreadDumpEndpointV2>());

        Assert.NotNull(filters);
        Assert.Single(filters.OfType<CloudFoundryActuatorsStartupFilter>());
    }

    [Fact]
    public void AddCloudFoundryActuators_IWebHostBuilder_Serilog()
    {
        IWebHostBuilder hostBuilder = WebHost.CreateDefaultBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ManagementSettings))
            .Configure(_ =>
            {
            }).ConfigureLogging(logging => logging.AddDynamicSerilog());

        IWebHost host = hostBuilder.AddCloudFoundryActuators().Build();
        IEnumerable<IManagementOptions> managementOptions = host.Services.GetServices<IManagementOptions>();

        IEnumerable<IStartupFilter> filters = host.Services.GetServices<IStartupFilter>();

        Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
        Assert.Single(host.Services.GetServices<ThreadDumpEndpointV2>());

        Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());

        Assert.NotNull(filters);
        Assert.Single(filters.OfType<CloudFoundryActuatorsStartupFilter>());
    }

    [Fact]
    public void AddCloudFoundryActuators_IHostBuilder()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ManagementSettings));

        IHost host = hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1).Build();
        IEnumerable<IManagementOptions> managementOptions = host.Services.GetServices<IManagementOptions>();

        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
        Assert.Single(host.Services.GetServices<ThreadDumpEndpoint>());

        Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());

        Assert.NotNull(filter);
        Assert.IsType<CloudFoundryActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddCloudFoundryActuators_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting)
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ManagementSettings));

        using IHost host = await hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V2).StartAsync();

        Task<HttpResponseMessage> response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
        Assert.Equal(HttpStatusCode.OK, response.Result.StatusCode);
        response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/info");
        Assert.Equal(HttpStatusCode.OK, response.Result.StatusCode);
        response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/httptrace");
        Assert.Equal(HttpStatusCode.OK, response.Result.StatusCode);
    }

    [Fact]
    public async Task AddCloudFoundryActuatorsV1_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting)
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ManagementSettings));

        using IHost host = await hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1).StartAsync();

        Task<HttpResponseMessage> response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
        Assert.Equal(HttpStatusCode.OK, response.Result.StatusCode);
        response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/info");
        Assert.Equal(HttpStatusCode.OK, response.Result.StatusCode);
        response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/trace");
        Assert.Equal(HttpStatusCode.OK, response.Result.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuators_IHostBuilder_Serilog()
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder().ConfigureLogging(logging => logging.AddDynamicSerilog())
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ManagementSettings)).ConfigureWebHost(_testServerWithRouting)
            .AddCloudFoundryActuators();

        IHost host = hostBuilder.Build();
        IEnumerable<IManagementOptions> managementOptions = host.Services.GetServices<IManagementOptions>();

        IEnumerable<IStartupFilter> filters = host.Services.GetServices<IStartupFilter>();

        Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
        Assert.Single(host.Services.GetServices<ThreadDumpEndpointV2>());

        Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());

        Assert.NotNull(filters);
        Assert.Single(filters.OfType<CloudFoundryActuatorsStartupFilter>());
    }
}
