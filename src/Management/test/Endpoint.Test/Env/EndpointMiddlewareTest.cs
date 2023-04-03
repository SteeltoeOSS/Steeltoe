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
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Env;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true"
    };

    private readonly IHostEnvironment _host = HostingHelpers.GetHostingEnvironment();

    [Fact]
    public async Task HandleEnvRequestAsync_ReturnsExpected()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IOptionsMonitor<EnvEndpointOptions> optionsMonitor = GetOptionsMonitorFromSettings<EnvEndpointOptions, ConfigureEnvEndpointOptions>();
        var managementOptions = new TestOptionsMonitor<ManagementEndpointOptions>(new ManagementEndpointOptions());

        var ep = new EnvEndpoint(optionsMonitor, configurationRoot, _host);
        var middle = new EnvEndpointMiddleware(ep, managementOptions);

        HttpContext context = CreateRequest("GET", "/env");
        await middle.HandleEnvRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string json = await reader.ReadLineAsync();

        const string expected =
            "{\"activeProfiles\":[\"EnvironmentName\"],\"propertySources\":[{\"name\":\"MemoryConfigurationProvider\",\"properties\":{\"Logging:Console:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:enabled\":{\"value\":\"true\"}}}]}";

        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task EnvActuator_ReturnsExpectedData()
    {
        // Some developers set ASPNETCORE_ENVIRONMENT in their environment, which will break this test if we don't un-set it
        string originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

        var appSettings = new Dictionary<string, string>(AppSettings);
        appSettings.Add("management:endpoints:actuator:exposure:include:0", "*");

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using (var server = new TestServer(builder))
        {
            HttpClient client = server.CreateClient();
            HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/env"));
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            string json = await result.Content.ReadAsStringAsync();

            const string expected =
                "{\"activeProfiles\":[\"Production\"],\"propertySources\":[{\"name\":\"ChainedConfigurationProvider\",\"properties\":{\"applicationName\":{\"value\":\"Steeltoe.Management.Endpoint.Test\"}}},{\"name\":\"MemoryConfigurationProvider\",\"properties\":{\"Logging:Console:IncludeScopes\":{\"value\":\"false\"},\"Logging:LogLevel:Default\":{\"value\":\"Warning\"},\"Logging:LogLevel:Pivotal\":{\"value\":\"Information\"},\"Logging:LogLevel:Steeltoe\":{\"value\":\"Information\"},\"management:endpoints:actuator:exposure:include:0\":{\"value\":\"*\"},\"management:endpoints:enabled\":{\"value\":\"true\"}}}]}";

            Assert.Equal(expected, json);
        }

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<EnvEndpointOptions>();

        IOptionsMonitor<ManagementEndpointOptions> mgmtOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/env", options.GetContextPath(mgmtOptions.Get(ActuatorContext.Name)));
        Assert.Equal("/cloudfoundryapplication/env", options.GetContextPath(mgmtOptions.Get(CFContext.Name)));
        Assert.Single(options.AllowedVerbs);
        Assert.Contains("Get", options.AllowedVerbs);
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
