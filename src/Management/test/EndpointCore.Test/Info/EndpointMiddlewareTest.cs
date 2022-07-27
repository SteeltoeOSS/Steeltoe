// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Info;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test;

public class EndpointMiddlewareTest : BaseTest
{
    private readonly Dictionary<string, string> _appSettings = new ()
    {
        ["management:endpoints:enabled"] = "false",
        ["management:endpoints:path"] = "/management",
        ["management:endpoints:info:enabled"] = "true",
        ["management:endpoints:info:id"] = "infomanagement",
        ["management:endpoints:actuator:exposure:include:0"] = "*",
        ["info:application:name"] = "foobar",
        ["info:application:version"] = "1.0.0'",
        ["info:application:date"] = "5/1/2008",
        ["info:application:time"] = "8:30:52 AM",
        ["info:NET:type"] = "Core",
        ["info:NET:version"] = "2.0.0",
        ["info:NET:ASPNET:type"] = "Core",
        ["info:NET:ASPNET:version"] = "2.0.0"
    };

    [Fact]
    public async Task HandleInfoRequestAsync_ReturnsExpected()
    {
        var opts = new InfoEndpointOptions();
        var mopts = new ActuatorManagementOptions();
        mopts.EndpointOptions.Add(opts);
        var contribs = new List<IInfoContributor>() { new GitInfoContributor() };
        var ep = new TestInfoEndpoint(opts, contribs);
        var middle = new InfoEndpointMiddleware(null, ep, mopts);
        var context = CreateRequest("GET", "/loggers");
        await middle.HandleInfoRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        var json = await rdr.ReadToEndAsync();
        Assert.Equal("{}", json);
    }

    [Fact]
    public async Task InfoActuator_ReturnsExpectedData()
    {
        // Note: This test pulls in from git.properties and appsettings created
        // in the Startup class
        var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(_appSettings));

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var dict = await client.GetFromJsonAsync<Dictionary<string, Dictionary<string, JsonElement>>>("http://localhost/management/infomanagement", GetSerializerOptions());
        Assert.NotNull(dict);

        Assert.Equal(6, dict.Count);
        Assert.True(dict.ContainsKey("application"));
        Assert.True(dict.ContainsKey("NET"));
        Assert.True(dict.ContainsKey("git"));

        var appNode = dict["application"];
        Assert.NotNull(appNode);
        Assert.Equal("foobar", appNode["name"].ToString());

        var netNode = dict["NET"];
        Assert.NotNull(netNode);
        Assert.Equal("Core", netNode["type"].ToString());

        var gitNode = dict["git"];
        Assert.NotNull(gitNode);
        Assert.True(gitNode.ContainsKey("build"));
        Assert.True(gitNode.ContainsKey("branch"));
        Assert.True(gitNode.ContainsKey("commit"));
        Assert.True(gitNode.ContainsKey("closest"));
        Assert.True(gitNode.ContainsKey("dirty"));
        Assert.True(gitNode.ContainsKey("remote"));
        Assert.True(gitNode.ContainsKey("tags"));
        var buildInfo = gitNode["build"].TryGetProperty("time", out var bTime);
        Assert.Equal("2017-07-12T18:40:39Z", bTime.GetString());
        var commitInfo = gitNode["commit"].TryGetProperty("time", out var cTime);
        Assert.Equal("2017-06-08T12:47:02Z", cTime.GetString());
    }

    [Fact]
    public async Task InfoActuator_UsesCustomJsonSerializerOptions()
    {
        // Note: This test pulls in from git.properties and appsettings created
        // in the Startup class
        Dictionary<string, string> settings = new () { { "management:endpoints:CustomJsonConverters:0", "Steeltoe.Management.Endpoint.Info.EpochSecondsDateTimeConverter" } };
        var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((context, config) => config.AddInMemoryCollection(settings));

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var response = await client.GetStringAsync("http://localhost/actuator/info");

        Assert.Contains("1499884839000", response);
        Assert.DoesNotContain("2017-07-12T18:40:39Z", response);
        Assert.Contains("1496926022000", response);
        Assert.DoesNotContain("2017-06-08T12:47:02Z", response);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = new InfoEndpointOptions();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/info", options.GetContextPath(new ActuatorManagementOptions()));
        Assert.Equal("/cloudfoundryapplication/info", options.GetContextPath(new CloudFoundryManagementOptions()));
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