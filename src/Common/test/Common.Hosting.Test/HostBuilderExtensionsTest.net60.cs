// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Steeltoe.Common.Hosting.Test;

public partial class HostBuilderExtensionsTest
{
    [Fact]
    [Trait("Category", "SkipOnMacOS")] // for .NET 5+, this test produces an admin prompt on OSX
    public async Task UseCloudHosting_WebApplication_UsesLocalPortSettings()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);
        var hostBuilder = WebApplication.CreateBuilder();

        hostBuilder.UseCloudHosting(3000, 3001);
        var host = hostBuilder.Build();
        await host.StartAsync();
        var addressFeature = ((IApplicationBuilder)host).ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://[::]:3000", addressFeature.Addresses);
        Assert.Contains("https://[::]:3001", addressFeature.Addresses);
    }

    [Fact]
    public void UseCloudHosting_WebApplication_Default8080()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);

        // configure.UseStartup<TestServerStartupDefault>();
        var hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.UseCloudHosting();
        using var host = hostBuilder.Build();
        host.Start();

        var addressFeature = ((IApplicationBuilder)host).ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Single(addressFeature.Addresses);
        Assert.Equal("http://[::]:8080", addressFeature.Addresses.First());
    }

    [Fact]
    public void UseCloudHosting_WebApplication_MakeSureThePortIsSet()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", "5042");
        var hostBuilder = WebApplication.CreateBuilder();

        hostBuilder.UseCloudHosting();
        using var host = hostBuilder.Build();
        host.Start();

        var addressFeature = ((IApplicationBuilder)host).ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Single(addressFeature.Addresses);
        Assert.Equal("http://[::]:5042", addressFeature.Addresses.First());
    }
}
#endif
