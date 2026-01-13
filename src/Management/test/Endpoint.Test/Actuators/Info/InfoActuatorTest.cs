// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Test.Actuators.Info.TestContributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info;

public sealed class InfoActuatorTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "info"
    };

    private static readonly Assembly AppAssembly = Assembly.GetEntryAssembly()!;
    private static readonly string AppName = AppAssembly.GetName().Name!;
    private static readonly string AppAssemblyVersion = AppAssembly.GetName().Version!.ToString();

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => serviceProvider.GetRequiredService<InfoEndpointMiddleware>();

        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        InfoEndpointOptions options = serviceProvider.GetRequiredService<IOptions<InfoEndpointOptions>>().Value;

        options.Enabled.Should().BeNull();
        options.Id.Should().Be("info");
        options.Path.Should().Be("info");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/info");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Info:Enabled"] = "true",
            ["Management:Endpoints:Info:Id"] = "test-actuator-id",
            ["Management:Endpoints:Info:Path"] = "test-actuator-path",
            ["Management:Endpoints:Info:RequiredPermissions"] = "full",
            ["Management:Endpoints:Info:AllowedVerbs:0"] = "post"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddInfoActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        InfoEndpointOptions options = serviceProvider.GetRequiredService<IOptions<InfoEndpointOptions>>().Value;

        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
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
            ["Info:Some:Example:Key"] = "ExampleValue"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
            builder.ConfigureServices(services => services.AddInfoActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Parse the response to verify structure
        using JsonDocument json = JsonDocument.Parse(responseBody);

        // Verify runtime info is present (values depend on runtime so we check separately)
        json.RootElement.TryGetProperty("runtime", out JsonElement runtimeElement).Should().BeTrue();
        runtimeElement.TryGetProperty("name", out _).Should().BeTrue();
        runtimeElement.TryGetProperty("version", out _).Should().BeTrue();
        runtimeElement.TryGetProperty("runtimeIdentifier", out _).Should().BeTrue();

        // Verify other expected fields are present
        json.RootElement.TryGetProperty("git", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("Some", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("applicationVersionInfo", out JsonElement appVersionInfo).Should().BeTrue();
        appVersionInfo.GetProperty("ProductName").GetString().Should().Be(AppName);
        json.RootElement.TryGetProperty("steeltoeVersionInfo", out JsonElement steeltoeVersionInfo).Should().BeTrue();
        steeltoeVersionInfo.GetProperty("ProductName").GetString().Should().Be("Steeltoe.Management.Endpoint");
        json.RootElement.TryGetProperty("build", out JsonElement buildElement).Should().BeTrue();
        buildElement.GetProperty("version").GetString().Should().Be(AppAssemblyVersion);
    }

    [Fact]
    public async Task Endpoint_returns_empty_without_any_contributors()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddInfoActuator();
        builder.Services.RemoveAll<IInfoContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("{}");
    }

    [Fact]
    public async Task Endpoint_returns_data_from_custom_contributor()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddInfoActuator();
        builder.Services.RemoveAll<IInfoContributor>();
        builder.Services.AddInfoContributor<FakeContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "TestKey": "TestValue",
              "TestTime": "2021-07-19T03:41:55.003Z"
            }
            """);
    }

    [Fact]
    public async Task Can_use_custom_serializer_options()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:CustomJsonConverters:0"] = "Steeltoe.Management.Endpoint.Actuators.Info.EpochSecondsDateTimeConverter"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddInfoActuator();
        builder.Services.RemoveAll<IInfoContributor>();
        builder.Services.AddInfoContributor<FakeContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "TestKey": "TestValue",
              "TestTime": 1626666115003
            }
            """);
    }

    [Fact]
    public async Task Logs_warning_and_resumes_when_contributor_throws()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level == LogLevel.Warning);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Logging.AddProvider(loggerProvider);
        builder.Services.AddInfoActuator();
        builder.Services.RemoveAll<IInfoContributor>();
        builder.Services.AddInfoContributor<ThrowingContributor>();
        builder.Services.AddInfoContributor<FakeContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "TestKey": "TestValue",
              "TestTime": "2021-07-19T03:41:55.003Z"
            }
            """);

        IList<string> logMessages = loggerProvider.GetAll();

        logMessages.Should().ContainSingle().Which.Should().Be(
            $"WARN {typeof(InfoEndpointHandler)}: Exception thrown by contributor '{typeof(ThrowingContributor)}' while contributing to info endpoint.");
    }

    [Fact]
    public async Task Adds_contributor_type_once()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddInfoContributor<FakeContributor>();
        builder.Services.AddInfoContributor<FakeContributor>();
        await using WebApplication host = builder.Build();

        host.Services.GetServices<IInfoContributor>().Should().ContainSingle();
    }

    [Fact]
    public async Task Endpoint_includes_runtime_information()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/info"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        JsonDocument json = JsonDocument.Parse(responseBody);

        json.RootElement.TryGetProperty("runtime", out JsonElement runtimeElement).Should().BeTrue();
        runtimeElement.TryGetProperty("name", out JsonElement nameElement).Should().BeTrue();
        runtimeElement.TryGetProperty("version", out JsonElement versionElement).Should().BeTrue();
        runtimeElement.TryGetProperty("runtimeIdentifier", out JsonElement ridElement).Should().BeTrue();

        nameElement.GetString().Should().NotBeNullOrEmpty();
        versionElement.GetString().Should().NotBeNullOrEmpty();
        ridElement.GetString().Should().NotBeNullOrEmpty();
    }
}
