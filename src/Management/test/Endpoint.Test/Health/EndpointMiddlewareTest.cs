// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test;

public class EndpointMiddlewareTest : BaseTest
{
    private readonly Dictionary<string, string> _appSettings = new()
    {
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:path"] = "/cloudfoundryapplication",
        ["management:endpoints:health:enabled"] = "true"
    };

    [Fact]
    public async Task HandleHealthRequestAsync_ReturnsExpected()
    {
        var opts = new HealthEndpointOptions();
        var managementOptions = new CloudFoundryManagementOptions();
        managementOptions.EndpointOptions.Add(opts);

        var contributors = new List<IHealthContributor>
        {
            new DiskSpaceContributor()
        };

        var ep = new TestHealthEndpoint(opts, new DefaultHealthAggregator(), contributors);

        var middle = new HealthEndpointMiddleware(null, managementOptions)
        {
            Endpoint = ep
        };

        HttpContext context = CreateRequest("GET", "/health");
        await middle.HandleHealthRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        string json = await rdr.ReadToEndAsync();
        Assert.Equal("{\"status\":\"UNKNOWN\"}", json);
    }

    [Fact]
    public async Task HealthActuator_ReturnsOnlyStatus()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"));
    }

    [Fact]
    public async Task HealthActuator_ReturnsOnlyStatusWhenAuthorized()
    {
        var settings = new Dictionary<string, string>(_appSettings)
        {
            { "management:endpoints:health:showdetails", "whenauthorized" }
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<AuthStartup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();

        HttpResponseMessage result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"));
    }

    [Fact]
    public async Task HealthActuator_ReturnsDetailsWhenAuthorized()
    {
        var settings = new Dictionary<string, string>(_appSettings)
        {
            { "management:endpoints:health:showdetails", "whenauthorized" },
            { "management:endpoints:health:claim:type", "healthdetails" },
            { "management:endpoints:health:claim:value", "show" }
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<AuthStartup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();

        HttpResponseMessage result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"));
        Assert.True(health.ContainsKey("details"));
        Assert.Contains("diskSpace", health["details"].ToString());
        Assert.True(health.ContainsKey("status"), "Health should contain key: status");
        Assert.True(health.ContainsKey("details"), "Health should contain key: details");
        Assert.Contains("diskSpace", health["details"].ToString());
    }

    [Fact]
    public async Task HealthActuator_ReturnsDetails()
    {
        var settings = new Dictionary<string, string>(_appSettings);

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        // { "status":"UP","diskSpace":{ "total":499581448192,"free":407577710592,"threshold":10485760,"status":"UP"} }
        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"), "Health should contain key: status");
        Assert.True(health.ContainsKey("details"), "Health should contain key: details");
        Assert.Contains("diskSpace", health["details"].ToString());
    }

    [Fact]
    public async Task HealthActuatorV3_ReturnsDetails()
    {
        var settings = new Dictionary<string, string>(_appSettings)
        {
            { "management:endpoints:customjsonconverters:0", typeof(HealthConverterV3).FullName }
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        // {"status":"UP","components":{"diskSpace":{"status":"UP","details":{"total":1003588939776,"free":597722619904,"threshold":10485760,"status":"UP"}},"readiness":{"status":"UNKNOWN","description":"Failed to get current availability state","details":{}}}}
        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"), "Health should contain key: status");
        Assert.False(health.ContainsKey("details"), "Health should not contain key: details");
        Assert.True(health.ContainsKey("components"), "Health should contain key: components");
        string componentString = health["components"].ToString() ?? string.Empty;
        Assert.Contains("diskSpace", componentString);
        Assert.Contains("details", componentString);
    }

    [Fact]
    public async Task HealthActuator_ReturnsMicrosoftHealthDetails()
    {
        var settings = new Dictionary<string, string>(_appSettings);

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);

        var health = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.NotNull(health);
        Assert.True(health.ContainsKey("status"));
        Assert.True(health.ContainsKey("details"));
        Assert.Contains("diskSpace", health["details"].ToString());
    }

    [Fact]
    public async Task TestDI()
    {
        var settings = new Dictionary<string, string>(_appSettings);

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        builder.ConfigureServices(services =>
        {
            services.BuildServiceProvider().GetServices<HealthEndpoint>();
            services.BuildServiceProvider().GetServices<HealthEndpointCore>();
            services.BuildServiceProvider().GetServices<IEndpoint<HealthCheckResult, ISecurityContext>>();
        });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);
    }

    [Fact]
    public async Task GetStatusCode_ReturnsExpected()
    {
        IWebHostBuilder builder = new WebHostBuilder().ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings))
            .UseStartup<Startup>();

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage result = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            string json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);
            Assert.Contains("\"status\":\"UP\"", json);
        }

        builder = new WebHostBuilder().ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string>(_appSettings)
            {
                ["HealthCheckType"] = "down"
            })).UseStartup<Startup>();

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage downResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, downResult.StatusCode);
            string downJson = await downResult.Content.ReadAsStringAsync();
            Assert.NotNull(downJson);
            Assert.Contains("\"status\":\"DOWN\"", downJson);
        }

        builder = new WebHostBuilder().ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string>(_appSettings)
            {
                ["HealthCheckType"] = "out"
            })).UseStartup<Startup>();

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage outResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, outResult.StatusCode);
            string outJson = await outResult.Content.ReadAsStringAsync();
            Assert.NotNull(outJson);
            Assert.Contains("\"status\":\"OUT_OF_SERVICE\"", outJson);
        }

        builder = new WebHostBuilder().UseStartup<Startup>().ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string>(_appSettings)
            {
                ["HealthCheckType"] = "unknown"
            }));

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage unknownResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, unknownResult.StatusCode);
            string unknownJson = await unknownResult.Content.ReadAsStringAsync();
            Assert.NotNull(unknownJson);
            Assert.Contains("\"status\":\"UNKNOWN\"", unknownJson);
        }

        builder = new WebHostBuilder().UseStartup<Startup>().ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string>(_appSettings)
            {
                ["HealthCheckType"] = "defaultAggregator"
            }));

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage unknownResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, unknownResult.StatusCode);
            string unknownJson = await unknownResult.Content.ReadAsStringAsync();
            Assert.NotNull(unknownJson);
            Assert.Contains("\"status\":\"UP\"", unknownJson);
        }

        builder = new WebHostBuilder().UseStartup<Startup>().ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string>(_appSettings)
            {
                ["HealthCheckType"] = "down",
                ["management:endpoints:UseStatusCodeFromResponse"] = "false"
            }));

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage downResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
            Assert.Equal(HttpStatusCode.OK, downResult.StatusCode);
            string downJson = await downResult.Content.ReadAsStringAsync();
            Assert.NotNull(downJson);
            Assert.Contains("\"status\":\"DOWN\"", downJson);
        }
    }

    [Fact]
    public async Task GetStatusCode_MicrosoftAggregator_ReturnsExpected()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>().ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string>(_appSettings)
            {
                ["HealthCheckType"] = "microsoftHealthAggregator"
            }));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage unknownResult = await client.GetAsync("http://localhost/cloudfoundryapplication/health");
        Assert.Equal(HttpStatusCode.OK, unknownResult.StatusCode);
        string unknownJson = await unknownResult.Content.ReadAsStringAsync();
        Assert.NotNull(unknownJson);
        Assert.Contains("\"status\":\"UP\"", unknownJson);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = new HealthEndpointOptions();
        Assert.False(options.ExactMatch);
        Assert.Equal("/actuator/health/{**_}", options.GetContextPath(new ActuatorManagementOptions()));
        Assert.Equal("/cloudfoundryapplication/health/{**_}", options.GetContextPath(new CloudFoundryManagementOptions()));
        Assert.Null(options.AllowedVerbs);
    }

    private HttpContext CreateRequest(string method, string path)
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Response.Body = new MemoryStream();
        context.Request.Method = method;
        context.Request.Path = new PathString(path);
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        return context;
    }
}
