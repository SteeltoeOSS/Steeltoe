// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Services;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Services;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly string SteeltoeVersion = typeof(ServicesEndpointHandler).Assembly.GetName().Version!.ToString();

    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["management:endpoints:actuator:exposure:include:0"] = "beans"
    };

    private readonly ITestOutputHelper _output;

    public EndpointMiddlewareTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task HandleServicesRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<ServicesEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<ServicesEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>(AppSettings);

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<ServicesEndpointHandler>();
        services.AddTransient<ServicesEndpointOptions>();
        services.AddScoped<Startup>();

        var handler = new ServicesEndpointHandler(endpointOptionsMonitor, services);
        var middleware = new ServicesEndpointMiddleware(handler, managementOptionsMonitor, NullLoggerFactory.Instance);

        HttpContext httpContext = CreateHttpContextForRequest("GET", "/beans");
        await middleware.InvokeAsync(httpContext, null);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);
        string json = await reader.ReadToEndAsync();

        json.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "ServicesEndpointHandler": {
                      "scope": "Singleton",
                      "type": "Steeltoe.Management.Endpoint.Services.ServicesEndpointHandler",
                      "resource": "Steeltoe.Management.Endpoint.Services.ServicesEndpointHandler, Steeltoe.Management.Endpoint, Version={{SteeltoeVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": [
                        "Microsoft.Extensions.Options.IOptionsMonitor`1[Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions]",
                        "Microsoft.Extensions.DependencyInjection.IServiceCollection"
                      ]
                    },
                    "ServicesEndpointOptions": {
                      "scope": "Transient",
                      "type": "Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions",
                      "resource": "Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions, Steeltoe.Management.Endpoint, Version={{SteeltoeVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    },
                    "Startup": {
                      "scope": "Scoped",
                      "type": "Steeltoe.Management.Endpoint.Test.Services.Startup",
                      "resource": "Steeltoe.Management.Endpoint.Test.Services.Startup, Steeltoe.Management.Endpoint.Test, Version={{SteeltoeVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": [
                        "Microsoft.Extensions.Configuration.IConfiguration"
                      ]
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ServicesActuator_ReturnsExpectedData()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));

        using var server = new TestServer(builder);

        HttpClient client = server.CreateClient();
        HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/beans"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await result.Content.ReadAsStringAsync();
        using JsonDocument sourceJson = JsonDocument.Parse(json);

        string jsonFragment = sourceJson.RootElement.GetProperty("contexts").GetProperty("application").GetProperty("beans")
            .GetProperty("IServicesEndpointHandler").ToString();

        jsonFragment.Should().BeJson($$"""
            {
              "scope": "Singleton",
              "type": "Steeltoe.Management.Endpoint.Services.IServicesEndpointHandler",
              "resource": "Steeltoe.Management.Endpoint.Services.IServicesEndpointHandler, Steeltoe.Management.Endpoint, Version={{SteeltoeVersion}}, Culture=neutral, PublicKeyToken=null",
              "dependencies": [
                "Microsoft.Extensions.Options.IOptionsMonitor`1[Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions]",
                "Microsoft.Extensions.DependencyInjection.IServiceCollection"
              ]
            }
            """);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<ServicesEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        endpointOptions.RequiresExactMatch().Should().BeTrue();
        endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path).Should().Be("/actuator/beans");

        endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath).Should()
            .Be("/cloudfoundryapplication/beans");

        endpointOptions.AllowedVerbs.Should().HaveCount(1);
        endpointOptions.AllowedVerbs[0].Should().Be("Get");
    }

    [Fact]
    public async Task DoInvoke_ReturnsExpected()
    {
        using var testContext = new TestContext(_output);
        testContext.AdditionalServices = (services, _) => services.AddServicesActuator();
        testContext.AdditionalConfiguration = configuration => configuration.AddInMemoryCollection(AppSettings);

        var middleware = testContext.GetRequiredService<ServicesEndpointMiddleware>();
        HttpContext httpContext = CreateHttpContextForRequest("GET", "/beans");

        await middleware.InvokeAsync(httpContext, null);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);

        string response = await reader.ReadToEndAsync();
        response.Should().StartWith("{\"contexts\":{\"application\":{\"beans\":{");
    }

    private static HttpContext CreateHttpContextForRequest(string method, string path)
    {
        HttpContext context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Method = method;
        context.Request.Path = new PathString(path);
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        return context;
    }
}
