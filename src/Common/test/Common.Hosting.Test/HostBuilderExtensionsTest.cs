// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
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

        (List<int> httpPorts, List<int> httpsPorts) = HostBuilderExtensions.GetPortsFromConfiguration(null, null, null, null);
        Assert.Contains(8080, httpPorts);
        Assert.Empty(httpsPorts);
    }

    [Fact]
    public void UseCloudHosting_MakeSureThePortIsSet()
    {
        Environment.SetEnvironmentVariable("PORT", "42");
        (List<int> httpPorts, List<int> httpsPorts) = HostBuilderExtensions.GetPortsFromConfiguration(null, null, null, null);
        Assert.Contains(42, httpPorts);
        Assert.Empty(httpsPorts);

        Environment.SetEnvironmentVariable("PORT", null);
    }

    [Fact]
    public void UseCloudHosting_ReadsTyePorts()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", "80;443");
        (List<int> httpPorts, List<int> httpsPorts) = HostBuilderExtensions.GetPortsFromConfiguration(null, null, null, null);
        Assert.Contains(80, httpPorts);
        Assert.Contains(443, httpsPorts);
        Environment.SetEnvironmentVariable("PORT", null);
    }

    [Fact]
    public void UseCloudHosting_SeesTyePortsAndUsesAspNetCoreURL()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://*:80;https://*:443");
        Environment.SetEnvironmentVariable("PORT", "88;4443");
        (List<int> httpPorts, List<int> httpsPorts) = HostBuilderExtensions.GetPortsFromConfiguration(null, null, null, null);
        Assert.Contains(80, httpPorts);
        Assert.Contains(443, httpsPorts);

        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);
    }

    [Fact]
    public void UseCloudHosting_UsesServerPort()
    {
        Environment.SetEnvironmentVariable("SERVER_PORT", "42");
        (List<int> httpPorts, List<int> httpsPorts) = HostBuilderExtensions.GetPortsFromConfiguration(null, null, null, null);
        Assert.Contains(42, httpPorts);
        Assert.Empty(httpsPorts);

        Environment.SetEnvironmentVariable("SERVER_PORT", null);
    }

    [Fact]
    public void UseCloudHosting_UsesLocalPortSettings()
    {
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);

        (List<int> httpPorts, List<int> httpsPorts) = HostBuilderExtensions.GetPortsFromConfiguration(null, 5000, 5001, null);
        Assert.Contains(5000, httpPorts);
        Assert.Contains(5001, httpsPorts);
    }

#pragma warning disable S2699 // Tests should include assertions
    [Fact]
    public void UseCloudHosting_GenericHost_Default8080()
    {
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);

        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(configure =>
        {
            configure.UseStartup<TestServerStartup>();
            configure.UseTestServer();
        });

        hostBuilder.UseCloudHosting();
        using IHost host = hostBuilder.Build();
        host.Start();
    }

    [Fact]
    public void UseCloudHosting_WebApplication_Default8080()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", null);
        Environment.SetEnvironmentVariable("PORT", null);
        Environment.SetEnvironmentVariable("SERVER_PORT", null);

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.UseCloudHosting();
        hostBuilder.WebHost.UseTestServer();
        using WebApplication host = hostBuilder.Build();
        host.Start();
    }

#pragma warning restore S2699 // Tests should include assertions
}
