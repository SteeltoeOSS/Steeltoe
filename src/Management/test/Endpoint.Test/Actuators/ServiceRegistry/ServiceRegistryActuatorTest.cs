// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.ServiceRegistry;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ServiceRegistry;

public sealed class ServiceRegistryActuatorTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "service-registry"
    };

    private static readonly Uri EndpointUri = new("http://localhost/actuator/service-registry");

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddServiceRegistryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => serviceProvider.GetRequiredService<ServiceRegistryEndpointMiddleware>();

        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddServiceRegistryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ServiceRegistryEndpointOptions options = serviceProvider.GetRequiredService<IOptions<ServiceRegistryEndpointOptions>>().Value;

        options.Enabled.Should().BeNull();
        options.Id.Should().Be("service-registry");
        options.Path.Should().Be("service-registry");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().BeEquivalentTo("GET", "POST");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/service-registry");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:ServiceRegistry:Enabled"] = "true",
            ["Management:Endpoints:ServiceRegistry:Id"] = "test-actuator-id",
            ["Management:Endpoints:ServiceRegistry:Path"] = "test-actuator-path",
            ["Management:Endpoints:ServiceRegistry:RequiredPermissions"] = "full",
            ["Management:Endpoints:ServiceRegistry:AllowedVerbs:0"] = "post"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddServiceRegistryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ServiceRegistryEndpointOptions options = serviceProvider.GetRequiredService<IOptions<ServiceRegistryEndpointOptions>>().Value;

        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/alt-actuators").Should().Be("/alt-actuators/test-actuator-path");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_status(HostBuilderType hostBuilderType)
    {
        var discoveryClient = new FakeServiceRegistryDiscoveryClient("test-client", true);

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDiscoveryClient>(discoveryClient);
                services.AddServiceRegistryActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(EndpointUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UP"
            }
            """);
    }

    [Fact]
    public async Task Endpoint_returns_status_per_registry_when_multiple_registered()
    {
        await using WebApplication host = await CreateHostAsync(new FakeServiceRegistryDiscoveryClient("first-client", true),
            new FakeServiceRegistryDiscoveryClient("second-client", true), new FakeServiceRegistryDiscoveryClient("unregistered-client", false));

        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(EndpointUri, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "first-client": {
                "status": "UP"
              },
              "second-client": {
                "status": "UP"
              }
            }
            """);
    }

    [Fact]
    public async Task Endpoint_returns_not_found_without_discovery_client()
    {
        await using WebApplication host = await CreateHostAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage getResponse = await httpClient.GetAsync(EndpointUri, TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage postResponse = await httpClient.PostAsync(EndpointUri, CreateJsonContent("""{"status":"OUT_OF_SERVICE"}"""),
            TestContext.Current.CancellationToken);

        postResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoint_returns_not_found_when_registration_disabled()
    {
        var discoveryClient = new FakeServiceRegistryDiscoveryClient("test-client", false);

        await using WebApplication host = await CreateHostAsync(discoveryClient);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage getResponse = await httpClient.GetAsync(EndpointUri, TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage postResponse = await httpClient.PostAsync(EndpointUri, CreateJsonContent("""{"status":"OUT_OF_SERVICE"}"""),
            TestContext.Current.CancellationToken);

        postResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        discoveryClient.Status.Should().Be("UP");
    }

    [Fact]
    public async Task Endpoint_changes_status_of_all_registries()
    {
        var firstDiscoveryClient = new FakeServiceRegistryDiscoveryClient("first-client", true);
        var secondDiscoveryClient = new FakeServiceRegistryDiscoveryClient("second-client", true);

        await using WebApplication host = await CreateHostAsync(firstDiscoveryClient, secondDiscoveryClient);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.PostAsync(EndpointUri, CreateJsonContent("""{"status":"OUT_OF_SERVICE"}"""),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().BeEmpty();

        firstDiscoveryClient.Status.Should().Be("OUT_OF_SERVICE");
        secondDiscoveryClient.Status.Should().Be("OUT_OF_SERVICE");
    }

    [Fact]
    public async Task Endpoint_returns_bad_request_for_unsupported_status()
    {
        var discoveryClient = new FakeServiceRegistryDiscoveryClient("test-client", true);

        await using WebApplication host = await CreateHostAsync(discoveryClient);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.PostAsync(EndpointUri, CreateJsonContent("""{"status":"BOGUS"}"""),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be($"Status 'BOGUS' is not supported by {nameof(FakeServiceRegistryDiscoveryClient)}.");

        discoveryClient.Status.Should().Be("UP");
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("""{"status":""}""")]
    [InlineData("""{"status":null}""")]
    [InlineData("{\"status\":")]
    public async Task Endpoint_returns_bad_request_for_missing_status(string requestBody)
    {
        var discoveryClient = new FakeServiceRegistryDiscoveryClient("test-client", true);

        await using WebApplication host = await CreateHostAsync(discoveryClient);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.PostAsync(EndpointUri, CreateJsonContent(requestBody), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be("The request body must contain a 'status' property.");

        discoveryClient.Status.Should().Be("UP");
    }

    private static async Task<WebApplication> CreateHostAsync(params IDiscoveryClient[] discoveryClients)
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);

        foreach (IDiscoveryClient discoveryClient in discoveryClients)
        {
            builder.Services.AddSingleton(discoveryClient);
        }

        builder.Services.AddServiceRegistryActuator();
        WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        return host;
    }

    private static StringContent CreateJsonContent(string json)
    {
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.spring-boot.actuator.v3+json");
        return content;
    }
}
