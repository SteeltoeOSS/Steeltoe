// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.All;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class CloudFoundryActuatorTest
{
    private const string VcapApplicationForMock = """
        {
            "cf_api": "https://example.api.com",
            "application_id": "success"
        }
        """;

    private static readonly string MockAccessToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ0b3B0YWwuY29tIiwiZXhwIjoxNDI2NDIwODAwLCJhd2Vzb21lIjp0cnVlfQ." +
        Convert.ToBase64String("signature"u8.ToArray());

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddCloudFoundryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Func<CloudFoundryEndpointMiddleware> middlewareAction = serviceProvider.GetRequiredService<CloudFoundryEndpointMiddleware>;
        middlewareAction.Should().NotThrow();

        Func<PermissionsProvider> providerAction = serviceProvider.GetRequiredService<PermissionsProvider>;
        providerAction.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddCloudFoundryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        CloudFoundryEndpointOptions options = serviceProvider.GetRequiredService<IOptions<CloudFoundryEndpointOptions>>().Value;

        options.ValidateCertificates.Should().BeTrue();
        options.ApplicationId.Should().BeNull();
        options.Api.Should().BeNull();
        options.Enabled.Should().BeNull();
        options.Id.Should().BeEmpty();
        options.Path.Should().BeEmpty();
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/cloudfoundryapplication").Should().Be("/cloudfoundryapplication");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:CloudFoundry:ValidateCertificates"] = "false",
            ["Management:Endpoints:CloudFoundry:ApplicationId"] = "test-app-id",
            ["Management:Endpoints:CloudFoundry:CloudFoundryApi"] = "https://api.domain.com",
            ["Management:Endpoints:CloudFoundry:Enabled"] = "true",
            ["Management:Endpoints:CloudFoundry:Id"] = "test-actuator-id",
            ["Management:Endpoints:CloudFoundry:Path"] = "test-actuator-path",
            ["Management:Endpoints:CloudFoundry:RequiredPermissions"] = "full",
            ["Management:Endpoints:CloudFoundry:AllowedVerbs:0"] = "post"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddCloudFoundryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        CloudFoundryEndpointOptions options = serviceProvider.GetRequiredService<IOptions<CloudFoundryEndpointOptions>>().Value;

        options.ValidateCertificates.Should().BeFalse();
        options.ApplicationId.Should().Be("test-app-id");
        options.Api.Should().Be("https://api.domain.com");
        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/alt-cloudfoundry-app").Should().Be("/alt-cloudfoundry-app/test-actuator-path");
    }

    [Fact]
    public async Task Configures_from_Cloud_Foundry_environment()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", """
            {
                "cf_api": "https://api.system.test-cloud.com",
                "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"
            }
            """);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddCloudFoundry().Build());
        services.AddCloudFoundryActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        CloudFoundryEndpointOptions options = serviceProvider.GetRequiredService<IOptions<CloudFoundryEndpointOptions>>().Value;

        options.ApplicationId.Should().Be("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3");
        options.Api.Should().Be("https://api.system.test-cloud.com");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_expected_data_with_all_actuators_registered(HostBuilderType hostBuilderType)
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", VcapApplicationForMock);

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddCloudFoundry());
            builder.ConfigureServices(services => services.AddAllActuators());
        });

        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await AuthenticatedGetAsync(httpClient, new Uri("http://localhost/cloudfoundryapplication"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "beans": {
                  "href": "http://localhost/cloudfoundryapplication/beans",
                  "templated": false
                },
                "dbmigrations": {
                  "href": "http://localhost/cloudfoundryapplication/dbmigrations",
                  "templated": false
                },
                "env": {
                  "href": "http://localhost/cloudfoundryapplication/env",
                  "templated": false
                },
                "health": {
                  "href": "http://localhost/cloudfoundryapplication/health",
                  "templated": true
                },
                "heapdump": {
                  "href": "http://localhost/cloudfoundryapplication/heapdump",
                  "templated": false
                },
                "httpexchanges": {
                  "href": "http://localhost/cloudfoundryapplication/httpexchanges",
                  "templated": false
                },
                "info": {
                  "href": "http://localhost/cloudfoundryapplication/info",
                  "templated": false
                },
                "loggers": {
                  "href": "http://localhost/cloudfoundryapplication/loggers",
                  "templated": true
                },
                "mappings": {
                  "href": "http://localhost/cloudfoundryapplication/mappings",
                  "templated": false
                },
                "refresh": {
                  "href": "http://localhost/cloudfoundryapplication/refresh",
                  "templated": false
                },
                "threaddump": {
                  "href": "http://localhost/cloudfoundryapplication/threaddump",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/cloudfoundryapplication",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Returns_error_when_not_running_on_Cloud_Foundry()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundry();
        builder.Services.AddCloudFoundryActuator();
        WebApplication host = builder.Build();

        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await AuthenticatedGetAsync(httpClient, new Uri("http://localhost/cloudfoundryapplication"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Fails_at_startup_when_Cloud_Foundry_actuator_not_added()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundry();
        builder.Services.AddInfoActuator();
        WebApplication host = builder.Build();

        Func<Task> action = async () => await host.StartAsync(TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("Running on Cloud Foundry without security middleware. Call services.AddCloudFoundryActuator() to fix this.");
    }

    [Fact]
    public async Task Returns_error_when_Cloud_Foundry_security_middleware_not_activated_from_custom_middleware()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        using var loggerProvider = new CapturingLoggerProvider((category, level) =>
            category.StartsWith("Steeltoe.", StringComparison.Ordinal) && level == LogLevel.Warning);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Logging.AddProvider(loggerProvider);
        builder.Services.AddAllActuators(false);
        WebApplication host = builder.Build();

        host.UseRouting();
        host.UseActuatorEndpoints();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/cloudfoundryapplication"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeEmpty();

        IList<string> logMessages = loggerProvider.GetAll();

        logMessages.Should().Contain($"WARN {typeof(ActuatorMapper)}: Actuators at the /cloudfoundryapplication endpoint are disabled " +
            $"because the Cloud Foundry security middleware is not active. Call UseCloudFoundrySecurity() from your custom middleware pipeline to enable them.");
    }

    [Fact]
    public async Task Returns_only_self_when_no_other_actuators_registered()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", VcapApplicationForMock);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundry();
        builder.Services.AddCloudFoundryActuator();
        await using WebApplication host = builder.Build();

        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await AuthenticatedGetAsync(httpClient, new Uri("http://localhost/cloudfoundryapplication"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "self": {
                  "href": "http://localhost/cloudfoundryapplication",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_disable_CloudFoundry_actuator()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", VcapApplicationForMock);

        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:CloudFoundry:Enabled"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundry();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage rootResponse = await AuthenticatedGetAsync(httpClient, new Uri("http://localhost/cloudfoundryapplication"));

        rootResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage infoResponse = await AuthenticatedGetAsync(httpClient, new Uri("http://localhost/cloudfoundryapplication/info"));

        infoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Can_disable_all_actuators_at_CloudFoundry_URL()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", VcapApplicationForMock);

        var appSettings = new Dictionary<string, string?>
        {
            ["Management:CloudFoundry:Enabled"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundry();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage rootResponse = await AuthenticatedGetAsync(httpClient, new Uri("http://localhost/cloudfoundryapplication"));

        rootResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage infoResponse = await AuthenticatedGetAsync(httpClient, new Uri("http://localhost/cloudfoundryapplication/info"));

        infoResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Hides_disabled_actuators_and_ignores_exposure()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", VcapApplicationForMock);

        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Info:Enabled"] = "false",
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*",
            ["Management:Endpoints:Actuator:Exposure:Exclude:1"] = "loggers"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundry();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        builder.Services.AddLoggersActuator();

        await using WebApplication host = builder.Build();

        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await AuthenticatedGetAsync(httpClient, new Uri("http://localhost/cloudfoundryapplication"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "loggers": {
                  "href": "http://localhost/cloudfoundryapplication/loggers",
                  "templated": true
                },
                "self": {
                  "href": "http://localhost/cloudfoundryapplication",
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
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", VcapApplicationForMock);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundry();
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        await using WebApplication host = builder.Build();

        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();
        httpClient.DefaultRequestHeaders.Add("X-Forwarded-Proto", headerValue);

        HttpResponseMessage response = await AuthenticatedGetAsync(httpClient, new Uri($"{requestUri}/cloudfoundryapplication"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson($$"""
            {
              "type": "steeltoe",
              "_links": {
                "info": {
                  "href": "{{responseUri}}/cloudfoundryapplication/info",
                  "templated": false
                },
                "self": {
                  "href": "{{responseUri}}/cloudfoundryapplication",
                  "templated": false
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_change_configuration_at_runtime()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", VcapApplicationForMock);

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
        builder.Configuration.AddCloudFoundry();
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddCloudFoundryActuator();
        builder.Services.AddInfoActuator();
        builder.Services.AddHealthActuator();
        await using WebApplication host = builder.Build();

        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(CloudControllerPermissionsMock.GetHttpMessageHandler());
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response1 = await AuthenticatedGetAsync(httpClient, new Uri("http://localhost/cloudfoundryapplication"));

        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody1.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "health": {
                  "href": "http://localhost/cloudfoundryapplication/health",
                  "templated": true
                },
                "self": {
                  "href": "http://localhost/cloudfoundryapplication",
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

        HttpResponseMessage response2 = await AuthenticatedGetAsync(httpClient, new Uri("http://localhost/cloudfoundryapplication"));

        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody2.Should().BeJson("""
            {
              "type": "steeltoe",
              "_links": {
                "info": {
                  "href": "http://localhost/cloudfoundryapplication/info",
                  "templated": false
                },
                "self": {
                  "href": "http://localhost/cloudfoundryapplication",
                  "templated": false
                }
              }
            }
            """);
    }

    private static async Task<HttpResponseMessage> AuthenticatedGetAsync(HttpClient httpClient, Uri uri)
    {
        var rootRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
        rootRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", MockAccessToken);
        return await httpClient.SendAsync(rootRequestMessage, TestContext.Current.CancellationToken);
    }
}
