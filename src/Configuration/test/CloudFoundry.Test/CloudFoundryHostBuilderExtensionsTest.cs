// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.CloudFoundry.Test;

public sealed class CloudFoundryHostBuilderExtensionsTest
{
    [Fact]
    public void HostAddCloudFoundryConfiguration_Adds()
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.AddCloudFoundryConfiguration();
        using IHost host = hostBuilder.Build();

        var instanceInfo = host.Services.GetRequiredService<IApplicationInstanceInfo>();
        Assert.IsAssignableFrom<CloudFoundryApplicationOptions>(instanceInfo);
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        Assert.Single(configurationRoot.EnumerateProviders<CloudFoundryConfigurationProvider>());
    }

    [Fact]
    public void WebHostAddCloudFoundryConfiguration_Adds()
    {
        WebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddCloudFoundryConfiguration();
        using IWebHost host = hostBuilder.Build();

        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        Assert.Single(configurationRoot.EnumerateProviders<CloudFoundryConfigurationProvider>());
    }

    [Fact]
    public async Task WebApplicationAddCloudFoundryConfiguration_Adds()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.AddCloudFoundryConfiguration();
        await using WebApplication host = hostBuilder.Build();

        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

        Assert.Single(configurationRoot.EnumerateProviders<CloudFoundryConfigurationProvider>());
    }
}
