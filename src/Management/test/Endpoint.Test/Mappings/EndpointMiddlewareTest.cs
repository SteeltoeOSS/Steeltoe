// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Mappings;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Mappings;

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

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = new MappingsEndpointOptions();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/mappings", options.GetContextPath(new ActuatorManagementOptions()));
        Assert.Equal("/cloudfoundryapplication/mappings", options.GetContextPath(new CloudFoundryManagementOptions()));
        Assert.Null(options.AllowedVerbs);
    }

    [Fact]
    public async Task HandleMappingsRequestAsync_MVCNotUsed_NoRoutes_ReturnsExpected()
    {
        var opts = new MappingsEndpointOptions();
        var managementOptions = new CloudFoundryManagementOptions();
        managementOptions.EndpointOptions.Add(opts);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings);
        var ep = new MappingsEndpoint(opts);
        var middle = new MappingsEndpointMiddleware(null, opts, managementOptions, ep);

        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/mappings");
        await middle.HandleMappingsRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string json = await reader.ReadLineAsync();
        const string expected = "{\"contexts\":{\"application\":{\"mappings\":{\"dispatcherServlets\":{\"dispatcherServlet\":[]}}}}}";
        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task MappingsActuator_ReturnsExpectedData()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/cloudfoundryapplication/mappings"));
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();

        string expected = "{\"contexts\":{\"application\":{\"mappings\":{\"dispatcherServlets\":{\"" + typeof(HomeController).FullName + "\":[{\"handler\":\"" +
            typeof(Person).FullName +
            " Index()\",\"predicate\":\"{[/Home/Index],methods=[GET],produces=[text/plain || application/json || text/json]}\"}]}}}}}";

        Assert.Equal(expected, json);
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