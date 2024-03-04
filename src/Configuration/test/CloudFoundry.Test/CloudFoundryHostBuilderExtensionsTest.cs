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
using Xunit;

namespace Steeltoe.Configuration.CloudFoundry.Test;

public sealed class CloudFoundryHostBuilderExtensionsTest
{
    [Fact]
    public void HostAddCloudFoundryConfiguration_Adds()
    {
        var hostbuilder = new HostBuilder();
        hostbuilder.AddCloudFoundryConfiguration();
        IHost host = hostbuilder.Build();

        IApplicationInstanceInfo instanceInfo = host.Services.GetApplicationInstanceInfo();
        Assert.IsAssignableFrom<CloudFoundryApplicationOptions>(instanceInfo);
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
        Assert.Contains(configurationRoot.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }

    [Fact]
    public void WebHostAddCloudFoundryConfiguration_Adds()
    {
        var hostbuilder = new WebHostBuilder();
        hostbuilder.Configure(HostingHelpers.EmptyAction);
        hostbuilder.AddCloudFoundryConfiguration();
        IWebHost host = hostbuilder.Build();

        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
        Assert.Contains(configurationRoot.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }

    [Fact]
    public void WebApplicationAddCloudFoundryConfiguration_Adds()
    {
        WebApplicationBuilder hostbuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostbuilder.AddCloudFoundryConfiguration();
        WebApplication host = hostbuilder.Build();

        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
        Assert.Contains(configurationRoot.Providers, provider => provider is CloudFoundryConfigurationProvider);
    }
}
