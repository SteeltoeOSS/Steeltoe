// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Refresh;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Refresh;

public sealed class RefreshActuatorTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "refresh"
    };

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRefreshActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Func<RefreshEndpointMiddleware> action = serviceProvider.GetRequiredService<RefreshEndpointMiddleware>;
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddRefreshActuator();
        await using WebApplication host = builder.Build();

        RefreshEndpointOptions options = host.Services.GetRequiredService<IOptions<RefreshEndpointOptions>>().Value;

        options.ReturnConfiguration.Should().BeTrue();
        options.Enabled.Should().BeNull();
        options.Id.Should().Be("refresh");
        options.Path.Should().Be("refresh");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/refresh");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Refresh:ReturnConfiguration"] = "false",
            ["Management:Endpoints:Refresh:Enabled"] = "true",
            ["Management:Endpoints:Refresh:Id"] = "test-actuator-id",
            ["Management:Endpoints:Refresh:Path"] = "test-actuator-path",
            ["Management:Endpoints:Refresh:RequiredPermissions"] = "full",
            ["Management:Endpoints:Refresh:AllowedVerbs:0"] = "put"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddRefreshActuator();
        await using WebApplication host = builder.Build();

        RefreshEndpointOptions options = host.Services.GetRequiredService<IOptions<RefreshEndpointOptions>>().Value;

        options.ReturnConfiguration.Should().BeFalse();
        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("PUT");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/alt-actuators").Should().Be("/alt-actuators/test-actuator-path");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_expected_data(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Test:SOME:Example:Key"] = "ExampleValue"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddInMemoryCollection(appSettings);
                configurationBuilder.Add(new TestConfigurationSource());
            });

            builder.ConfigureServices(services => services.AddRefreshActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.PostAsync(new Uri("http://localhost/actuator/refresh"), null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        var responseArray = await response.Content.ReadFromJsonAsync<JsonArray>(TestContext.Current.CancellationToken);

        string[] keysExpected =
        [
            "FakeLoadCount",
            "Management",
            "Management:Endpoints",
            "Management:Endpoints:Actuator",
            "Management:Endpoints:Actuator:Exposure",
            "Management:Endpoints:Actuator:Exposure:Include",
            "Management:Endpoints:Actuator:Exposure:Include:0",
            "Test",
            "Test:SOME",
            "Test:SOME:Example",
            "Test:SOME:Example:Key"
        ];

        responseArray.Should().NotBeNull();
        string?[] responseKeys = [.. responseArray.Select(node => node?.AsValue().ToString())];
        responseKeys.Should().ContainInOrder(keysExpected);

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        configuration["FakeLoadCount"].Should().Be("2");
    }

    [Fact]
    public async Task Can_hide_keys_from_response()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Refresh:ReturnConfiguration"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Configuration.Sources.Add(new TestConfigurationSource());
        builder.Services.AddRefreshActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.PostAsync(new Uri("http://localhost/actuator/refresh"), null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("[]");

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        configuration["FakeLoadCount"].Should().Be("2");
    }

    [Fact]
    public async Task Can_reload_multiple_times()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Configuration.Sources.Add(new TestConfigurationSource());
        builder.Services.AddRefreshActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        _ = await httpClient.PostAsync(new Uri("http://localhost/actuator/refresh"), null, TestContext.Current.CancellationToken);
        _ = await httpClient.PostAsync(new Uri("http://localhost/actuator/refresh"), null, TestContext.Current.CancellationToken);
        _ = await httpClient.PostAsync(new Uri("http://localhost/actuator/refresh"), null, TestContext.Current.CancellationToken);

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        configuration["FakeLoadCount"].Should().Be("4");
    }

    [Fact]
    public async Task Can_change_configuration_at_runtime()
    {
        var fileProvider = new MemoryFileProvider();
        const string appSettingsJsonFileName = "appsettings.json";

        fileProvider.IncludeFile(appSettingsJsonFileName, """
        {
          "Management": {
            "Endpoints": {
              "Refresh": {
                "ReturnConfiguration": false
              }
            }
          }
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Configuration.AddJsonFile(fileProvider, appSettingsJsonFileName, false, true);
        builder.Services.AddRefreshActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response1 = await httpClient.PostAsync(new Uri("http://localhost/actuator/refresh"), null, TestContext.Current.CancellationToken);

        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody1.Should().BeJson("[]");

        fileProvider.ReplaceFile(appSettingsJsonFileName, """
        {
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 = await httpClient.PostAsync(new Uri("http://localhost/actuator/refresh"), null, TestContext.Current.CancellationToken);

        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseArray2 = await response2.Content.ReadFromJsonAsync<JsonArray>(TestContext.Current.CancellationToken);

        responseArray2.Should().NotBeNull();
        string?[] responseKeys2 = [.. responseArray2.Select(node => node?.AsValue().ToString())];
        responseKeys2.Should().NotBeEmpty();
    }
}
