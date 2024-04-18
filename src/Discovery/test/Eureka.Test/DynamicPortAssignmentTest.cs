// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class DynamicPortAssignmentTest
{
    [Fact]
    public async Task Applies_dynamically_assigned_ports_after_startup()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Instance:InstanceId"] = "DEMO"
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting("urls", "http://*:0;https://*:0");
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();
        await app.StartAsync();

        var infoManager = app.Services.GetRequiredService<EurekaApplicationInfoManager>();

        infoManager.Instance.IsNonSecurePortEnabled.Should().BeTrue();
        infoManager.Instance.NonSecurePort.Should().BeGreaterThan(40_000);
        infoManager.Instance.IsSecurePortEnabled.Should().BeTrue();
        infoManager.Instance.SecurePort.Should().BeGreaterThan(40_000);
    }
}
