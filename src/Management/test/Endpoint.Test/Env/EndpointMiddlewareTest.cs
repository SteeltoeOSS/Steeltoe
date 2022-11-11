// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Hypermedia;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Env;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new()
    {
        ["Logging:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true"
    };

    private readonly IHostEnvironment _host = HostingHelpers.GetHostingEnvironment();

    [Fact]
    public async Task HandleEnvRequestAsync_ReturnsExpected()
    {
        var opts = new EnvEndpointOptions();

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        var options = new ActuatorManagementOptions();
        options.EndpointOptions.Add(opts);
        var ep = new EnvEndpoint(opts, configurationRoot, _host);
        var middle = new EnvEndpointMiddleware(null, ep, options);

        HttpContext context = CreateRequest("GET", "/env");
        await middle.HandleEnvRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string json = await reader.ReadLineAsync();

        const string expected =
            "{\"activeProfiles\":[\"EnvironmentName\"],\"propertySources\":[{\"name\":\"MemoryConfigurationProvider\",\"properties\":{\"Logging:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:enabled\":{\"value\":\"true\"}}}]}";

        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task EnvActuator_ReturnsExpectedData()
    {
        // Some developers set ASPNETCORE_ENVIRONMENT in their environment, which will break this test if we don't un-set it
        string originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage result = await client.GetAsync("http://localhost/cloudfoundryapplication/env");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            string json = await result.Content.ReadAsStringAsync();

            const string expected =
                "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"name\":\"ChainedConfigurationProvider\",\"properties\":{\"applicationName\":{\"value\":\"Steeltoe.Management.Endpoint.Test\"}}},{\"name\":\"MemoryConfigurationProvider\",\"properties\":{\"Logging:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:enabled\":{\"value\":\"true\"}}}]}";

            Assert.Equal(expected, json);
        }

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = new EnvEndpointOptions();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/env", options.GetContextPath(new ActuatorManagementOptions()));
        Assert.Equal("/cloudfoundryapplication/env", options.GetContextPath(new CloudFoundryManagementOptions()));
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
