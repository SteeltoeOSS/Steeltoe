// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
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
    private static readonly string AppFileVersion = AppAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
    private static readonly string AppProductVersion = AppAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    private static readonly Assembly SteeltoeAssembly = typeof(IInfoContributor).Assembly;
    private static readonly string SteeltoeFileVersion = SteeltoeAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
    private static readonly string SteeltoeProductVersion = SteeltoeAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    private static readonly string RuntimeName = RuntimeInformation.FrameworkDescription;
    private static readonly string RuntimeVersion = System.Environment.Version.ToString();
    private static readonly string RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier;

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

        responseBody.Should().BeJson($$"""
            {
              "git": {
                "branch": "924aabdad9eb1da7bfe5b075f9befa2d0b2374e8",
                "build": {
                  "host": "DESKTOP-K6I8LTH",
                  "time": "2017-07-12T18:40:39Z",
                  "user": {
                    "email": "someone@testdomain.com",
                    "name": "John Doe"
                  },
                  "version": "1.5.4.RELEASE"
                },
                "closest": {
                  "tag": {
                    "commit": {
                      "count": "10772"
                    },
                    "name": "v2.0.0.M2"
                  }
                },
                "commit": {
                  "id": "924aabdad9eb1da7bfe5b075f9befa2d0b2374e8",
                  "message": {
                    "full": "Release version 1.5.4.RELEASE",
                    "short": "Release version 1.5.4.RELEASE"
                  },
                  "time": "2017-06-08T12:47:02Z",
                  "user": {
                    "email": "buildmaster@springframework.org",
                    "name": "Spring Buildmaster"
                  }
                },
                "dirty": "true",
                "remote": {
                  "origin": {
                    "url": "https://github.com/spring-projects/spring-boot.git"
                  }
                },
                "tags": "v1.5.4.RELEASE"
              },
              "Some": {
                "Example": {
                  "Key": "ExampleValue"
                }
              },
              "applicationVersionInfo": {
                "ProductName": "{{AppName}}",
                "FileVersion": "{{AppFileVersion}}",
                "ProductVersion": "{{AppProductVersion}}"
              },
              "steeltoeVersionInfo": {
                "ProductName": "Steeltoe.Management.Endpoint",
                "FileVersion": "{{SteeltoeFileVersion}}",
                "ProductVersion": "{{SteeltoeProductVersion}}"
              },
              "build": {
                "version": "{{AppAssemblyVersion}}"
              },
              "runtime": {
                "name": "{{RuntimeName}}",
                "version": "{{RuntimeVersion}}",
                "runtimeIdentifier": "{{RuntimeIdentifier}}"
              }
            }
            """);
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
}
