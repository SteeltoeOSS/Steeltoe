// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Common.Hosting.Test;

public class HostBuilderExtensionsTest
{
    [Fact]
    public void UseCloudHosting_Web_ThrowsIfHostBuilderNull()
    {
        IWebHostBuilder webHostBuilder = null;

        var ex = Assert.Throws<ArgumentNullException>(() => HostBuilderExtensions.UseCloudHosting(webHostBuilder));
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
        try
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
        finally
        {
            Environment.SetEnvironmentVariable("PORT", null);
        }
    }

    [Fact]
    public void UseCloudHosting_ReadsTyePorts()
    {
        try
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
        finally
        {
            Environment.SetEnvironmentVariable("PORT", null);
        }
    }

    [Fact]
    public void UseCloudHosting_SeesTyePortsAndUsesAspNetCoreURL()
    {
        try
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
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
            Environment.SetEnvironmentVariable("PORT", null);
        }
    }

    [Fact]
    public void UseCloudHosting_UsesServerPort()
    {
        try
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
        finally
        {
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
        }
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

    [Fact]
    public void UseCloudHosting_UsesCommandLine_ServerUrls()
    {
        var config = new ConfigurationBuilder().AddCommandLine(new string[]
        {
            $"--{HostBuilderExtensions.DeprecatedServerUrlsKey}",
            "http://*:8088"
        }).Build();

        var hostBuilder = new WebHostBuilder().UseConfiguration(config).UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        var server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();

        Assert.Collection(addresses.Addresses,  (address) => Assert.Equal("http://*:8088", address));
    }

    [Fact]
    public void UseCloudHosting_UsesCommandLine_ServerUrls_Handles_Duplicates()
    {
        var config = new ConfigurationBuilder().AddCommandLine(new string[]
        {
            "--server.urls",
            "http://*:8088",
            "--urls",
            "http://*:8088"
        }).Build();

        var hostBuilder = new WebHostBuilder().UseConfiguration(config).UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        var server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();

        Assert.Collection(addresses.Addresses, (address) => Assert.Equal("http://*:8088", address));
    }
    [Fact]
    public void UseCloudHosting_AnyWildCard_Overrides_SpecificIps()
    {
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://192.168.1.2:8085;http://*:8085");

            var hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();

            Assert.Collection(addresses.Addresses, (address) => Assert.Equal("http://*:8085", address));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", string.Empty);
        }
    }

    [Fact]
    public void UseCloudHosting_MultipleIps_With_Same_Port()
    {
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://192.168.1.2:8085;http://192.168.1.3:8085");

            var hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>().UseKestrel();

            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();

            Assert.Collection(
                addresses.Addresses,
                address => Assert.Equal("http://192.168.1.2:8085", address),
                address => Assert.Equal("http://192.168.1.3:8085", address));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", string.Empty);
        }
    }

    [Fact]
    public void UseCloudHosting_UsesCommandLine_Urls()
    {
        var config = new ConfigurationBuilder().AddCommandLine(new[]
        {
            "--urls",
            "http://*:8081"
        }).Build();

        var hostBuilder = new WebHostBuilder().UseConfiguration(config).UseStartup<TestServerStartup>().UseKestrel();

        hostBuilder.UseCloudHosting();
        var server = hostBuilder.Build();

        var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();

        Assert.Single(addresses.Addresses, "http://*:8081");
        Assert.Contains("http://*:8081", addresses.Addresses);
    }

    [Fact]
    public void UseCloudHosting_MultipleVariantsWorkTogether()
    {
        try
        {
            Environment.SetEnvironmentVariable("SERVER_PORT", "8080");

            var config = new ConfigurationBuilder().AddCommandLine(new[] { "--urls", "http://0.0.0.0:8080" }).Build();

            var hostBuilder = new WebHostBuilder().UseConfiguration(config).UseStartup<TestServerStartup>()
                .UseKestrel();

            hostBuilder.UseCloudHosting();
            var server = hostBuilder.Build();

            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();

            Assert.Single(addresses.Addresses);
            Assert.Contains("http://*:8080", addresses.Addresses);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SERVER_PORT", null);
        }
    }

    [Fact]
    public void UseCloudHosting_GenericHost_Default8080()
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

    [Fact]
    public void UseCloudHosting_GenericHost_MakeSureThePortIsSet()
    {
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
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
        finally
        {
            Environment.SetEnvironmentVariable("PORT", null);
        }
    }

    [Fact]
#if NET6_0_OR_GREATER
    [Trait("Category", "SkipOnMacOS")] // for .NET 5+, this test produces an admin prompt on OSX
#endif
    public void UseCloudHosting_GenericHost_UsesLocalPortSettings()
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

#if NET6_0_OR_GREATER
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
        try
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
        finally
        {
            Environment.SetEnvironmentVariable("PORT", null);
        }
    }
#endif
}