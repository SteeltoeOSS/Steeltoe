// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Hypermedia;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private readonly Dictionary<string, string?> _appSettings = new()
    {
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:info:enabled"] = "true",
        ["info:application:name"] = "foobar",
        ["info:application:version"] = "1.0.0",
        ["info:application:date"] = "5/1/2008",
        ["info:application:time"] = "8:30:52 AM",
        ["info:NET:type"] = "Core",
        ["info:NET:version"] = "2.0.0",
        ["info:NET:ASPNET:type"] = "Core",
        ["info:NET:ASPNET:version"] = "2.0.0"
    };

    [Theory]
    [InlineData("http://somehost:1234", "https://somehost:1234", "https")]
    [InlineData("http://somehost:443", "https://somehost", "https")]
    [InlineData("http://somehost:80", "http://somehost", "http")]
    [InlineData("http://somehost:8080", "http://somehost:8080", "http")]
    public async Task CloudFoundryEndpointMiddleware_ReturnsExpectedData(string requestUriString, string calculatedHost, string xForwarded)
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", xForwarded);
        var links = await client.GetFromJsonAsync<Links>($"{requestUriString}/actuator", SerializerOptions);

        links.Should().NotBeNull();
        links!.Entries.Should().ContainKey("self").WhoseValue.Href.Should().Be($"{calculatedHost}/actuator");
        links.Entries.Should().ContainKey("info").WhoseValue.Href.Should().Be($"{calculatedHost}/actuator/info");
    }

    [Fact]
    public async Task HypermediaEndpointMiddleware_ServiceContractNotBroken()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator"));
        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "info": {
                  "href": "http://localhost/actuator/info",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/actuator",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task HypermediaEndpointMiddleware_Returns_Expected_When_ManagementPath_Is_Slash()
    {
        _appSettings.Add("Management:Endpoints:Path", "/");

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/"));
        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "info": {
                  "href": "http://localhost/info",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        HypermediaEndpointOptions endpointOptions = GetOptionsFromSettings<HypermediaEndpointOptions, ConfigureHypermediaEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions, ConfigureManagementOptions>().CurrentValue;
        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator", endpointOptions.GetPathMatchPattern(managementOptions.Path));
        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }
}
