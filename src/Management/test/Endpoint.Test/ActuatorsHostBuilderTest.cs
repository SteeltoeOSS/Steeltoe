// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint.Actuators.All;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.ManagementPort;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;
using Steeltoe.Management.Endpoint.Test.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ActuatorsHostBuilderTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*"
    };

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task CloudFoundryActuator(HostBuilderType hostBuilderType)
    {
        const string token =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczovL3VhYS5jbG91ZC5jb20vb2F1dGgvdG9rZW4iLCJpYXQiOjE3MzcwNjMxNzYsImV4cCI6MTc2ODU5OTE3NiwiYXVkIjoiYWN0dWF0b3IiLCJzdWIiOiJ1c2VyQGVtYWlsLmNvbSIsInNjb3BlIjpbImFjdHVhdG9yLnJlYWQiLCJjbG91ZF9jb250cm9sbGVyLnVzZXIiXSwiRW1haWwiOiJ1c2VyQGVtYWlsLmNvbSIsImNsaWVudF9pZCI6ImFwcHNfbWFuYWdlcl9qcyIsInVzZXJfbmFtZSI6InVzZXJAZW1haWwuY29tIiwidXNlcl9pZCI6InVzZXIifQ.bfCtDFxcWF8Yuie2p89S8_fTuUkAOd3i9M8PyKDV-N0";

        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", """
            {
                "cf_api": "https://api.cloud.com",
                "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"
            }
            """);

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddCloudFoundry();
            });

            builder.ConfigureServices(services => services.AddCloudFoundryActuator());
        });

        var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Get, "https://api.cloud.com/v2/apps/fa05c1a9-0fc1-4fbd-bae1-139850dec7a3/permissions")
            .WithHeaders("Authorization", $"bearer {token}").Respond("application/json", """
                {
                    "read_sensitive_data": true
                }
                """);

        host.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/cloudfoundryapplication"));
        request.Headers.Authorization = AuthenticationHeaderValue.Parse($"bearer {token}");

        HttpResponseMessage response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("\"http://localhost/cloudfoundryapplication\"");

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HealthActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddHealthActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Be("""{"status":"UP"}""");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HealthActuatorWithProbes(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:Liveness:Enabled"] = "true",
            ["Management:Endpoints:Health:Readiness:Enabled"] = "true"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
            builder.ConfigureServices(services => services.AddHealthActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().Be("""
            {"status":"UP","groups":["liveness","readiness"]}
            """);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HealthActuatorWithComponents(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:Liveness:Enabled"] = "true",
            ["Management:Endpoints:Health:Readiness:Enabled"] = "true"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
            builder.ConfigureServices(services => services.AddHealthActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().Be("""
            {"status":"UP","components":{"ping":{"status":"UP"},"diskSpace":{"status":"UP"},"readinessState":{"status":"UP"},"livenessState":{"status":"UP"}},"groups":["liveness","readiness"]}
            """);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HealthActuatorWithoutDefaultContributors(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:DiskSpace:Enabled"] = "false",
            ["Management:Endpoints:Health:Ping:Enabled"] = "false"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
            builder.ConfigureServices(services => services.AddHealthActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().Be("""{"status":"UNKNOWN"}""");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HealthActuatorWithExtraContributor(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

            builder.ConfigureServices(services =>
            {
                services.AddHealthActuator();
                services.AddHealthContributor<DownContributor>();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().Be("""
            {"status":"DOWN","components":{"alwaysDown":{"status":"DOWN"},"ping":{"status":"UP"},"diskSpace":{"status":"UP"}}}
            """);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HealthActuatorWithAvailability(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:Liveness:Enabled"] = "true",
            ["Management:Endpoints:Health:Readiness:Enabled"] = "true"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
            builder.ConfigureServices(services => services.AddHealthActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage livenessResponse =
            await httpClient.GetAsync(new Uri("http://localhost/actuator/health/liveness"), TestContext.Current.CancellationToken);

        livenessResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string livenessResponseText = await livenessResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        livenessResponseText.Should().Be("""{"status":"UP","components":{"livenessState":{"status":"UP"}}}""");

        HttpResponseMessage readinessResponse =
            await httpClient.GetAsync(new Uri("http://localhost/actuator/health/readiness"), TestContext.Current.CancellationToken);

        readinessResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string readinessResponseText = await readinessResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        readinessResponseText.Should().Be("""{"status":"UP","components":{"readinessState":{"status":"UP"}}}""");

        var availability = host.Services.GetRequiredService<ApplicationAvailability>();
        await host.StopAsync(TestContext.Current.CancellationToken);

        availability.GetLivenessState().Should().Be(LivenessState.Correct);
        availability.GetReadinessState().Should().Be(ReadinessState.RefusingTraffic);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HttpExchangesActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddHttpExchangesActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        _ = await httpClient.GetAsync(new Uri("http://localhost/does-not-exist"), TestContext.Current.CancellationToken);

        await Task.Delay(250.Milliseconds(), TestContext.Current.CancellationToken);

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("/does-not-exist");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HypermediaActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddHypermediaActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task InfoActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddInfoActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/info"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("\"buildmaster@springframework.org\"");
        responseText.Should().Contain("\"Steeltoe.Management.Endpoint\"");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task InfoActuatorWithExtraContributor(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddInfoActuator();
                services.AddInfoContributor<TestInfoContributor>();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/info"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("\"buildmaster@springframework.org\"");
        responseText.Should().Contain("\"Steeltoe.Management.Endpoint\"");
        responseText.Should().Contain("\"IsTest\"");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task LoggersActuatorGet(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddLoggersActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("\"Microsoft.AspNetCore.");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task LoggersActuatorPost(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddLoggersActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        const string requestBody = """
            {
                "configuredLevel": "FATAL"
            }
            """;

        var requestContent = new StringContent(requestBody, MediaTypeHeaderValue.Parse("application/vnd.spring-boot.actuator.v3+json"));

        HttpResponseMessage postResponse = await httpClient.PostAsync(new Uri("http://localhost/actuator/loggers/Microsoft"), requestContent,
            TestContext.Current.CancellationToken);

        postResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"), TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("\"Microsoft.AspNetCore\":{\"effectiveLevel\":\"FATAL\"}");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task LoggersActuatorWithDynamicSerilog(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureLogging(loggingBuilder => loggingBuilder.AddDynamicSerilog());
            builder.ConfigureServices(services => services.AddLoggersActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("\"Microsoft.AspNetCore.");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task RefreshActuatorGet(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddRefreshActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/refresh"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task RefreshActuatorPost(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddRefreshActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.PostAsync(new Uri("http://localhost/actuator/refresh"), null, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("\"Management:Endpoints:Actuator:Exposure:Include\"");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task RouteMappingsActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddRouteMappingsActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("\"dispatcherServlet\"");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task ServicesActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddServicesActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/beans"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseText.Should().Contain("\"Microsoft.Extensions.Configuration.IConfiguration\"");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task AddAllActuatorsDoesNotRegisterDuplicateServices(HostBuilderType hostBuilderType)
    {
        const int actuatorCount = 12;

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddAllActuators();
                services.AddAllActuators();
            });
        });

        host.Services.GetServices<ActuatorEndpointMapper>().Should().ContainSingle();
        host.Services.GetServices<IConfigureOptions<CorsOptions>>().OfType<ConfigureActuatorsCorsPolicyOptions>().Should().ContainSingle();
        host.Services.GetServices<IConfigureOptions<ManagementOptions>>().Should().ContainSingle();
        host.Services.GetServices<IOptionsChangeTokenSource<ManagementOptions>>().Should().ContainSingle();

        host.Services.GetServices<IEndpointOptionsMonitorProvider>().Should().HaveCount(actuatorCount);
        host.Services.GetServices<IEndpointMiddleware>().Should().HaveCount(actuatorCount);

        IStartupFilter[] startupFilters = [.. host.Services.GetServices<IStartupFilter>()];
        startupFilters.Should().ContainSingle(filter => filter is ConfigureActuatorsMiddlewareStartupFilter);
        startupFilters.Should().ContainSingle(filter => filter is ManagementPortStartupFilter);
        startupFilters.Should().ContainSingle(filter => filter is AvailabilityStartupFilter);

        host.Services.GetServices<IConfigureOptions<InfoEndpointOptions>>().Should().ContainSingle();
        host.Services.GetServices<IOptionsChangeTokenSource<InfoEndpointOptions>>().Should().ContainSingle();
        host.Services.GetServices<IEndpointOptionsMonitorProvider>().OfType<EndpointOptionsMonitorProvider<InfoEndpointOptions>>().Should().ContainSingle();
        host.Services.GetServices<IInfoEndpointHandler>().OfType<InfoEndpointHandler>().Should().ContainSingle();
        host.Services.GetServices<InfoEndpointMiddleware>().Should().ContainSingle();
        host.Services.GetServices<IEndpointMiddleware>().OfType<InfoEndpointMiddleware>().Should().ContainSingle();
    }
}
