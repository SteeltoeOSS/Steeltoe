// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        using IWebHost server = hostBuilder.Build();
        server.Start();
        

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://[::]:8080", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_MakeSureThePortIsSet()
    {
        Environment.SetEnvironmentVariable("PORT", "42");
        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        IWebHost server = hostBuilder.Build();
        server.Start();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://[::]:42", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_ReadsTyePorts()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", "80;443");
        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        IWebHost server = hostBuilder.Build();
        server.Start();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://[::]:80", addresses.Addresses);
        Assert.Contains("https://[::]:443", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_SeesTyePortsAndUsesAspNetCoreURL()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://*:80;https://*:443");
        Environment.SetEnvironmentVariable("PORT", "88;4443");
        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        IWebHost server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://*:80", addresses.Addresses);
        Assert.Contains("https://*:443", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_UsesServerPort()
    {
        Environment.SetEnvironmentVariable("SERVER_PORT", "42");
        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        IWebHost server = hostBuilder.Build();
        server.Start();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://[::]:42", addresses.Addresses);

        Environment.SetEnvironmentVariable("SERVER_PORT", null);
    }

    [Fact]
    public void UseCloudHosting_UsesLocalPortSettings()
    {
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);

        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting(5000, 5001);
        IWebHost server = hostBuilder.Build();
        server.Start();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Contains("http://[::]:5000", addresses.Addresses);
        Assert.Contains("https://[::]:5001", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_GenericHost_Default8080()
    {
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);

        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(configure =>
        {
            configure.UseStartup<TestServerStartup>();
            configure.UseKestrel();
        });

        hostBuilder.UseCloudHosting();
        using IHost host = hostBuilder.Build();
        host.Start();

        var addresses = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses;
        Assert.NotNull(addresses);
        Assert.Contains("http://[::]:8080", addresses);
    }

    [Fact]
    public void UseCloudHosting_GenericHost_MakeSureThePortIsSet()
    {
        Environment.SetEnvironmentVariable("PORT", "5042");

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(configure =>
        {
            configure.UseStartup<TestServerStartup>();
            configure.UseKestrel();
        });

        hostBuilder.UseCloudHosting();
        using IHost host = hostBuilder.Build();
        host.Start();

        var addresses = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses;
        Assert.NotNull(addresses);
        Assert.Contains("http://[::]:5042", addresses);

    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // for .NET 5+, this test produces an admin prompt on OSX
    public void UseCloudHosting_GenericHost_UsesLocalPortSettings()
    {
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(configure =>
        {
            configure.UseStartup<TestServerStartup>();
            configure.UseKestrel();
        });

        hostBuilder.UseCloudHosting(5001, 5002);
        using IHost host = hostBuilder.Build();
        host.Start();

        var addresses = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses;
        Assert.NotNull(addresses);
        Assert.Contains("http://[::]:5001", addresses);
        Assert.Contains("https://[::]:5002", addresses);

    }

    [Fact]
    public void UseCloudHosting_WebApplication_UsesLocalPortSettings()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);
        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();

        hostBuilder.UseCloudHosting(3000, 3001);
        var host = hostBuilder.Build();
            host.Start();

        var addresses = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses;
        Assert.NotNull(addresses);
        Assert.Contains("http://[::]:3000", addresses);
        Assert.Contains("https://[::]:3001", addresses);

    }

    [Fact]
    public void UseCloudHosting_WebApplication_Default8080()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.UseCloudHosting();
        using WebApplication host = hostBuilder.Build();
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
        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();

        hostBuilder.UseCloudHosting();
        using WebApplication host = hostBuilder.Build();
        host.Start();

        var addressFeature = ((IApplicationBuilder)host).ServerFeatures.Get<IServerAddressesFeature>();
        Assert.Single(addressFeature.Addresses);
        Assert.Equal("http://[::]:5042", addressFeature.Addresses.First());
    }
}
