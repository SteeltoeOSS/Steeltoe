// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
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
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:actuator:exposure:include:0"] = "loggers"
    };

    [Fact]
    public async Task LoggersActuator_ReturnsExpectedData()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        var json = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");

        json.TryGetProperty("levels", out JsonElement levels).Should().BeTrue();
        levels.ToString().Should().Be("""["OFF","FATAL","ERROR","WARN","INFO","DEBUG","TRACE"]""");

        json.TryGetProperty("groups", out JsonElement groups).Should().BeTrue();
        groups.EnumerateObject().Should().BeEmpty();

        json.TryGetProperty("loggers", out JsonElement loggers).Should().BeTrue();
        AssertLoggerCategoryHasJson(loggers, "Default", """{"effectiveLevel":"WARN"}""");
        AssertLoggerCategoryHasJson(loggers, "Steeltoe.Management", """{"effectiveLevel":"INFO"}""");
    }

    [Fact]
    public async Task LoggersActuator_ReturnsBadRequest()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"BadData\"}");
        HttpResponseMessage response = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Default"), content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().BeEmpty();
    }

    [Fact]
    public async Task LoggersActuator_AcceptsPost()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"ERROR\"}");

        HttpResponseMessage response = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Default"), content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().BeEmpty();

        var json = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");

        json.TryGetProperty("loggers", out JsonElement loggers).Should().BeTrue();
        AssertLoggerCategoryHasJson(loggers, "Default", """{"configuredLevel":"WARN","effectiveLevel":"ERROR"}""");
    }

    [Fact]
    public async Task LoggersActuator_AcceptsPost_When_ManagementPath_Is_Slash()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["management:endpoints:path"] = "/"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddLoggersActuator();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"ERROR\"}");
        HttpResponseMessage response = await client.PostAsync(new Uri("http://localhost/loggers/Default"), content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().BeEmpty();

        var json = await client.GetFromJsonAsync<JsonElement>("http://localhost/loggers");

        json.TryGetProperty("loggers", out JsonElement loggers).Should().BeTrue();
        AssertLoggerCategoryHasJson(loggers, "Default", """{"configuredLevel":"WARN","effectiveLevel":"ERROR"}""");
    }

    [Fact]
    public async Task LoggersActuator_UpdateCategory_UpdatesChildren()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpContent content = new StringContent("{\"configuredLevel\":\"TRACE\"}");
        HttpResponseMessage response = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Steeltoe"), content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().BeEmpty();

        var json = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");

        json.TryGetProperty("loggers", out JsonElement loggers).Should().BeTrue();
        AssertLoggerCategoryHasJson(loggers, "Steeltoe", """{"configuredLevel":"INFO","effectiveLevel":"TRACE"}""");
        AssertLoggerCategoryHasJson(loggers, "Steeltoe.Management", """{"effectiveLevel":"TRACE"}""");
        AssertLoggerCategoryHasJson(loggers, "Steeltoe.Management.Endpoint", """{"effectiveLevel":"TRACE"}""");
        AssertLoggerCategoryHasJson(loggers, "Steeltoe.Management.Endpoint.Actuators.Loggers", """{"effectiveLevel":"TRACE"}""");
        AssertLoggerCategoryHasJson(loggers, "Steeltoe.Management.Endpoint.Actuators.Loggers.LoggersEndpointMiddleware", """{"effectiveLevel":"TRACE"}""");
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<LoggersEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        endpointOptions.RequiresExactMatch().Should().BeFalse();

        string actuatorPathPattern = endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path);
        actuatorPathPattern.Should().Be("/actuator/loggers/{**_}");

        string cloudFoundryPathPattern = endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath);
        cloudFoundryPathPattern.Should().Be("/cloudfoundryapplication/loggers/{**_}");

        endpointOptions.AllowedVerbs.Should().HaveCount(2);
        endpointOptions.AllowedVerbs.Should().Contain("Get");
        endpointOptions.AllowedVerbs.Should().Contain("Post");
    }

    [Fact]
    public async Task LoggersActuator_MultipleProviders_ReturnsExpectedData()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddDynamicConsole();
        builder.Logging.AddDebug();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        var json = await client.GetFromJsonAsync<JsonElement>("http://localhost/actuator/loggers");

        json.TryGetProperty("levels", out JsonElement levels).Should().BeTrue();
        levels.ToString().Should().Be("""["OFF","FATAL","ERROR","WARN","INFO","DEBUG","TRACE"]""");

        json.TryGetProperty("groups", out JsonElement groups).Should().BeTrue();
        groups.EnumerateObject().Should().BeEmpty();

        json.TryGetProperty("loggers", out JsonElement loggers).Should().BeTrue();
        AssertLoggerCategoryHasJson(loggers, "Default", """{"effectiveLevel":"WARN"}""");
        AssertLoggerCategoryHasJson(loggers, "Steeltoe.Management", """{"effectiveLevel":"INFO"}""");
    }

    private static void AssertLoggerCategoryHasJson(JsonElement loggers, string category, string jsonValue)
    {
        loggers.TryGetProperty(category, out JsonElement loggerElement).Should().BeTrue();
        loggerElement.ToString().Should().Be(jsonValue);
    }
}
