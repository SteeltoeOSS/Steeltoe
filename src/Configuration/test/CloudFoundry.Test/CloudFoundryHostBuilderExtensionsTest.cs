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
        IHostBuilder hostbuilder = TestHostBuilderFactory.Create();
        hostbuilder.AddCloudFoundryConfiguration();
        using IHost host = hostbuilder.Build();

        var instanceInfo = host.Services.GetRequiredService<IApplicationInstanceInfo>();
        Assert.IsAssignableFrom<CloudFoundryApplicationOptions>(instanceInfo);
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
        Assert.Contains(configurationRoot.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }

    [Fact]
    public void WebHostAddCloudFoundryConfiguration_Adds()
    {
        IWebHostBuilder hostbuilder = TestWebHostBuilderFactory.Create();
        hostbuilder.AddCloudFoundryConfiguration();
        using IWebHost host = hostbuilder.Build();

        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
        Assert.Contains(configurationRoot.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }

    [Fact]
    public async Task WebApplicationAddCloudFoundryConfiguration_Adds()
    {
        WebApplicationBuilder hostbuilder = TestWebApplicationBuilderFactory.Create();
        hostbuilder.AddCloudFoundryConfiguration();
        await using WebApplication host = hostbuilder.Build();

        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
        Assert.Contains(configurationRoot.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }
}
