// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class EndpointMiddlewareTest : BaseTest
{
    // Allow routing to /cloudfoundryapplication
    private readonly EnvironmentVariableScope _scope = new("VCAP_APPLICATION", "{}");

    [Fact]
    public void RoutesByPathAndVerb()
    {
        HypermediaEndpointOptions endpointOptions = GetOptionsMonitorFromSettings<HypermediaEndpointOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/cloudfoundryapplication", endpointOptions.GetPathMatchPattern(ConfigureManagementOptions.DefaultCloudFoundryPath));
        Assert.Single(endpointOptions.AllowedVerbs);
        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }

    [Fact]
    public async Task CloudFoundryEndpointMiddleware_ReturnsExpectedData()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        var links = await client.GetFromJsonAsync<Links>("http://localhost/cloudfoundryapplication", SerializerOptions, TestContext.Current.CancellationToken);

        links.Should().NotBeNull();
        links.Entries["beans"].Href.Should().Be("http://localhost/cloudfoundryapplication/beans");
        links.Entries["dbmigrations"].Href.Should().Be("http://localhost/cloudfoundryapplication/dbmigrations");
        links.Entries["env"].Href.Should().Be("http://localhost/cloudfoundryapplication/env");
        links.Entries["health"].Href.Should().Be("http://localhost/cloudfoundryapplication/health");
        links.Entries["heapdump"].Href.Should().Be("http://localhost/cloudfoundryapplication/heapdump");
        links.Entries["httpexchanges"].Href.Should().Be("http://localhost/cloudfoundryapplication/httpexchanges");
        links.Entries["info"].Href.Should().Be("http://localhost/cloudfoundryapplication/info");
        links.Entries["refresh"].Href.Should().Be("http://localhost/cloudfoundryapplication/refresh");
        links.Entries["mappings"].Href.Should().Be("http://localhost/cloudfoundryapplication/mappings");
        links.Entries["loggers"].Href.Should().Be("http://localhost/cloudfoundryapplication/loggers");
        links.Entries["self"].Href.Should().Be("http://localhost/cloudfoundryapplication");
        links.Entries["threaddump"].Href.Should().Be("http://localhost/cloudfoundryapplication/threaddump");
        links.Entries.Should().HaveCount(12);
    }

    [Fact]
    public async Task CloudFoundryEndpointMiddleware_ServiceContractNotBroken()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication"), TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        json.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "threaddump": {
                  "href": "http://localhost/cloudfoundryapplication/threaddump",
                  "templated": false
                },
                "heapdump": {
                  "href": "http://localhost/cloudfoundryapplication/heapdump",
                  "templated": false
                },
                "dbmigrations": {
                  "href": "http://localhost/cloudfoundryapplication/dbmigrations",
                  "templated": false
                },
                "env": {
                  "href": "http://localhost/cloudfoundryapplication/env",
                  "templated": false
                },
                "info": {
                  "href": "http://localhost/cloudfoundryapplication/info",
                  "templated": false
                },
                "health": {
                  "href": "http://localhost/cloudfoundryapplication/health",
                  "templated": true
                },
                "loggers": {
                  "href": "http://localhost/cloudfoundryapplication/loggers",
                  "templated": true
                },
                "httpexchanges": {
                  "href": "http://localhost/cloudfoundryapplication/httpexchanges",
                  "templated": false
                },
                "mappings": {
                  "href": "http://localhost/cloudfoundryapplication/mappings",
                  "templated": false
                },
                "refresh": {
                  "href": "http://localhost/cloudfoundryapplication/refresh",
                  "templated": false
                },
                "beans": {
                  "href": "http://localhost/cloudfoundryapplication/beans",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/cloudfoundryapplication",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task CloudFoundryOptions_UseDefaultJsonSerializerOptions()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        string response = await client.GetStringAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);

        response.Should().Contain("2017-07-12T18:40:39Z");
        response.Should().Contain("2017-06-08T12:47:02Z");
    }

    [Fact]
    public async Task CloudFoundryOptions_UseCustomJsonSerializerOptions()
    {
        Dictionary<string, string?> settings = new()
        {
            ["management:endpoints:CustomJsonConverters:0"] = "Steeltoe.Management.Endpoint.Actuators.Info.EpochSecondsDateTimeConverter"
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        string response = await client.GetStringAsync(new Uri("http://localhost/cloudfoundryapplication/info"), TestContext.Current.CancellationToken);

        response.Should().Contain("1499884839000");
        response.Should().NotContain("2017-07-12T18:40:39Z");
        response.Should().Contain("1496926022000");
        response.Should().NotContain("2017-06-08T12:47:02Z");
    }

    [Fact]
    public async Task ThrowsForMissingSecurityMiddlewareOnCloudFoundry()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddInfoActuator();
        await using WebApplication app = builder.Build();

        Func<Task> action = async () => await app.StartAsync(TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("Running on Cloud Foundry without security middleware. Call services.AddCloudFoundryActuator() to fix this.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
    }
}
