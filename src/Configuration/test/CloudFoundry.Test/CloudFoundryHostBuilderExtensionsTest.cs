// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test;

public class CloudFoundryHostBuilderExtensionsTest
{
    [Fact]
    public void WebHostAddCloudConfigurationFoundry_Adds()
    {
        var hostbuilder = new WebHostBuilder();

        hostbuilder.Configure(_ =>
        {
        });

        hostbuilder.AddCloudFoundryConfiguration();
        IWebHost host = hostbuilder.Build();

        IApplicationInstanceInfo instanceInfo = host.Services.GetApplicationInstanceInfo();
        Assert.IsAssignableFrom<CloudFoundryApplicationOptions>(instanceInfo);
        var cfg = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
        Assert.Contains(cfg.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }

    [Fact]
    public void HostAddCloudFoundryConfiguration_Adds()
    {
        var hostbuilder = new HostBuilder();

        hostbuilder.AddCloudFoundryConfiguration();
        IHost host = hostbuilder.Build();

        IApplicationInstanceInfo instanceInfo = host.Services.GetApplicationInstanceInfo();
        Assert.IsAssignableFrom<CloudFoundryApplicationOptions>(instanceInfo);
        var cfg = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
        Assert.Contains(cfg.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }

    [Fact]
    public void WebHostAddCloudFoundryConfiguration_Adds()
    {
        var hostbuilder = new WebHostBuilder();

        hostbuilder.Configure(_ =>
        {
        });

        hostbuilder.AddCloudFoundryConfiguration();
        IWebHost host = hostbuilder.Build();

        var cfg = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
        Assert.Contains(cfg.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }

    [Fact]
    public void WebApplicationAddCloudFoundryConfiguration_Adds()
    {
        WebApplicationBuilder hostbuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostbuilder.AddCloudFoundryConfiguration();
        WebApplication host = hostbuilder.Build();

        var config = host.Services.GetService(typeof(IConfiguration)) as IConfigurationRoot;
        Assert.Contains(config.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }
}
