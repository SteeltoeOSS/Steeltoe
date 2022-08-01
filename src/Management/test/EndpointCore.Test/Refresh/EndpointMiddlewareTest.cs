// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using System.Net;
using System.Text;
using Xunit;

namespace Steeltoe.Management.Endpoint.Refresh.Test;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new ()
    {
        ["Logging:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
    };

    [Fact]
    public async Task HandleRefreshRequestAsync_ReturnsExpected()
    {
        var opts = new RefreshEndpointOptions();
        var managementOptions = new ActuatorManagementOptions();
        managementOptions.EndpointOptions.Add(opts);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings);
        var config = configurationBuilder.Build();

        var ep = new RefreshEndpoint(opts, config);
        var middle = new RefreshEndpointMiddleware(null, ep, managementOptions);

        var context = CreateRequest("GET", "/refresh");
        await middle.HandleRefreshRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var json = await reader.ReadLineAsync();
        var expected = "[\"management\",\"management:endpoints\",\"management:endpoints:enabled\",\"Logging\",\"Logging:LogLevel\",\"Logging:LogLevel:Steeltoe\",\"Logging:LogLevel:Pivotal\",\"Logging:LogLevel:Default\",\"Logging:IncludeScopes\"]";
        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task RefreshActuator_ReturnsExpectedData()
    {
        var ancEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        var builder = new WebHostBuilder()
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(AppSettings))
            .ConfigureLogging((webHostContext, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(webHostContext.Configuration);
                loggingBuilder.AddDynamicConsole();
            });
        using (var server = new TestServer(builder))
        {
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/cloudfoundryapplication/refresh");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var json = await result.Content.ReadAsStringAsync();
            var expected = "[\"urls\",\"management\",\"management:endpoints\",\"management:endpoints:enabled\",\"Logging\",\"Logging:LogLevel\",\"Logging:LogLevel:Steeltoe\",\"Logging:LogLevel:Pivotal\",\"Logging:LogLevel:Default\",\"Logging:IncludeScopes\",\"environment\",\"applicationName\"]";
            Assert.Equal(expected, json);
        }

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", ancEnv);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = new RefreshEndpointOptions();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/refresh", options.GetContextPath(new ActuatorManagementOptions()));
        Assert.Equal("/cloudfoundryapplication/refresh", options.GetContextPath(new CloudFoundryManagementOptions()));
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
