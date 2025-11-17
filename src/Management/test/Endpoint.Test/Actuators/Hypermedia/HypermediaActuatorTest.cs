// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.All;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Services;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Hypermedia;

public sealed class HypermediaActuatorTest
{
    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddHypermediaActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => serviceProvider.GetRequiredService<HypermediaEndpointMiddleware>();

        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddHypermediaActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        HypermediaEndpointOptions options = serviceProvider.GetRequiredService<IOptions<HypermediaEndpointOptions>>().Value;

        options.Enabled.Should().BeNull();
        options.Id.Should().BeEmpty();
        options.Path.Should().BeEmpty();
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Enabled"] = "true",
            ["Management:Endpoints:Actuator:Id"] = "test-actuator-id",
            ["Management:Endpoints:Actuator:Path"] = "test-actuator-path",
            ["Management:Endpoints:Actuator:RequiredPermissions"] = "full",
            ["Management:Endpoints:Actuator:AllowedVerbs:0"] = "post"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddHypermediaActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        HypermediaEndpointOptions options = serviceProvider.GetRequiredService<IOptions<HypermediaEndpointOptions>>().Value;

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
    public async Task Endpoint_returns_expected_data_with_all_actuators_registered(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder => builder.ConfigureServices(services => services.AddAllActuators()));

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "health": {
                  "href": "http://localhost/actuator/health",
                  "templated": false
                },
                "info": {
                  "href": "http://localhost/actuator/info",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/actuator",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Returns_only_self_when_no_other_actuators_registered()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddHypermediaActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "self": {
                  "href": "http://localhost/actuator",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_use_alternate_IDs_and_paths()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Path"] = "alt-actuator",
            ["Management:Endpoints:Actuator:Path"] = "hypermedia",
            ["Management:Endpoints:Info:Id"] = "alt-info-id",
            ["Management:Endpoints:Info:Path"] = "alt-info-path",
            ["Management:Endpoints:Loggers:Path"] = "alt-loggers-path",
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "hypermedia",
            ["Management:Endpoints:Actuator:Exposure:Include:1"] = "alt-info-id",
            ["Management:Endpoints:Actuator:Exposure:Include:2"] = "loggers"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHypermediaActuator();
        builder.Services.AddInfoActuator();
        builder.Services.AddLoggersActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/alt-actuator/hypermedia"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "alt-info-id": {
                  "href": "http://localhost/alt-actuator/alt-info-path",
                  "templated": false
                },
                "loggers": {
                  "href": "http://localhost/alt-actuator/alt-loggers-path",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/alt-actuator/hypermedia",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_exclude_hypermedia_actuator()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Id"] = "hypermedia",
            ["Management:Endpoints:Actuator:Path"] = string.Empty,
            ["Management:Endpoints:Actuator:Exposure:Exclude:0"] = "hypermedia"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHypermediaActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Can_disable_hypermedia_actuator()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Enabled"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHypermediaActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Hides_excluded_and_disabled_actuators()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Info:Enabled"] = "false",
            ["Management:Endpoints:Loggers:Id"] = "alt-loggers",
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*",
            ["Management:Endpoints:Actuator:Exposure:Exclude:0"] = "health",
            ["Management:Endpoints:Actuator:Exposure:Exclude:1"] = "alt-loggers"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHypermediaActuator();
        builder.Services.AddInfoActuator();
        builder.Services.AddLoggersActuator();
        builder.Services.AddServicesActuator();
        builder.Services.AddHealthActuator();

        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "beans": {
                  "href": "http://localhost/actuator/beans",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/actuator",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Logs_warning_when_duplicate_endpoint_ID_detected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Info:Id"] = "same",
            ["Management:Endpoints:Loggers:Id"] = "same",
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*"
        };

        using var loggerProvider = new CapturingLoggerProvider((_, level) => level == LogLevel.Warning);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddProvider(loggerProvider);
        builder.Services.AddHypermediaActuator();
        builder.Services.AddInfoActuator();
        builder.Services.AddLoggersActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        IList<string> logLines = loggerProvider.GetAll();
        logLines.Should().ContainSingle().Which.Should().Be($"WARN {typeof(HypermediaService)}: Duplicate endpoint with ID 'same' detected.");
    }

    [Fact]
    public async Task Can_use_slash_as_management_path()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Path"] = "/"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHypermediaActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "info": {
                  "href": "http://localhost/info",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/",
                  "templated": false
                }
              }
            }
            """);
    }

    [Theory]
    [InlineData("http://somehost:1234", "https://somehost:1234", "https")]
    [InlineData("http://somehost:443", "https://somehost", "https")]
    [InlineData("http://somehost:80", "http://somehost", "http")]
    [InlineData("http://somehost:8080", "http://somehost:8080", "http")]
    public async Task Converts_scheme_and_port_behind_load_balancer(string requestUri, string responseUri, string headerValue)
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddHypermediaActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();
        httpClient.DefaultRequestHeaders.Add("X-Forwarded-Proto", headerValue);

        HttpResponseMessage response = await httpClient.GetAsync(new Uri($"{requestUri}/actuator"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson($$"""
            {
              "type": "steeltoe",
              "_links": {
                "info": {
                  "href": "{{responseUri}}/actuator/info",
                  "templated": false
                },
                "self": {
                  "href": "{{responseUri}}/actuator",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_change_configuration_at_runtime()
    {
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
          "Management": {
            "Endpoints": {
              "Info": {
                "Enabled": false
              }
            }
          }
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddHypermediaActuator();
        builder.Services.AddInfoActuator();
        builder.Services.AddHealthActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);

        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody1.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "health": {
                  "href": "http://localhost/actuator/health",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/actuator",
                  "templated": false
                }
              }
            }
            """);

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
          "Management": {
            "Endpoints": {
              "Health": {
                "Enabled": false
              }
            }
          }
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);

        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody2.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "info": {
                  "href": "http://localhost/actuator/info",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/actuator",
                  "templated": false
                }
              }
            }
            """);
    }
}
