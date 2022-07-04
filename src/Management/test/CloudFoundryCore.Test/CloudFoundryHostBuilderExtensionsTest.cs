// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.CloudFoundry.Test;

[Obsolete("To be removed in the next major version.")]
public class CloudFoundryHostBuilderExtensionsTest
{
    private static readonly Dictionary<string, string> ManagementSettings = new ()
    {
        ["management:endpoints:path"] = "/testing",
    };

    private readonly Action<IWebHostBuilder> _testServerWithRouting = builder => builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());

    [Fact]
    public void AddCloudFoundryActuators_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings)).Configure(_ => { });

        var host = hostBuilder.AddCloudFoundryActuators().Build();
        var managementOptions = host.Services.GetServices<IManagementOptions>();

        var filters = host.Services.GetServices<IStartupFilter>();

        Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));

        Assert.Single(host.Services.GetServices<ThreadDumpEndpointV2>());

        Assert.NotNull(filters);
        Assert.Single(filters.OfType<CloudFoundryActuatorsStartupFilter>());
    }

    [Fact]
    public void AddCloudFoundryActuators_IWebHostBuilder_Serilog()
    {
        var hostBuilder = WebHost.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings))
            .Configure(_ => { })
            .ConfigureLogging(logging => logging.AddDynamicSerilog());

        var host = hostBuilder.AddCloudFoundryActuators().Build();
        var managementOptions = host.Services.GetServices<IManagementOptions>();

        var filters = host.Services.GetServices<IStartupFilter>();

        Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
        Assert.Single(host.Services.GetServices<ThreadDumpEndpointV2>());

        Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());

        Assert.NotNull(filters);
        Assert.Single(filters.OfType<CloudFoundryActuatorsStartupFilter>());
    }

    [Fact]
    public void AddCloudFoundryActuators_IHostBuilder()
    {
        var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings));

        var host = hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1).Build();
        var managementOptions = host.Services.GetServices<IManagementOptions>();

        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
        Assert.Single(host.Services.GetServices<ThreadDumpEndpoint>());

        Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());

        Assert.NotNull(filter);
        Assert.IsType<CloudFoundryActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddCloudFoundryActuators_IHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(_testServerWithRouting)
            .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings));

        var host = await hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V2).StartAsync();

        var response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/info");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/httptrace");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
    }

    [Fact]
    public async Task AddCloudFoundryActuatorsV1_IHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(_testServerWithRouting)
            .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings));

        var host = await hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1).StartAsync();

        var response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/info");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication/trace");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuators_IHostBuilder_Serilog()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.AddDynamicSerilog())
            .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ManagementSettings))
            .ConfigureWebHost(_testServerWithRouting)
            .AddCloudFoundryActuators();

        var host = hostBuilder.Build();
        var managementOptions = host.Services.GetServices<IManagementOptions>();

        var filters = host.Services.GetServices<IStartupFilter>();

        Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
        Assert.Single(host.Services.GetServices<ThreadDumpEndpointV2>());

        Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());

        Assert.NotNull(filters);
        Assert.Single(filters.OfType<CloudFoundryActuatorsStartupFilter>());
    }
}
