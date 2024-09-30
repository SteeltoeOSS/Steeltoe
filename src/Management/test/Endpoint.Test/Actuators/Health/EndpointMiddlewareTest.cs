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
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"));
    }

    [Fact]
    public async Task HealthActuator_ReturnsOnlyStatusWhenAuthorized()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            { "management:endpoints:health:showDetails", "whenAuthorized" }
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<AuthStartup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"));
    }

    [Fact]
    public async Task HealthActuator_ReturnsDetailsWhenAuthorized()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            { "management:endpoints:health:showDetails", "whenAuthorized" },
            { "management:endpoints:health:claim:type", "health-details" },
            { "management:endpoints:health:claim:value", "show" }
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<AuthStartup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"));
        Assert.True(health.ContainsKey("details"));
        Assert.Contains("diskSpace", health["details"].ToString(), StringComparison.Ordinal);
        Assert.True(health.ContainsKey("status"), "Health should contain key: status");
        Assert.True(health.ContainsKey("details"), "Health should contain key: details");
        Assert.Contains("diskSpace", health["details"].ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HealthActuator_ReturnsDetails()
    {
        var settings = new Dictionary<string, string?>(_appSettings);

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        // { "status":"UP","diskSpace":{ "total":499581448192,"free":407577710592,"threshold":10485760,"status":"UP"} }
        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"), "Health should contain key: status");
        Assert.True(health.ContainsKey("details"), "Health should contain key: details");
        Assert.Contains("diskSpace", health["details"].ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HealthActuatorV3_ReturnsDetails()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            { "management:endpoints:customJsonConverters:0", typeof(HealthConverterV3).FullName! }
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        // {"status":"UP","components":{"diskSpace":{"status":"UP","details":{"total":1003588939776,"free":597722619904,"threshold":10485760,"status":"UP"}},"readiness":{"status":"UNKNOWN","description":"Failed to get current availability state","details":{}}}}
        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"), "Health should contain key: status");
        Assert.False(health.ContainsKey("details"), "Health should not contain key: details");
        Assert.True(health.ContainsKey("components"), "Health should contain key: components");
        string componentString = health["components"].ToString() ?? string.Empty;
        Assert.Contains("diskSpace", componentString, StringComparison.Ordinal);
        Assert.Contains("details", componentString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HealthActuator_ReturnsMicrosoftHealthDetails()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["HealthCheckType"] = "default"
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"));
        Assert.True(health.ContainsKey("details"));
        Assert.Contains("diskSpace", health["details"].ToString(), StringComparison.Ordinal);
        Assert.Contains("test-registration", health["details"].ToString(), StringComparison.Ordinal);
        Assert.Contains("test-tag-2", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HealthActuator_ReturnsNoDiskSpaceDetailsWhenDisabled()
    {
        var settings = new Dictionary<string, string?>(_appSettings)
        {
            ["management:endpoints:health:diskSpace:enabled"] = "false"
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"));
        Assert.True(health.ContainsKey("details"));
        Assert.DoesNotContain("diskSpace", health["details"].ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TestDI()
    {
        var settings = new Dictionary<string, string?>(_appSettings);

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        Assert.NotNull(json);
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
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
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
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>(_appSettings)
            {
                ["HealthCheckType"] = "down",
                ["management:endpoints:UseStatusCodeFromResponse"] = "false"
            };

            configuration.AddInMemoryCollection(settings);
        });

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();

        json.Should().Contain("""
            "status":"DOWN"
            """);
    }

    [Fact]
    public async Task GetStatusCode_OverrideUseStatusCodeFromResponseInHeader_ReturnsExpected()
    {
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>(_appSettings)
            {
                ["HealthCheckType"] = "down",
                ["management:endpoints:UseStatusCodeFromResponse"] = "false"
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

        json.Should().Contain("""
            "status":"DOWN"
            """);
    }

    [Fact]
    public async Task GetStatusCode_MicrosoftAggregator_ReturnsExpected()
    {
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>(_appSettings)
        {
            ["HealthCheckType"] = "default"
        }));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage unknownResult = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/health"));
        Assert.Equal(HttpStatusCode.OK, unknownResult.StatusCode);

        string unknownJson = await unknownResult.Content.ReadAsStringAsync();
        Assert.NotNull(unknownJson);
        Assert.Contains("\"status\":\"UP\"", unknownJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetStatusCode_InvalidGroupName_Returns404()
    {
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage responseMessage = await client.GetAsync(new Uri("http://localhost/actuator/health/foo"));
        Assert.Equal(HttpStatusCode.NotFound, responseMessage.StatusCode);

        string body = await responseMessage.Content.ReadAsStringAsync();
        Assert.Empty(body);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<HealthEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.False(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/health/{**_}", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));

        Assert.Equal("/cloudfoundryapplication/health/{**_}",
            endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));

        Assert.Single(endpointOptions.AllowedVerbs);
        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }
}
