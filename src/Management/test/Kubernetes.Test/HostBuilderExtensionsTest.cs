// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Kubernetes.Test;

public class HostBuilderExtensionsTest
{
    private readonly Action<IWebHostBuilder> _testServerWithRouting = builder =>
        builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());

    private readonly Action<IWebHostBuilder> _testServerWithSecureRouting = builder => builder.UseTestServer().ConfigureServices(s =>
    {
        s.AddRouting();

        s.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme,
            _ =>
            {
            });

        s.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));
    }).Configure(a => a.UseRouting().UseAuthentication().UseAuthorization());

    [Fact]
    public async Task AddKubernetesActuators_IHostBuilder_AddsAndActivatesActuators()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddKubernetesActuators().StartAsync();
        HttpClient testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponsesAsync(testClient);
    }

    [Fact]
    public async Task AddKubernetesActuators_IHostBuilder_AddsAndActivatesActuators_MediaV1()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddKubernetesActuators(MediaTypeVersion.V1).StartAsync();
        HttpClient testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponsesAsync(testClient, MediaTypeVersion.V1);
    }

    [Fact]
    public async Task AddKubernetesActuatorsWithConventions_IHostBuilder_AddsAndActivatesActuatorsAddAllActuators()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithSecureRouting);

        using IHost host = await hostBuilder.AddKubernetesActuators(ep => ep.RequireAuthorization("TestAuth")).StartAsync();
        HttpClient testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponsesAsync(testClient);
    }

    [Fact]
    public async Task AddKubernetesActuators_IWebHostBuilder_AddsAndActivatesActuators()
    {
        var hostBuilder = new WebHostBuilder();
        _testServerWithRouting.Invoke(hostBuilder);

        using IWebHost host = hostBuilder.AddKubernetesActuators().Start();
        HttpClient testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponsesAsync(testClient);
    }

    [Fact]
    public async Task AddKubernetesActuators_IWebHostBuilder_AddsAndActivatesActuators_MediaV1()
    {
        var hostBuilder = new WebHostBuilder();
        _testServerWithRouting.Invoke(hostBuilder);

        using IWebHost host = hostBuilder.AddKubernetesActuators(MediaTypeVersion.V1).Start();
        HttpClient testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponsesAsync(testClient, MediaTypeVersion.V1);
    }

    [Fact]
    public async Task AddKubernetesActuatorsWithConventions_IWebHostBuilder_AddsAndActivatesActuatorsAddAllActuators()
    {
        var hostBuilder = new WebHostBuilder();
        _testServerWithSecureRouting.Invoke(hostBuilder);

        using IWebHost host = hostBuilder.AddKubernetesActuators(ep => ep.RequireAuthorization("TestAuth")).Start();
        HttpClient testClient = host.GetTestServer().CreateClient();
        await AssertActuatorResponsesAsync(testClient);
    }

    [Fact]
    public async Task AddKubernetesActuators_WebApplicationBuilder_AddsAndActivatesActuators()
    {
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        await using WebApplication host = hostBuilder.AddKubernetesActuators().Build();

        host.UseRouting();
        await host.StartAsync();
        HttpClient testClient = host.GetTestServer().CreateClient();
        await AssertActuatorResponsesAsync(testClient);
    }

    [Fact]
    public async Task AddKubernetesActuators_WebApplicationBuilder_AddsAndActivatesActuators_MediaV1()
    {
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        await using WebApplication host = hostBuilder.AddKubernetesActuators(MediaTypeVersion.V1).Build();

        host.UseRouting();
        await host.StartAsync();
        HttpClient testClient = host.GetTestServer().CreateClient();
        await AssertActuatorResponsesAsync(testClient, MediaTypeVersion.V1);
    }

    [Fact]
    public async Task AddKubernetesActuatorsWithConventions_WebApplicationBuilder_AddsAndActivatesActuatorsAddAllActuators()
    {
        await using WebApplication host = GetTestWebAppWithSecureRouting(b => b.AddKubernetesActuators(ep => ep.RequireAuthorization("TestAuth")));

        await host.StartAsync();
        HttpClient testClient = host.GetTestServer().CreateClient();
        await AssertActuatorResponsesAsync(testClient);
    }

    private WebApplication GetTestWebAppWithSecureRouting(Action<WebApplicationBuilder> customizeBuilder = null)
    {
        WebApplicationBuilder builder = TestHelpers.GetTestWebApplicationBuilder();
        customizeBuilder?.Invoke(builder);

        builder.Services.AddRouting();

        builder.Services.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            TestAuthHandler.AuthenticationScheme, _ =>
            {
            });

        builder.Services.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));

        WebApplication app = builder.Build();
        app.UseRouting().UseAuthentication().UseAuthorization();
        return app;
    }

    private async Task AssertActuatorResponsesAsync(HttpClient testClient, MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
    {
        HttpResponseMessage response = await testClient.GetAsync("/actuator");
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
}
