// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Kubernetes.Test;

public class HostBuilderExtensionsTest
{
    [Fact]
    public async Task AddKubernetesActuators_IHostBuilder_AddsAndActivatesActuators()
    {
        var hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        var host = await hostBuilder.AddKubernetesActuators().StartAsync();
        var testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponses(testClient);
    }

    [Fact]
    public async Task AddKubernetesActuators_IHostBuilder_AddsAndActivatesActuators_MediaV1()
    {
        var hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        var host = await hostBuilder.AddKubernetesActuators(MediaTypeVersion.V1).StartAsync();
        var testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponses(testClient, MediaTypeVersion.V1);
    }

    [Fact]
    public async Task AddKubernetesActuatorsWithConventions_IHostBuilder_AddsAndActivatesActuatorsAddAllActuators()
    {
        var hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithSecureRouting);

        var host = await hostBuilder.AddKubernetesActuators(ep => ep.RequireAuthorization("TestAuth")).StartAsync();
        var testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponses(testClient);
    }

    [Fact]
    public async Task AddKubernetesActuators_IWebHostBuilder_AddsAndActivatesActuators()
    {
        var hostBuilder = new WebHostBuilder();
        _testServerWithRouting.Invoke(hostBuilder);

        var host = hostBuilder.AddKubernetesActuators().Start();
        var testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponses(testClient);
    }

    [Fact]
    public async Task AddKubernetesActuators_IWebHostBuilder_AddsAndActivatesActuators_MediaV1()
    {
        var hostBuilder = new WebHostBuilder();
        _testServerWithRouting.Invoke(hostBuilder);

        var host = hostBuilder.AddKubernetesActuators(MediaTypeVersion.V1).Start();
        var testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponses(testClient, MediaTypeVersion.V1);
    }

    [Fact]
    public async Task AddKubernetesActuatorsWithConventions_IWebHostBuilder_AddsAndActivatesActuatorsAddAllActuators()
    {
        var hostBuilder = new WebHostBuilder();
        _testServerWithSecureRouting.Invoke(hostBuilder);

        var host = hostBuilder.AddKubernetesActuators(ep => ep.RequireAuthorization("TestAuth")).Start();
        var testClient = host.GetTestServer().CreateClient();
        await AssertActuatorResponses(testClient);
    }

#if NET6_0_OR_GREATER
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
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
        builder.Services.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));

        var app = builder.Build();
        app.UseRouting().UseAuthentication().UseAuthorization();
        return app;
    }
#endif

    private async Task AssertActuatorResponses(HttpClient testClient, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
    {
        var response = await testClient.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync("/actuator/health/liveness");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync());
        response = await testClient.GetAsync("/actuator/health/readiness");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync());
        if (mediaTypeVersion == MediaTypeVersion.V1)
        {
            response = await testClient.GetAsync("/actuator/trace");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        else
        {
            response = await testClient.GetAsync("/actuator/httptrace");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private readonly Action<IWebHostBuilder> _testServerWithRouting = builder => builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());
    private readonly Action<IWebHostBuilder> _testServerWithSecureRouting =
        builder => builder.UseTestServer()
            .ConfigureServices(s =>
            {
                s.AddRouting();
                s.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
                s.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));
            })
            .Configure(a => a.UseRouting().UseAuthentication().UseAuthorization());
}