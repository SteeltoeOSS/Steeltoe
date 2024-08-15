// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Loggers;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
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
    public async Task LoggersActuator_ReturnsExpectedData()
    {
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<Startup>()
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
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<Startup>()
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
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<Startup>()
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
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["management:endpoints:path"] = "/"
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<Startup>()
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
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<Startup>()
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
        var endpointOptions = GetOptionsFromSettings<LoggersEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.False(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/loggers/{**_}", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));

        Assert.Equal("/cloudfoundryapplication/loggers/{**_}",
            endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));

        Assert.Collection(endpointOptions.AllowedVerbs, verb => Assert.Contains("Get", verb, StringComparison.Ordinal),
            verb => Assert.Contains("Post", verb, StringComparison.Ordinal));
    }

    [Fact]
    public async Task LoggersActuator_MultipleProviders_ReturnsExpectedData()
    {
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<Startup>()
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
}
