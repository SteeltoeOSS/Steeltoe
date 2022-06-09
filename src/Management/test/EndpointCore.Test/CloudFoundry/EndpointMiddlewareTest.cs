// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test;

public class EndpointMiddlewareTest : BaseTest
{
    private readonly Dictionary<string, string> _appSettings = new ()
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

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = new HypermediaEndpointOptions();
        Assert.True(options.ExactMatch);
        Assert.Equal("/cloudfoundryapplication", options.GetContextPath(new CloudFoundryManagementOptions()));
        Assert.Null(options.AllowedVerbs);
    }

    [Fact]
    public async Task HandleCloudFoundryRequestAsync_ReturnsExpected()
    {
        var opts = new CloudFoundryEndpointOptions();
        var mgmtOptions = new CloudFoundryManagementOptions();
        mgmtOptions.EndpointOptions.Add(opts);
        var ep = new TestCloudFoundryEndpoint(opts, mgmtOptions);

        var middle = new CloudFoundryEndpointMiddleware(null, ep, mgmtOptions);

        var context = CreateRequest("GET", "/");
        await middle.HandleCloudFoundryRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        var json = await rdr.ReadToEndAsync();
        Assert.Equal("{\"type\":\"steeltoe\",\"_links\":{}}", json);
    }

    [Fact]
    public async Task CloudFoundryEndpointMiddleware_ReturnsExpectedData()
    {
        var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var options = GetSerializerOptions();
        options.PropertyNameCaseInsensitive = true;
        var links = await client.GetFromJsonAsync<Links>("http://localhost/cloudfoundryapplication", options);
        Assert.NotNull(links);
        Assert.True(links._links.ContainsKey("self"));
        Assert.Equal("http://localhost/cloudfoundryapplication", links._links["self"].Href);
        Assert.True(links._links.ContainsKey("info"));
        Assert.Equal("http://localhost/cloudfoundryapplication/info", links._links["info"].Href);
    }

    [Fact]
    public async Task CloudFoundryEndpointMiddleware_ServiceContractNotBroken()
    {
        // arrange a server and client
        var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        var client = server.CreateClient();

        // send the request
        var result = await client.GetAsync("http://localhost/cloudfoundryapplication");
        var json = await result.Content.ReadAsStringAsync();

        Assert.Equal("{\"type\":\"steeltoe\",\"_links\":{\"info\":{\"href\":\"http://localhost/cloudfoundryapplication/info\",\"templated\":false},\"self\":{\"href\":\"http://localhost/cloudfoundryapplication\",\"templated\":false}}}", json);
    }

    [Fact]
    public async Task CloudFoundryOptions_UseDefaultJsonSerializerOptions()
    {
        var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var response = await client.GetStringAsync("http://localhost/cloudfoundryapplication/info");

        Assert.Contains("2017-07-12T18:40:39Z", response);
        Assert.Contains("2017-06-08T12:47:02Z", response);
    }

    [Fact]
    public async Task CloudFoundryOptions_UseCustomJsonSerializerOptions()
    {
        Dictionary<string, string> settings = new (_appSettings) { { "management:endpoints:CustomJsonConverters:0", "Steeltoe.Management.Endpoint.Info.EpochSecondsDateTimeConverter" } };
        var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(settings));

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var response = await client.GetStringAsync("http://localhost/cloudfoundryapplication/info");

        Assert.Contains("1499884839000", response);
        Assert.DoesNotContain("2017-07-12T18:40:39Z", response);
        Assert.Contains("1496926022000", response);
        Assert.DoesNotContain("2017-06-08T12:47:02Z", response);
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
