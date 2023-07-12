// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public class EndpointMiddlewareTest : BaseTest
{
    private readonly Dictionary<string, string> _appSettings = new()
    {
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:path"] = "/cloudfoundryapplication",
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

    public EndpointMiddlewareTest()
    {
        System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", "somevalue"); // Allow routing to /cloudfoundryapplication
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = new HypermediaEndpointOptions();
        IOptionsMonitor<ManagementEndpointOptions> mgmtOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        Assert.True(options.ExactMatch);
        Assert.Equal("/cloudfoundryapplication", options.GetPathMatchPattern(ConfigureManagementEndpointOptions.DefaultCFPath,mgmtOptions.CurrentValue));

        Assert.Single(options.AllowedVerbs);
        Assert.Contains("Get", options.AllowedVerbs);
    }

    [Fact]
    public async Task CloudFoundryEndpointMiddleware_ReturnsExpectedData()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        JsonSerializerOptions options = GetSerializerOptions();
        options.PropertyNameCaseInsensitive = true;
        var links = await client.GetFromJsonAsync<Links>("http://localhost/cloudfoundryapplication", options);
        Assert.NotNull(links);
        Assert.True(links.LinkCollection.ContainsKey("self"));
        Assert.Equal("http://localhost/cloudfoundryapplication", links.LinkCollection["self"].Href);
        Assert.True(links.LinkCollection.ContainsKey("info"));
        Assert.Equal("http://localhost/cloudfoundryapplication/info", links.LinkCollection["info"].Href);
    }

    [Fact]
    public async Task CloudFoundryEndpointMiddleware_ServiceContractNotBroken()
    {
        // arrange a server and client
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();

        // send the request
        HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication"));
        string json = await result.Content.ReadAsStringAsync();

        Assert.Equal(
            "{\"type\":\"steeltoe\",\"_links\":{\"info\":{\"href\":\"http://localhost/cloudfoundryapplication/info\",\"templated\":false},\"self\":{\"href\":\"http://localhost/cloudfoundryapplication\",\"templated\":false}}}",
            json);
    }

    [Fact]
    public async Task CloudFoundryOptions_UseDefaultJsonSerializerOptions()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        string response = await client.GetStringAsync(new Uri("http://localhost/cloudfoundryapplication/info"));

        Assert.Contains("2017-07-12T18:40:39Z", response, StringComparison.Ordinal);
        Assert.Contains("2017-06-08T12:47:02Z", response, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CloudFoundryOptions_UseCustomJsonSerializerOptions()
    {
        Dictionary<string, string> settings = new(_appSettings)
        {
            { "management:endpoints:CustomJsonConverters:0", "Steeltoe.Management.Endpoint.Info.EpochSecondsDateTimeConverter" }
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        string response = await client.GetStringAsync(new Uri("http://localhost/cloudfoundryapplication/info"));

        Assert.Contains("1499884839000", response, StringComparison.Ordinal);
        Assert.DoesNotContain("2017-07-12T18:40:39Z", response, StringComparison.Ordinal);
        Assert.Contains("1496926022000", response, StringComparison.Ordinal);
        Assert.DoesNotContain("2017-06-08T12:47:02Z", response, StringComparison.Ordinal);
    }
}
