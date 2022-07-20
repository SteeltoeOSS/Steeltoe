// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Steeltoe.Common.Hosting.Test;

public class HostBuilderExtensionsTest
{
    [Fact]
    public void UseCloudHosting_Web_ThrowsIfHostBuilderNull()
    {
        const IWebHostBuilder webHostBuilder = null;

        var ex = Assert.Throws<ArgumentNullException>(() => webHostBuilder.UseCloudHosting());
        Assert.Contains(nameof(webHostBuilder), ex.Message);
    }

    [Fact]
    public void UseCloudHosting_Default8080()
    {
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);
        var hostBuilder = new WebHostBuilder()
            .UseStartup<TestServerStartup>()
            .UseKestrel();

        hostBuilder.UseCloudHosting();
        var server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://*:8080", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_MakeSureThePortIsSet()
    {
        Environment.SetEnvironmentVariable("PORT", "42");
        var hostBuilder = new WebHostBuilder()
            .UseStartup<TestServerStartup>()
            .UseKestrel();

        hostBuilder.UseCloudHosting();
        var server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://*:42", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_ReadsTyePorts()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", "80;443");
        var hostBuilder = new WebHostBuilder()
            .UseStartup<TestServerStartup>()
            .UseKestrel();

        hostBuilder.UseCloudHosting();
        var server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://*:80", addresses.Addresses);
        Assert.Contains("https://*:443", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_SeesTyePortsAndUsesAspNetCoreURL()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://*:80;https://*:443");
        Environment.SetEnvironmentVariable("PORT", "88;4443");
        var hostBuilder = new WebHostBuilder()
            .UseStartup<TestServerStartup>()
            .UseKestrel();

        hostBuilder.UseCloudHosting();
        var server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://*:80", addresses.Addresses);
        Assert.Contains("https://*:443", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_UsesServerPort()
    {
        Environment.SetEnvironmentVariable("SERVER_PORT", "42");
        var hostBuilder = new WebHostBuilder()
            .UseStartup<TestServerStartup>()
            .UseKestrel();

        hostBuilder.UseCloudHosting();
        var server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://*:42", addresses.Addresses);

        Environment.SetEnvironmentVariable("SERVER_PORT", null);
    }

    [Fact]
    public void UseCloudHosting_UsesLocalPortSettings()
    {
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);
        var hostBuilder = new WebHostBuilder()
            .UseStartup<TestServerStartup>()
            .UseKestrel();

        hostBuilder.UseCloudHosting(5000, 5001);
        var server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://*:5000", addresses.Addresses);
        Assert.Contains("https://*:5001", addresses.Addresses);
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void UseCloudHosting_GenericHost_Default8080()
#pragma warning restore S2699 // Tests should include assertions
    {
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(configure =>
            {
                configure.UseStartup<TestServerStartupDefault>();
                configure.UseKestrel();
            });

        hostBuilder.UseCloudHosting();
        using var host = hostBuilder.Build();
        host.Start();
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void UseCloudHosting_GenericHost_MakeSureThePortIsSet()
#pragma warning restore S2699 // Tests should include assertions
    {
        Environment.SetEnvironmentVariable("PORT", "5042");
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(configure =>
            {
                configure.UseStartup<TestServerStartup42>();
                configure.UseKestrel();
            });

        hostBuilder.UseCloudHosting();
        using var host = hostBuilder.Build();
        host.Start();
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
    [Trait("Category", "SkipOnMacOS")] // for .NET 5+, this test produces an admin prompt on OSX
#pragma warning disable S2699 // Tests should include assertions
    public void UseCloudHosting_GenericHost_UsesLocalPortSettings()
#pragma warning restore S2699 // Tests should include assertions
    {
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(configure =>
            {
                configure.UseStartup<TestServerStartupLocals>();
                configure.UseKestrel();
            });

        hostBuilder.UseCloudHosting(5001, 5002);
        using var host = hostBuilder.Build();
        host.Start();
    }
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
