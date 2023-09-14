// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Steeltoe.Logging.DynamicLogger;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Services;
using Xunit;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Services;

public class EndpointMiddlewareTest:BaseTest
{
    private readonly ITestOutputHelper _output;

    public EndpointMiddlewareTest(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly Dictionary<string, string> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true"
    };

    [Fact]
    public async Task HandleServicesRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<ServicesEndpointOptions> opts = GetOptionsMonitorFromSettings<ServicesEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings).Build();

        IServiceCollection serviceCollection= new ServiceCollection();

        // Add some known services 
        serviceCollection.AddSingleton<ServicesEndpoint>(); 
        serviceCollection.AddTransient<ServicesEndpointOptions>();
        serviceCollection.AddScoped<Startup>();

        var ep = new ServicesEndpoint(opts, serviceCollection, NullLogger<ServicesEndpoint>.Instance);
        var middle = new ServicesEndpointMiddleware(ep, managementOptions, NullLogger<ServicesEndpointMiddleware>.Instance);

        HttpContext context = CreateRequest("GET", "/beans");
        await middle.HandleServicesRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string json = await reader.ReadLineAsync();

        const string expected =
           "{\"contexts\":{\"application\":{\"beans\":{\"ServicesEndpoint\":{\"scope\":\"Singleton\",\"type\":\"ServiceType: Steeltoe.Management.Endpoint.Services.ServicesEndpoint Lifetime: Singleton ImplementationType: Steeltoe.Management.Endpoint.Services.ServicesEndpoint\",\"resource\":\"Steeltoe.Management.Endpoint.Services.ServicesEndpoint, Steeltoe.Management.Endpoint, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null\",\"dependencies\":[\"Void .ctor(Microsoft.Extensions.Options.IOptionsMonitor\\u00601[Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions], Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Logging.ILogger\\u00601[Steeltoe.Management.Endpoint.Services.ServicesEndpoint])\"]},\"ServicesEndpointOptions\":{\"scope\":\"Transient\",\"type\":\"ServiceType: Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions Lifetime: Transient ImplementationType: Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions\",\"resource\":\"Steeltoe.Management.Endpoint.Services.ServicesEndpointOptions, Steeltoe.Management.Endpoint, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null\",\"dependencies\":[\"Void .ctor()\"]},\"Startup\":{\"scope\":\"Scoped\",\"type\":\"ServiceType: Steeltoe.Management.Endpoint.Test.Services.Startup Lifetime: Scoped ImplementationType: Steeltoe.Management.Endpoint.Test.Services.Startup\",\"resource\":\"Steeltoe.Management.Endpoint.Test.Services.Startup, Steeltoe.Management.Endpoint.Test, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null\",\"dependencies\":[\"Void .ctor(Microsoft.Extensions.Configuration.IConfiguration)\"]}}}}}";
        Assert.Equal(expected, json);
    }

    [Fact]
    public async Task ServicesActuator_ReturnsExpectedData()
    {
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
            HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/beans"));
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            string json = await result.Content.ReadAsStringAsync();

            const string expected = "\"IServicesEndpoint\":{\"scope\":\"Singleton\",\"type\":\"ServiceType: Steeltoe.Management.Endpoint.Services.IServicesEndpoint Lifetime: Singleton ImplementationType: Steeltoe.Management.Endpoint.Services.ServicesEndpoint\"";
            Assert.Contains(expected, json, StringComparison.OrdinalIgnoreCase);
        }

    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<ServicesEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> mgmtOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/beans", options.GetContextPath(mgmtOptions.Get(ActuatorContext.Name)));
        Assert.Equal("/cloudfoundryapplication/beans", options.GetContextPath(mgmtOptions.Get(CFContext.Name)));
        Assert.Contains("Get", options.AllowedVerbs);
    }
    [Fact]
    public async Task DoInvoke_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>
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

        var ep = tc.GetService<ServicesEndpointMiddleware>();
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
