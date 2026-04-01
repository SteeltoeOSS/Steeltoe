// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class DynamicPortAssignmentTest
{
    [FactSkippedOnPlatform(nameof(OSPlatform.OSX))]
    public async Task Applies_dynamically_assigned_ports_after_startup()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.WebHost.UseSetting("urls", "http://*:0;https://*:0");
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var infoManager = app.Services.GetRequiredService<EurekaApplicationInfoManager>();

        infoManager.Instance.IsNonSecurePortEnabled.Should().BeTrue();
        infoManager.Instance.NonSecurePort.Should().NotBe(5000);
        infoManager.Instance.NonSecurePort.Should().BePositive();
        infoManager.Instance.SecurePort.Should().NotBe(5001);
        infoManager.Instance.IsSecurePortEnabled.Should().BeTrue();
        infoManager.Instance.SecurePort.Should().BePositive();
    }

    [Fact]
    public async Task Applies_dynamically_assigned_ports_when_kestrel_overrides_urls_config()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["urls"] = "http://*:5000"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(0);
        });

        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var infoManager = app.Services.GetRequiredService<EurekaApplicationInfoManager>();

        infoManager.Instance.IsNonSecurePortEnabled.Should().BeTrue();
        infoManager.Instance.NonSecurePort.Should().NotBe(5000);
        infoManager.Instance.NonSecurePort.Should().BePositive();
    }

    [Fact]
    public async Task Does_not_override_explicitly_configured_secure_port()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:SecurePort"] = "443",
            ["Eureka:Instance:SecurePortEnabled"] = "true"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.WebHost.UseSetting("urls", "http://*:0");
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var infoManager = app.Services.GetRequiredService<EurekaApplicationInfoManager>();

        infoManager.Instance.IsSecurePortEnabled.Should().BeTrue();
        infoManager.Instance.SecurePort.Should().Be(443);
        infoManager.Instance.IsNonSecurePortEnabled.Should().BeFalse();
        infoManager.Instance.NonSecurePort.Should().Be(0);
    }

    [Fact]
    public async Task Does_not_override_explicitly_configured_non_secure_port()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:Port"] = "80",
            ["Eureka:Instance:NonSecurePortEnabled"] = "true"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.WebHost.UseSetting("urls", "http://*:0");
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var infoManager = app.Services.GetRequiredService<EurekaApplicationInfoManager>();

        infoManager.Instance.IsNonSecurePortEnabled.Should().BeTrue();
        infoManager.Instance.NonSecurePort.Should().Be(80);
        infoManager.Instance.IsSecurePortEnabled.Should().BeFalse();
        infoManager.Instance.SecurePort.Should().Be(0);
    }

    [Fact]
    public async Task Does_not_override_ports_configured_via_code()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.WebHost.UseSetting("urls", "http://*:0");
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        builder.Services.Configure<EurekaInstanceOptions>(options =>
        {
            options.SecurePort = 8443;
            options.IsSecurePortEnabled = true;
        });

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var infoManager = app.Services.GetRequiredService<EurekaApplicationInfoManager>();

        infoManager.Instance.IsSecurePortEnabled.Should().BeTrue();
        infoManager.Instance.SecurePort.Should().Be(8443);
        infoManager.Instance.IsNonSecurePortEnabled.Should().BeFalse();
        infoManager.Instance.NonSecurePort.Should().Be(0);
    }

    [Fact]
    public async Task Does_not_apply_dynamic_ports_when_UseAspNetCoreUrls_is_false()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:UseAspNetCoreUrls"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.WebHost.UseSetting("urls", "http://*:0");
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var infoManager = app.Services.GetRequiredService<EurekaApplicationInfoManager>();

        infoManager.Instance.IsNonSecurePortEnabled.Should().BeFalse();
        infoManager.Instance.NonSecurePort.Should().Be(0);
        infoManager.Instance.IsSecurePortEnabled.Should().BeFalse();
        infoManager.Instance.SecurePort.Should().Be(0);
    }
}
