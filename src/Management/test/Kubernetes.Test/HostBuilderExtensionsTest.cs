// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Kubernetes.Test;

public sealed class HostBuilderExtensionsTest
{
    private static readonly Dictionary<string, string> AppSettings = new()
    {
        { "management:endpoints:actuator:exposure:include:0", "*" }
    };

    private readonly Action<IWebHostBuilder> _testServerWithRouting = builder => builder.UseTestServer()
        .ConfigureServices(s => s.AddRouting().AddActionDescriptorCollectionProvider()).Configure(a => a.UseRouting())
        .ConfigureAppConfiguration(b => b.AddInMemoryCollection(AppSettings));

    private readonly Action<IWebHostBuilder> _testServerWithSecureRouting = builder => builder.UseTestServer().ConfigureServices(s =>
    {
        s.AddRouting();
        s.AddActionDescriptorCollectionProvider();

        s.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme,
            _ =>
            {
            });

        s.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));
    }).Configure(a => a.UseRouting().UseAuthentication().UseAuthorization()).ConfigureAppConfiguration(b => b.AddInMemoryCollection(AppSettings));

    public HostBuilderExtensionsTest()
    {
        // Workaround for CryptographicException: PKCS12 (PFX) without a supplied password has exceeded maximum allowed iterations.
        // https://support.microsoft.com/en-us/topic/kb5025823-change-in-how-net-applications-import-x-509-certificates-bf81c936-af2b-446e-9f7a-016f4713b46b
        Environment.SetEnvironmentVariable("COMPlus_Pkcs12UnspecifiedPasswordIterationLimit", "-1");
    }

    [Fact]
    public async Task AddKubernetesActuators_IHostBuilder_AddsAndActivatesActuators()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddKubernetesActuators().StartAsync();
        HttpClient testClient = host.GetTestServer().CreateClient();

        await AssertActuatorResponsesAsync(testClient);
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

    private async Task AssertActuatorResponsesAsync(HttpClient testClient)
    {
        HttpResponseMessage response = await testClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await testClient.GetAsync(new Uri("/actuator/health/liveness", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        response = await testClient.GetAsync(new Uri("/actuator/health/readiness", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        response = await testClient.GetAsync(new Uri("/actuator/httptrace", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
