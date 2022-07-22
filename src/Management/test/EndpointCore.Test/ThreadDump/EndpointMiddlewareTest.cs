// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.ThreadDump.Test;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new ()
    {
        ["Logging:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:dump:enabled"] = "true",
    };

    [Fact]
    public async Task HandleThreadDumpRequestAsync_ReturnsExpected()
    {
        var opts = new ThreadDumpEndpointOptions();

        var mgmtOptions = new ActuatorManagementOptions();
        mgmtOptions.EndpointOptions.Add(opts);

        var obs = new ThreadDumperEP(opts);
        var ep = new ThreadDumpEndpoint(opts, obs);
        var middle = new ThreadDumpEndpointMiddleware(null, ep, mgmtOptions);
        var context = CreateRequest("GET", "/dump");
        await middle.HandleThreadDumpRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        var json = await rdr.ReadToEndAsync();
        Assert.NotNull(json);
        Assert.NotEqual("[]", json);
        Assert.StartsWith("[", json);
        Assert.EndsWith("]", json);
    }

    [Fact]
    public async Task ThreadDumpActuator_ReturnsExpectedData()
    {
        var builder = new WebHostBuilder()
            .UseStartup<StartupV1>()
            .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
            .ConfigureLogging((webhostContext, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(webhostContext.Configuration);
                loggingBuilder.AddDynamicConsole();
            });

        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var result = await client.GetAsync("http://localhost/cloudfoundryapplication/dump");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);
        Assert.NotEqual("[]", json);
        Assert.StartsWith("[", json);
        Assert.EndsWith("]", json);
    }

    [Fact]
    public async Task ThreadDumpActuatorv2_ReturnsExpectedData()
    {
        if (Platform.IsWindows)
        {
            var builder = new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((builderContext, config) => config.AddInMemoryCollection(AppSettings))
                .ConfigureLogging((webhostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webhostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/threaddump");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            Assert.NotNull(json);
            Assert.NotEqual("{}", json);
            Assert.StartsWith("{", json);
            Assert.EndsWith("}", json);
        }
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = new ThreadDumpEndpointOptions();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/dump", options.GetContextPath(new ActuatorManagementOptions()));
        Assert.Equal("/cloudfoundryapplication/dump", options.GetContextPath(new CloudFoundryManagementOptions()));
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