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
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:path"] = "/cloudfoundryapplication",
        ["management:endpoints:health:enabled"] = "true"
    };

    [Fact]
    public async Task HealthActuator_ReturnsOnlyStatus()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().NotContainKey("components");
        health.Should().NotContainKey("details");
    }

    [Fact]
    public async Task HealthActuator_ReturnsOnlyStatus_WhenAuthorizedSetButUserIsNot()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["Management:Endpoints:Health:ShowDetails"] = "whenAuthorized"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<AuthStartup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().NotContainKey("components");
        health.Should().NotContainKey("details");
    }

    [Fact]
    public async Task HealthActuator_ReturnsDetailsWhenAuthorized()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["Management:Endpoints:Health:ShowDetails"] = "whenAuthorized",
            ["Management:Endpoints:Health:Claim:Type"] = "health-details",
            ["Management:Endpoints:Health:Claim:Value"] = "show"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<AuthStartup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().ContainKey("details");
        health!["details"].ToString().Should().Contain("diskSpace");
    }

    [Fact]
    public async Task HealthActuator_ReturnsDetailsWhenConfigured()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["Management:Endpoints:Health:ShowDetails"] = "Always"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNull();

        // { "status":"UP","diskSpace":{ "total":499581448192,"free":407577710592,"threshold":10485760,"status":"UP"} }
        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().ContainKey("details");
        health!["details"].ToString().Should().Contain("diskSpace");
    }

    [Fact]
    public async Task HealthActuatorV3_ReturnsDetailsWhenConfigured()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["Management:Endpoints:CustomJsonConverters:0"] = typeof(HealthConverterV3).FullName!,
            ["Management:Endpoints:Health:ShowDetails"] = "Always"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNull();

        // {"status":"UP","components":{"diskSpace":{"status":"UP","details":{"total":1003588939776,"free":597722619904,"threshold":10485760,"status":"UP"}},"readiness":{"status":"UNKNOWN","description":"Failed to get current availability state","details":{}}}}
        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().NotContainKey("details");
        health.Should().ContainKey("components");
        string componentString = health!["components"].ToString() ?? string.Empty;
        componentString.Should().Contain("diskSpace");
        componentString.Should().Contain("details");
    }

    [Fact]
    public async Task HealthActuator_ReturnsMicrosoftHealthDetailsWhenConfigured()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["HealthCheckType"] = "default",
            ["Management:Endpoints:Health:ShowDetails"] = "Always"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().ContainKey("details");
        var details = JsonSerializer.Deserialize<Dictionary<string, object>>(health!["details"].ToString()!);
        details.Should().ContainKey("diskSpace");
        details.Should().ContainKey("test-registration");
        details!["test-registration"].ToString().Should().Contain("\"tags\":[\"test-tag-1\",\"test-tag-2\"]");
    }

    [Fact]
    public async Task HealthActuator_ReturnsNoDiskSpaceDetailsWhenDisabled()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["Management:Endpoints:Health:ShowDetails"] = "Always",
            ["Management:Endpoints:Health:DiskSpace:Enabled"] = "false"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNull();

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        health.Should().NotBeNull();
        health.Should().ContainKey("status");
        health.Should().ContainKey("details");
        health!["details"].ToString().Should().NotContain("diskSpace");
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
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
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
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        response.StatusCode.Should().Be(expectedStatusCode);

        string json = await response.Content.ReadAsStringAsync();
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
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
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
        await app.StartAsync();

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/cloudfoundryapplication/health"));
        requestMessage.Headers.Add("X-Use-Status-Code-From-Response", "true");

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.SendAsync(requestMessage);
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string json = await response.Content.ReadAsStringAsync();
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
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage responseMessage = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await responseMessage.Content.ReadAsStringAsync();
        json.Should().Be("""{"status":"UP"}""");
    }

    [Fact]
    public async Task GetStatusCode_InvalidGroupName_Returns404()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage responseMessage = await client.GetAsync(new Uri("http://localhost/actuator/health/foo"));
        responseMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);

        string body = await responseMessage.Content.ReadAsStringAsync();
        body.Should().BeEmpty();
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<HealthEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        endpointOptions.RequiresExactMatch().Should().BeFalse();
        endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path).Should().Be("/actuator/health/{**_}");

        endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath).Should()
            .Be("/cloudfoundryapplication/health/{**_}");

        endpointOptions.AllowedVerbs.Should().ContainSingle("Get");
    }
}
