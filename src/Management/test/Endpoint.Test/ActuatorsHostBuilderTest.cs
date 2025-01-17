// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
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
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Metrics;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/cloudfoundryapplication"));
        request.Headers.Authorization = AuthenticationHeaderValue.Parse($"bearer {token}");

        HttpResponseMessage response = await httpClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Contain("\"http://localhost/cloudfoundryapplication\"");

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task DbMigrationsActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddDbMigrationsActuator());
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/dbmigrations"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task EnvironmentActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddEnvironmentActuator());
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/env"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Be("""{"status":"UP"}""");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HealthActuatorWithDetails(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            { "Management:Endpoints:Health:ShowDetails", "Always" }
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
            builder.ConfigureServices(services => services.AddHealthActuator());
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Contain("\"diskSpace\":");
        responseText.Should().Contain("\"readiness\":");
        responseText.Should().Contain("\"liveness\":");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HealthActuatorWithoutDefaultContributors(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            { "Management:Endpoints:Health:ShowDetails", "Always" },
            { "Management:Endpoints:Health:DiskSpace:Enabled", "false" },
            { "Management:Endpoints:Health:Liveness:Enabled", "false" },
            { "Management:Endpoints:Health:Ping:Enabled", "false" },
            { "Management:Endpoints:Health:Readiness:Enabled", "false" }
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
            builder.ConfigureServices(services => services.AddHealthActuator());
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
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
            { "Management:Endpoints:Health:ShowDetails", "Always" }
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"));
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Contain("\"diskSpace\":");
        responseText.Should().Contain("\"readiness\":");
        responseText.Should().Contain("\"liveness\":");
        responseText.Should().Contain("\"alwaysDown\":");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HealthActuatorWithAvailability(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            { "Management:Endpoints:Health:ShowDetails", "Always" }
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
            builder.ConfigureServices(services => services.AddHealthActuator());
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage livenessResponse = await httpClient.GetAsync(new Uri("http://localhost/actuator/health/liveness"));
        livenessResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string livenessResponseText = await livenessResponse.Content.ReadAsStringAsync();
        livenessResponseText.Should().Contain("\"CORRECT\"");

        HttpResponseMessage readinessResponse = await httpClient.GetAsync(new Uri("http://localhost/actuator/health/readiness"));
        readinessResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string readinessResponseText = await readinessResponse.Content.ReadAsStringAsync();
        readinessResponseText.Should().Contain("\"ACCEPTING_TRAFFIC\"");

        var availability = host.Services.GetRequiredService<ApplicationAvailability>();
        await host.StopAsync();

        availability.GetLivenessState().Should().Be(LivenessState.Correct);
        availability.GetReadinessState().Should().Be(ReadinessState.RefusingTraffic);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task HeapDumpActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddHeapDumpActuator());
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/heapdump"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        _ = await httpClient.GetAsync(new Uri("http://localhost/does-not-exist"));

        await Task.Delay(250);

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator"));
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/info"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/info"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        const string requestBody = """
            {
                "configuredLevel": "FATAL"
            }
            """;

        var requestContent = new StringContent(requestBody, MediaTypeHeaderValue.Parse("application/vnd.spring-boot.actuator.v3+json"));
        HttpResponseMessage postResponse = await httpClient.PostAsync(new Uri("http://localhost/actuator/loggers/Microsoft"), requestContent);
        postResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"));
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await getResponse.Content.ReadAsStringAsync();
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Contain("\"Microsoft.AspNetCore.");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task MetricsActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddMetricsActuator());
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/metrics"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Contain("\"clr.cpu.count\"");
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/refresh"));
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.PostAsync(new Uri("http://localhost/actuator/refresh"), null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/beans"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Contain("\"Microsoft.Extensions.Configuration.IConfiguration\"");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task ThreadDumpActuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
            builder.ConfigureServices(services => services.AddThreadDumpActuator());
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/threaddump"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Contain("\"stackTrace\"");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task AddAllActuatorsDoesNotRegisterDuplicateServices(HostBuilderType hostBuilderType)
    {
        const int actuatorCount = 13;

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

        IStartupFilter[] startupFilters = host.Services.GetServices<IStartupFilter>().ToArray();
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
