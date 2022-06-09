// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Hypermedia.Test;

public class EndpointMiddlewareTest : BaseTest
{
    private readonly Dictionary<string, string> _appSettings = new ()
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

    [Fact]
    public async Task HandleCloudFoundryRequestAsync_ReturnsExpected()
    {
        var opts = new HypermediaEndpointOptions();
        var mgmtOpts = new ActuatorManagementOptions();
        var ep = new TestHypermediaEndpoint(opts, mgmtOpts);
        var middle = new ActuatorHypermediaEndpointMiddleware(null, ep, mgmtOpts);
        var context = CreateRequest("GET", "/");
        await middle.Invoke(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        var json = await rdr.ReadToEndAsync();
        Assert.Equal("{\"type\":\"steeltoe\",\"_links\":{}}", json);
    }

    [Theory]
    [InlineData("http://somehost:1234", "https://somehost:1234", "https")]
    [InlineData("http://somehost:443", "https://somehost", "https")]
    [InlineData("http://somehost:80", "http://somehost", "http")]
    [InlineData("http://somehost:8080", "http://somehost:8080", "http")]
    public async Task CloudFoundryEndpointMiddleware_ReturnsExpectedData(string requestUriString, string calculatedHost, string xForwarded)
    {
        var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", xForwarded);
        var links = await client.GetFromJsonAsync<Links>($"{requestUriString}/actuator");
        Assert.NotNull(links);
        Assert.True(links._links.ContainsKey("self"));
        Assert.Equal($"{calculatedHost}/actuator", links._links["self"].Href);
        Assert.True(links._links.ContainsKey("info"));
        Assert.Equal($"{calculatedHost}/actuator/info", links._links["info"].Href);
    }

    [Fact]
    public async Task HypermediaEndpointMiddleware_ServiceContractNotBroken()
    {
        // arrange a server and client
        var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        var client = server.CreateClient();

        // send the request
        var result = await client.GetAsync("http://localhost/actuator");
        var json = await result.Content.ReadAsStringAsync();

        Assert.Equal("{\"type\":\"steeltoe\",\"_links\":{\"self\":{\"href\":\"http://localhost/actuator\",\"templated\":false},\"info\":{\"href\":\"http://localhost/actuator/info\",\"templated\":false}}}", json);
    }

    [Fact]
    public async Task HypermediaEndpointMiddleware_Returns_Expected_When_ManagementPath_Is_Slash()
    {
        var settings = new Dictionary<string, string>(_appSettings);
        _appSettings.Add("Management:Endpoints:Path", "/");

        // arrange a server and client
        var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        var client = server.CreateClient();

        // send the request
        var result = await client.GetAsync("http://localhost/");
        var json = await result.Content.ReadAsStringAsync();

        Assert.Equal("{\"type\":\"steeltoe\",\"_links\":{\"self\":{\"href\":\"http://localhost/\",\"templated\":false},\"info\":{\"href\":\"http://localhost/info\",\"templated\":false}}}", json);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = new HypermediaEndpointOptions();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator", options.GetContextPath(new ActuatorManagementOptions()));
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
