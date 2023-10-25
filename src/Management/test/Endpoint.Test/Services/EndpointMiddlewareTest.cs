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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Services;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Services;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
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
        IOptionsMonitor<ServicesEndpointOptions> opts = GetOptionsMonitorFromSettings<ServicesEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>(AppSettings);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings).Build();

        IServiceCollection serviceCollection = new ServiceCollection();

        // Add some known services 
        serviceCollection.AddSingleton<ServicesEndpointHandler>();
        serviceCollection.AddTransient<ServicesEndpointOptions>();
        serviceCollection.AddScoped<Startup>();

        var ep = new ServicesEndpointHandler(opts, serviceCollection, NullLoggerFactory.Instance);
        var middle = new ServicesEndpointMiddleware(ep, managementOptions, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/beans");
        await middle.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string? json = await reader.ReadLineAsync();

        const string expected = @"
            {
              ""contexts"": {
                ""application"": {
                  ""beans"": {
                    ""ServicesEndpointHandler"": {
                      ""scope"": ""Singleton"",
                      ""type"": ""Steeltoe.Management.Endpoint.Services.ServicesEndpointHandler"",
                      ""resource"": ""Steeltoe.Management.Endpoint.Services.ServicesEndpointHandler, Steeltoe.Management.Endpoint, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null"",
                      ""dependencies"": [
                        ""Microsoft.Extensions.Options.IOptionsMonitor`1[Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions] options"",
                        ""Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection"",
                        ""Microsoft.Extensions.Logging.ILoggerFactory loggerFactory""
                      ]
                    },
                    ""ServicesEndpointOptions"": {
                      ""scope"": ""Transient"",
                      ""type"": ""Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions"",
                      ""resource"": ""Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions, Steeltoe.Management.Endpoint, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null"",
                      ""dependencies"": []
                    },
                    ""Startup"": {
                      ""scope"": ""Scoped"",
                      ""type"": ""Steeltoe.Management.Endpoint.Test.Services.Startup"",
                      ""resource"": ""Steeltoe.Management.Endpoint.Test.Services.Startup, Steeltoe.Management.Endpoint.Test, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null"",
                      ""dependencies"": [
                        ""Microsoft.Extensions.Configuration.IConfiguration configuration""
                      ]
                    }
                  }
                }
              }
            }";

        json.Should().BeJson(expected);
    }

    [Fact]
    public async Task ServicesActuator_ReturnsExpectedData()
    {
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
            HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/beans"));
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            string json = await result.Content.ReadAsStringAsync();

            using JsonDocument sourceJson = JsonDocument.Parse(json);

            string jsonFragment = sourceJson.RootElement.GetProperty("contexts").GetProperty("application").GetProperty("beans")
                .GetProperty("IServicesEndpointHandler").ToString();

            string expected = @"
                {
                  ""scope"": ""Singleton"",
                  ""type"": ""Steeltoe.Management.Endpoint.Services.IServicesEndpointHandler"",
                  ""resource"": ""Steeltoe.Management.Endpoint.Services.IServicesEndpointHandler, Steeltoe.Management.Endpoint, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null"",
                  ""dependencies"": [
                    ""Microsoft.Extensions.Options.IOptionsMonitor`1[Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions] options"",
                    ""Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection"",
                    ""Microsoft.Extensions.Logging.ILoggerFactory loggerFactory""
                  ]
                }";

            jsonFragment.Should().BeJson(expected);
        }
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<ServicesEndpointOptions>();
        ManagementOptions mgmtOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;
        Assert.True(options.RequiresExactMatch());
        Assert.Equal("/actuator/beans", options.GetPathMatchPattern(mgmtOptions, mgmtOptions.Path));

        Assert.Equal("/cloudfoundryapplication/beans", options.GetPathMatchPattern(mgmtOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));
        Assert.Contains("Get", options.AllowedVerbs);
    }

    [Fact]
    public async Task DoInvoke_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:services:enabled"] = "true",
            ["management:endpoints:actuator:exposure:include:0"] = "beans"
        };

        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddServicesActuator();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appsettings);
        };

        var ep = tc.GetRequiredService<ServicesEndpointMiddleware>();
        HttpContext context = CreateRequest("GET", "/beans");

        await ep.InvokeAsync(context, null);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string result = await reader.ReadToEndAsync();

        Assert.StartsWith("{\"contexts\":{\"application\":{\"beans\":{", result, StringComparison.OrdinalIgnoreCase);
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