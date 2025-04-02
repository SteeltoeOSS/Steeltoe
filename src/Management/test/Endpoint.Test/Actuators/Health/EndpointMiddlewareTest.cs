// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private readonly Dictionary<string, string?> _appSettings = new()
    {
        ["management:endpoints:path"] = "/cloudfoundryapplication"
    };

    [Fact]
    public async Task HealthActuator_ReturnsOnlyStatus()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings));

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().NotContainKey("components");
    }

    [Fact]
    public async Task HealthActuator_ReturnsOnlyStatus_WhenAuthorizedSetButUserIsNot()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "whenAuthorized"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<AuthStartup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull().And.ContainKey("status").And.NotContainKey("components");
    }

    [Fact]
    public async Task HealthActuator_ReturnsComponentsWhenAuthorized()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "whenAuthorized",
            ["Management:Endpoints:Health:Claim:Type"] = "health-details",
            ["Management:Endpoints:Health:Claim:Value"] = "show"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<AuthStartup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().ContainKey("components");
        health["components"].ToString().Should().Contain("diskSpace").And.NotContain("details");
    }

    [Fact]
    public async Task HealthActuator_ReturnsComponentsAndDetailsWhenConfigured()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:ShowDetails"] = "Always"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().ContainKey("components");
        string componentString = health["components"].ToString() ?? string.Empty;
        componentString.Should().Contain("diskSpace");
        componentString.Should().Contain("details");
    }

    [Fact]
    public async Task HealthActuator_ReturnsMicrosoftHealthComponentsWithoutDetailsAsConfigured()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["HealthCheckType"] = "default",
            ["Management:Endpoints:Health:ShowComponents"] = "Always"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().ContainKey("components");
        var components = JsonSerializer.Deserialize<Dictionary<string, object>>(health["components"].ToString()!);
        components.Should().ContainKey("diskSpace");
        components.Should().ContainKey("test-registration");
        components["test-registration"].ToString().Should().NotContain("\"tags\":[\"test-tag-1\",\"test-tag-2\"]");
    }

    [Fact]
    public async Task HealthActuator_ReturnsMicrosoftHealthComponentsAndDetailsWhenConfigured()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["HealthCheckType"] = "default",
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:ShowDetails"] = "Always"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().ContainKey("components");
        var components = JsonSerializer.Deserialize<Dictionary<string, object>>(health["components"].ToString()!);
        components.Should().ContainKey("diskSpace");
        components.Should().ContainKey("test-registration");
        components["test-registration"].ToString().Should().Contain("\"tags\":[\"test-tag-1\",\"test-tag-2\"]");
    }

    [Fact]
    public async Task HealthActuator_ReturnsNoDiskSpaceDetailsWhenDisabled()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:DiskSpace:Enabled"] = "false"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().ContainKey("components");
        health["components"].ToString().Should().NotContain("diskSpace");
    }

    [Fact]
    public async Task TestDI()
    {
        var settings = new Dictionary<string, string?>(_appSettings);

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        builder.ConfigureServices(services =>
        {
            using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
            serviceProvider.GetServices<HealthEndpointHandler>();
            serviceProvider.GetServices<IEndpointHandler<HealthEndpointRequest, HealthEndpointResponse>>();
        });

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null, HttpStatusCode.OK, "\"status\":\"UP\"")]
    [InlineData("down", HttpStatusCode.ServiceUnavailable, "\"status\":\"DOWN\"")]
    [InlineData("out", HttpStatusCode.ServiceUnavailable, "\"status\":\"OUT_OF_SERVICE\"")]
    [InlineData("unknown", HttpStatusCode.OK, "\"status\":\"UNKNOWN\"")]
    [InlineData("disabled", HttpStatusCode.OK, "\"status\":\"UNKNOWN\"")]
    [InlineData("default", HttpStatusCode.OK, "\"status\":\"UP\"")]
    public async Task GetStatusCode_ReturnsExpected(string? healthCheckType, HttpStatusCode expectedStatusCode, string expectedJson)
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>(_appSettings);

            if (healthCheckType != null)
            {
                settings.Add("HealthCheckType", healthCheckType);
            }

            configuration.AddInMemoryCollection(settings);
        });

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(expectedStatusCode);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().Contain(expectedJson);
    }

    [Fact]
    public async Task GetStatusCode_OverrideUseStatusCodeFromResponseInConfiguration_ReturnsExpected()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>(_appSettings)
            {
                ["HealthCheckType"] = "down",
                ["Management:Endpoints:UseStatusCodeFromResponse"] = "false"
            };

            configuration.AddInMemoryCollection(settings);
        });

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().Be("""{"status":"DOWN"}""");
    }

    [Fact]
    public async Task GetStatusCode_OverrideUseStatusCodeFromResponseInHeader_ReturnsExpected()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>(_appSettings)
            {
                ["HealthCheckType"] = "down",
                ["Management:Endpoints:UseStatusCodeFromResponse"] = "false"
            };

            configuration.AddInMemoryCollection(settings);
        });

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/cloudfoundryapplication/health"));
        requestMessage.Headers.Add("X-Use-Status-Code-From-Response", "true");

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.SendAsync(requestMessage, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().Be("""{"status":"DOWN"}""");
    }

    [Fact]
    public async Task GetStatusCode_MicrosoftAggregator_ReturnsExpected()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>(_appSettings)
        {
            ["HealthCheckType"] = "default"
        }));

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage responseMessage = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"), TestContext.Current.CancellationToken);
        responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await responseMessage.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        json.Should().Be("""{"status":"UP"}""");
    }

    [Fact]
    public async Task GetStatusCode_InvalidGroupName_Returns404()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage responseMessage = await client.GetAsync(new Uri("http://localhost/actuator/health/foo"), TestContext.Current.CancellationToken);
        responseMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);

        string body = await responseMessage.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().BeEmpty();
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<HealthEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        endpointOptions.RequiresExactMatch().Should().BeFalse();
        endpointOptions.GetPathMatchPattern(managementOptions.Path).Should().Be("/actuator/health/{**_}");
        endpointOptions.GetPathMatchPattern(ConfigureManagementOptions.DefaultCloudFoundryPath).Should().Be("/cloudfoundryapplication/health/{**_}");
        endpointOptions.AllowedVerbs.Should().ContainSingle("Get");
    }
}
