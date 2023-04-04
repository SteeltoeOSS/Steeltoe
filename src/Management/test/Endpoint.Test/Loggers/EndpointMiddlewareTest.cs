// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Loggers;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:loggers:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "loggers"
    };

    [Fact]
    public async Task HandleLoggersRequestAsync_ReturnsExpected()
    {
        IOptionsMonitor<LoggersEndpointOptions> opts = GetOptionsMonitorFromSettings<LoggersEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        var ep = new TestLoggersEndpoint(opts);
        var middle = new LoggersEndpointMiddleware(ep, managementOptions, NullLogger<LoggersEndpointMiddleware>.Instance);
        HttpContext context = CreateRequest("GET", "/loggers");
        await middle.HandleLoggersRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        string json = await rdr.ReadToEndAsync();
        Assert.Equal("{}", json);
    }

    [Fact]
    public async Task LoggersActuator_ReturnsExpectedData()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging((context, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(context.Configuration);
                loggingBuilder.AddDynamicConsole();
            });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        var result = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");

        Assert.True(result.TryGetProperty("loggers", out JsonElement loggers));
        Assert.True(result.TryGetProperty("levels", out _));
        Assert.Equal("WARN", loggers.GetProperty("Default").GetProperty("configuredLevel").GetString());
        Assert.Equal("INFO", loggers.GetProperty("Steeltoe.Management").GetProperty("effectiveLevel").GetString());
    }

    [Fact]
    public async Task LoggersActuator_ReturnsBadRequest()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging((context, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(context.Configuration);
                loggingBuilder.AddDynamicConsole();
            });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"BadData\"}");
        HttpResponseMessage changeResult = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Default"), content);
        Assert.Equal(HttpStatusCode.BadRequest, changeResult.StatusCode);
    }

    [Fact]
    public async Task LoggersActuator_AcceptsPost()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging((context, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));
                loggingBuilder.AddDynamicConsole();
            });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"ERROR\"}");
        HttpResponseMessage changeResult = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Default"), content);
        Assert.Equal(HttpStatusCode.OK, changeResult.StatusCode);

        var parsedObject = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");
        Assert.Equal("ERROR", parsedObject.GetProperty("loggers").GetProperty("Default").GetProperty("effectiveLevel").GetString());
    }

    [Fact]
    public async Task LoggersActuator_AcceptsPost_When_ManagementPath_Is_Slash()
    {
        var appSettings = new Dictionary<string, string>(AppSettings)
        {
            ["management:endpoints:path"] = "/"
        };

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings)).ConfigureLogging((context, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));
                loggingBuilder.AddDynamicConsole();
            });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"ERROR\"}");
        HttpResponseMessage changeResult = await client.PostAsync(new Uri("http://localhost/loggers/Default"), content);
        Assert.Equal(HttpStatusCode.OK, changeResult.StatusCode);

        var parsedObject = await client.GetFromJsonAsync<JsonElement>("http://localhost/loggers");
        Assert.Equal("ERROR", parsedObject.GetProperty("loggers").GetProperty("Default").GetProperty("effectiveLevel").GetString());
    }

    [Fact]
    public async Task LoggersActuator_UpdateNameSpace_UpdatesChildren()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging((context, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging"));
                loggingBuilder.AddDynamicConsole();
            });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"TRACE\"}");
        HttpResponseMessage changeResult = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Steeltoe"), content);
        Assert.Equal(HttpStatusCode.OK, changeResult.StatusCode);

        var json = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");
        JsonElement loggers = json.GetProperty("loggers");
        Assert.Equal("TRACE", loggers.GetProperty("Steeltoe").GetProperty("effectiveLevel").GetString());
        Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management").GetProperty("effectiveLevel").GetString());
        Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management.Endpoint").GetProperty("effectiveLevel").GetString());
        Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management.Endpoint.Loggers").GetProperty("effectiveLevel").GetString());
        Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management.Endpoint.Loggers.LoggersEndpointMiddleware").GetProperty("effectiveLevel").GetString());
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<LoggersEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        Assert.False(options.ExactMatch);
        Assert.Equal("/actuator/loggers/{**_}", options.GetContextPath(managementOptions.Get(ActuatorContext.Name)));
        Assert.Equal("/cloudfoundryapplication/loggers/{**_}", options.GetContextPath(managementOptions.Get(CFContext.Name)));

        Assert.Collection(options.AllowedVerbs, verb => Assert.Contains("Get", verb, StringComparison.Ordinal),
            verb => Assert.Contains("Post", verb, StringComparison.Ordinal));
    }

    [Fact]
    public async Task LoggersActuator_MultipleProviders_ReturnsExpectedData()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging((context, loggingBuilder) =>
            {
                loggingBuilder.AddConfiguration(context.Configuration);
                loggingBuilder.AddDynamicConsole();
                loggingBuilder.AddDebug();
            });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        var result = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");
        Assert.True(result.TryGetProperty("loggers", out JsonElement loggers));
        Assert.True(result.TryGetProperty("levels", out _));
        Assert.Equal("WARN", loggers.GetProperty("Default").GetProperty("configuredLevel").GetString());
        Assert.Equal("WARN", loggers.GetProperty("Microsoft").GetProperty("effectiveLevel").GetString());
        Assert.Equal("INFO", loggers.GetProperty("Steeltoe.Management").GetProperty("effectiveLevel").GetString());
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
