// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Configuration;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Services;

public sealed class EndpointMiddlewareTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private static readonly string SteeltoeVersion = typeof(ServicesEndpointHandler).Assembly.GetName().Version!.ToString();

    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["management:endpoints:actuator:exposure:include:0"] = "beans"
    };

    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task HandleServicesRequestAsync_ReturnsExpected()
    {
        IServiceCollection testServices = new ServiceCollection();
        testServices.AddScoped<IServicesEndpointHandler, ServicesEndpointHandler>();
        testServices.AddTransient<ServicesEndpointOptions>();
        testServices.AddSingleton<Startup>();

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(AppSettings).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddSingleton(testServices);
        services.AddServicesActuator();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var middleware = serviceProvider.GetRequiredService<ServicesEndpointMiddleware>();

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
                    "Steeltoe.Management.Endpoint.Actuators.Services.IServicesEndpointHandler": {
                      "scope": "Scoped",
                      "type": "Steeltoe.Management.Endpoint.Actuators.Services.ServicesEndpointHandler",
                      "resource": "Steeltoe.Management.Endpoint, Version={{SteeltoeVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": [
                        "Microsoft.Extensions.Options.IOptionsMonitor`1[Steeltoe.Management.Endpoint.Actuators.Services.ServicesEndpointOptions]",
                        "Microsoft.Extensions.DependencyInjection.IServiceCollection"
                      ]
                    },
                    "Steeltoe.Management.Endpoint.Actuators.Services.ServicesEndpointOptions": {
                      "scope": "Transient",
                      "type": "Steeltoe.Management.Endpoint.Actuators.Services.ServicesEndpointOptions",
                      "resource": "Steeltoe.Management.Endpoint, Version={{SteeltoeVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    },
                    "Steeltoe.Management.Endpoint.Test.Actuators.Services.Startup": {
                      "scope": "Singleton",
                      "type": "Steeltoe.Management.Endpoint.Test.Actuators.Services.Startup",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeVersion}}, Culture=neutral, PublicKeyToken=null",
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
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/beans"));
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await result.Content.ReadAsStringAsync();
        using JsonDocument sourceJson = JsonDocument.Parse(json);

        string jsonFragment = sourceJson.RootElement.GetProperty("contexts").GetProperty("application").GetProperty("beans")
            .GetProperty(typeof(IServicesEndpointHandler).FullName!).ToString();

        jsonFragment.Should().BeJson($$"""
            {
              "scope": "Singleton",
              "type": "Steeltoe.Management.Endpoint.Actuators.Services.ServicesEndpointHandler",
              "resource": "Steeltoe.Management.Endpoint, Version={{SteeltoeVersion}}, Culture=neutral, PublicKeyToken=null",
              "dependencies": [
                "Microsoft.Extensions.Options.IOptionsMonitor`1[Steeltoe.Management.Endpoint.Actuators.Services.ServicesEndpointOptions]",
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
        endpointOptions.GetPathMatchPattern(managementOptions.Path).Should().Be("/actuator/beans");
        endpointOptions.GetPathMatchPattern(ConfigureManagementOptions.DefaultCloudFoundryPath).Should().Be("/cloudfoundryapplication/beans");
        endpointOptions.AllowedVerbs.Should().ContainSingle();
        endpointOptions.AllowedVerbs[0].Should().Be("Get");
    }

    [Fact]
    public async Task DoInvoke_ReturnsExpected()
    {
        using var testContext = new TestContext(_testOutputHelper);
        testContext.AdditionalServices = (services, _) => services.AddServicesActuator();
        testContext.AdditionalConfiguration = configuration => configuration.AddInMemoryCollection(AppSettings);

        var middleware = testContext.GetRequiredService<ServicesEndpointMiddleware>();
        HttpContext httpContext = CreateHttpContextForRequest("GET", "/beans");

        await middleware.InvokeAsync(httpContext, null);

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);

        string response = await reader.ReadToEndAsync();
        response.Should().StartWith("""{"contexts":{"application":{"beans":{""");
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
