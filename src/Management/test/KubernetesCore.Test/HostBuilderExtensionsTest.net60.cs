// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Kubernetes.Test;

public partial class HostBuilderExtensionsTest
{
    [Fact]
    public async Task AddKubernetesActuators_WebApplicationBuilder_AddsAndActivatesActuators()
    {
        var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        var host = hostBuilder.AddKubernetesActuators().Build();
        host.UseRouting();
        await host.StartAsync();
        var testClient = host.GetTestServer().CreateClient();
        await AssertActuatorResponses(testClient);
    }

    [Fact]
    public async Task AddKubernetesActuators_WebApplicationBuilder_AddsAndActivatesActuators_MediaV1()
    {
        var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        var host = hostBuilder.AddKubernetesActuators(MediaTypeVersion.V1).Build();
        host.UseRouting();
        await host.StartAsync();
        var testClient = host.GetTestServer().CreateClient();
        await AssertActuatorResponses(testClient, MediaTypeVersion.V1);
    }

    [Fact]
    public async Task AddKubernetesActuatorsWithConventions_WebApplicationBuilder_AddsAndActivatesActuatorsAddAllActuators()
    {
        var host = GetTestWebAppWithSecureRouting(b => b.AddKubernetesActuators(ep => ep.RequireAuthorization("TestAuth")));
        await host.StartAsync();
        var testClient = host.GetTestServer().CreateClient();
        await AssertActuatorResponses(testClient);
    }

    private WebApplication GetTestWebAppWithSecureRouting(Action<WebApplicationBuilder> customizeBuilder = null)
    {
        var builder = TestHelpers.GetTestWebApplicationBuilder();
        customizeBuilder?.Invoke(builder);

        builder.Services.AddRouting();
        builder.Services
            .AddAuthentication(TestAuthHandler.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, _ => { });
        builder.Services.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));

        var app = builder.Build();
        app.UseRouting().UseAuthentication().UseAuthorization();
        return app;
    }
}
#endif
