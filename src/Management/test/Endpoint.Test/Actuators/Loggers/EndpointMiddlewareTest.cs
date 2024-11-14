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
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:loggers:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "loggers"
    };

    [Fact]
    public async Task LoggersActuator_ReturnsExpectedData()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));
        builder.ConfigureLogging((context, loggingBuilder) => loggingBuilder.AddConfiguration(context.Configuration));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        var result = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");

        result.TryGetProperty("levels", out JsonElement levels).Should().BeTrue();
        levels.ToString().Should().Be("""["OFF","FATAL","ERROR","WARN","INFO","DEBUG","TRACE"]""");

        result.TryGetProperty("loggers", out JsonElement loggers).Should().BeTrue();

        loggers.TryGetProperty("Default", out JsonElement defaultLogger).Should().BeTrue();
        defaultLogger.ToString().Should().Be("""{"effectiveLevel":"WARN"}""");

        loggers.TryGetProperty("Steeltoe.Management", out JsonElement managementLogger).Should().BeTrue();
        managementLogger.ToString().Should().Be("""{"effectiveLevel":"INFO"}""");
    }

    [Fact]
    public async Task LoggersActuator_ReturnsBadRequest()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));
        builder.ConfigureLogging((context, loggingBuilder) => loggingBuilder.AddConfiguration(context.Configuration));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"BadData\"}");
        HttpResponseMessage changeResult = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Default"), content);
        Assert.Equal(HttpStatusCode.BadRequest, changeResult.StatusCode);
    }

    [Fact]
    public async Task LoggersActuator_AcceptsPost()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));
        builder.ConfigureLogging((context, loggingBuilder) => loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging")));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
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

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));
        builder.ConfigureLogging((context, loggingBuilder) => loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging")));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"ERROR\"}");
        HttpResponseMessage changeResult = await client.PostAsync(new Uri("http://localhost/loggers/Default"), content);
        Assert.Equal(HttpStatusCode.OK, changeResult.StatusCode);

        var parsedObject = await client.GetFromJsonAsync<JsonElement>("http://localhost/loggers");
        Assert.Equal("ERROR", parsedObject.GetProperty("loggers").GetProperty("Default").GetProperty("effectiveLevel").GetString());
    }

    [Fact]
    public async Task LoggersActuator_UpdateNameSpace_UpdatesChildren()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));
        builder.ConfigureLogging((context, loggingBuilder) => loggingBuilder.AddConfiguration(context.Configuration.GetSection("Logging")));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"TRACE\"}");
        HttpResponseMessage changeResult = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Steeltoe"), content);
        Assert.Equal(HttpStatusCode.OK, changeResult.StatusCode);

        var json = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");
        JsonElement loggers = json.GetProperty("loggers");
        Assert.Equal("TRACE", loggers.GetProperty("Steeltoe").GetProperty("effectiveLevel").GetString());
        Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management").GetProperty("effectiveLevel").GetString());
        Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management.Endpoint").GetProperty("effectiveLevel").GetString());
        Assert.Equal("TRACE", loggers.GetProperty("Steeltoe.Management.Endpoint.Actuators.Loggers").GetProperty("effectiveLevel").GetString());

        Assert.Equal("TRACE",
            loggers.GetProperty("Steeltoe.Management.Endpoint.Actuators.Loggers.LoggersEndpointMiddleware").GetProperty("effectiveLevel").GetString());
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
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));

        builder.ConfigureLogging((context, loggingBuilder) =>
        {
            loggingBuilder.AddConfiguration(context.Configuration);
            loggingBuilder.AddDynamicConsole();
            loggingBuilder.AddDebug();
        });

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        var result = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");

        result.TryGetProperty("levels", out JsonElement levels).Should().BeTrue();
        levels.ToString().Should().Be("""["OFF","FATAL","ERROR","WARN","INFO","DEBUG","TRACE"]""");

        result.TryGetProperty("loggers", out JsonElement loggers).Should().BeTrue();

        loggers.TryGetProperty("Default", out JsonElement defaultLogger).Should().BeTrue();
        defaultLogger.ToString().Should().Be("""{"effectiveLevel":"WARN"}""");

        loggers.TryGetProperty("Steeltoe.Management", out JsonElement managementLogger).Should().BeTrue();
        managementLogger.ToString().Should().Be("""{"effectiveLevel":"INFO"}""");
    }
}
