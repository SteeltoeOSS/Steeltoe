// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
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
        var endpointOptions = GetOptionsFromSettings<RouteMappingsEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/mappings", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));

        Assert.Equal("/cloudfoundryapplication/mappings",
            endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));

        Assert.Single(endpointOptions.AllowedVerbs);
        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }

    [Fact]
    public async Task HandleMappingsRequestAsync_MVCNotUsed_NoRoutes_ReturnsExpected()
    {
        IOptionsMonitor<RouteMappingsEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<RouteMappingsEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings);
        var routerMappings = new RouterMappings();
        var mockActionDescriptorCollectionProvider = new Mock<IActionDescriptorCollectionProvider>();

        mockActionDescriptorCollectionProvider.Setup(provider => provider.ActionDescriptors)
            .Returns(new ActionDescriptorCollection(Array.Empty<ActionDescriptor>(), 0));

        var mockApiDescriptionProviders = new List<IApiDescriptionProvider>
        {
            new Mock<IApiDescriptionProvider>().Object
        };

        var handler = new RouteMappingsEndpointHandler(endpointOptionsMonitor, mockActionDescriptorCollectionProvider.Object, mockApiDescriptionProviders,
            routerMappings, NullLoggerFactory.Instance);

        var middleware = new RouteMappingsEndpointMiddleware(handler, managementOptionsMonitor, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/mappings");
        await middleware.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string? json = await reader.ReadLineAsync();
        const string expected = "{\"contexts\":{\"application\":{\"mappings\":{\"dispatcherServlets\":{\"dispatcherServlet\":[]}}}}}";
        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task MappingsActuator_EndpointRouting_ReturnsExpectedData()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            { "management:endpoints:actuator:exposure:include:0", "*" }
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/mappings"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();

        string expected = $"{{\"contexts\":{{\"application\":{{\"mappings\":{{\"dispatcherServlets\":{{\"{typeof(HomeController).FullName}\":" +
            $"[{{\"handler\":\"{typeof(Person).FullName} Index()\",\"predicate\":\"{{[/Home/Index],methods=[GET],produces=[text/plain || " +
            "application/json || text/json],consumes=[text/plain || application/json || text/json]}\",\"details\":{\"requestMappingConditions\":" +
            "{\"patterns\":[\"/Home/Index\"],\"methods\":[\"GET\"],\"consumes\":[{\"mediaType\":\"text/plain\",\"negated\":false}," +
            "{\"mediaType\":\"application/json\",\"negated\":false},{\"mediaType\":\"text/json\",\"negated\":false}],\"produces\":" +
            "[{\"mediaType\":\"text/plain\",\"negated\":false},{\"mediaType\":\"application/json\",\"negated\":false}," +
            "{\"mediaType\":\"text/json\",\"negated\":false}],\"headers\":[],\"params\":[]}}}]}}}}}";

        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task MappingsActuator_ConventionalRouting_ReturnsExpectedData()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            { "management:endpoints:actuator:exposure:include:0", "*" },
            { "TestUsesEndpointRouting", "False" }
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/mappings"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson($@"{{
  ""contexts"": {{
    ""application"": {{
      ""mappings"": {{
        ""dispatcherServlets"": {{
          ""{typeof(HomeController).FullName}"": [
            {{
              ""handler"": ""{typeof(Person).FullName} Index()"",
              ""predicate"": ""{{[/Home/Index],methods=[GET],produces=[text/plain || application/json || text/json],consumes=[text/plain || application/json || text/json]}}"",
              ""details"": {{
                ""requestMappingConditions"": {{
                  ""patterns"": [
                    ""/Home/Index""
                  ],
                  ""methods"": [
                    ""GET""
                  ],
                  ""consumes"": [
                    {{
                      ""mediaType"": ""text/plain"",
                      ""negated"": false
                    }},
                    {{
                      ""mediaType"": ""application/json"",
                      ""negated"": false
                    }},
                    {{
                      ""mediaType"": ""text/json"",
                      ""negated"": false
                    }}
                  ],
                  ""produces"": [
                    {{
                      ""mediaType"": ""text/plain"",
                      ""negated"": false
                    }},
                    {{
                      ""mediaType"": ""application/json"",
                      ""negated"": false
                    }},
                    {{
                      ""mediaType"": ""text/json"",
                      ""negated"": false
                    }}
                  ],
                  ""headers"": [],
                  ""params"": []
                }}
              }}
            }}
          ],
          ""CoreRouteHandler"": [
            {{
              ""handler"": ""CoreRouteHandler"",
              ""predicate"": ""{{[{{controller=Home}}/{{action=Index}}/{{id?}}],methods=[GET || PUT || POST || DELETE || HEAD || OPTIONS]}}""
            }},
            {{
              ""handler"": ""CoreRouteHandler"",
              ""predicate"": ""{{[/actuator/mappings],methods=[Get]}}""
            }}
          ]
        }}
      }}
    }}
  }}
}}");
    }

    private HttpContext CreateRequest(string method, string path)
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Response.Body = new MemoryStream();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        return context;
    }
}
