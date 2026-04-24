// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;
using MicrosoftHealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

public sealed class HealthAggregationTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "health",
        ["Management:Endpoints:Health:ShowComponents"] = "Always",
        ["Management:Endpoints:Health:ShowDetails"] = "Always",
        ["Management:Endpoints:Health:Ping:Enabled"] = "false",
        ["Management:Endpoints:Health:DiskSpace:Enabled"] = "false"
    };

    [Fact]
    public async Task No_contributors()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UNKNOWN"
            }
            """);
    }

    [Fact]
    public async Task Only_disabled_contributor()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<DisabledContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UNKNOWN"
            }
            """);
    }

    [Fact]
    public async Task Only_unknown_contributor()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<UnknownContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UNKNOWN",
              "components": {
                "alwaysUnknown": {
                  "status": "UNKNOWN"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Only_up_contributor()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<UpContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "alwaysUp": {
                  "status": "UP"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Only_warning_contributor()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<WarningContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "WARNING",
              "components": {
                "alwaysWarning": {
                  "status": "WARNING"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Only_out_of_service_Contributor()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<OutOfServiceContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "OUT_OF_SERVICE",
              "components": {
                "alwaysOutOfService": {
                  "status": "OUT_OF_SERVICE"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Only_down_contributor()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<DownContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "DOWN",
              "components": {
                "alwaysDown": {
                  "status": "DOWN"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Only_complex_contributor()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<ComplexDetailsContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "alwaysComplexDetails": {
                  "status": "UP",
                  "description": "test-description",
                  "details": {
                    "ComplexType": {
                      "testString": "test-string",
                      "testInteger": 123,
                      "testFloatingPoint": 1.23,
                      "testBoolean": true,
                      "nestedComplexType": {
                        "testString": "nested-test-string",
                        "testInteger": -1,
                        "testFloatingPoint": 0,
                        "testBoolean": false,
                        "testList": [],
                        "testDictionary": {}
                      },
                      "testList": [
                        "A",
                        "B",
                        "C"
                      ],
                      "testDictionary": {
                        "One": 1,
                        "Two": 2,
                        "Three": 3
                      }
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Adds_contributor_type_once()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddHealthContributor<UpContributor>();
        builder.Services.AddHealthContributor<UpContributor>();
        await using WebApplication host = builder.Build();

        host.Services.GetServices<IHealthContributor>().Should().ContainSingle();
    }

    [Fact]
    public async Task Renames_contributors_with_same_ID_and_orders_by_descending_status()
    {
        List<IHealthContributor> contributors =
        [
            new UpContributor(),
            new OutOfServiceContributor(),
            new UnknownContributor(),
            new UpContributor(),
            new DisabledContributor(),
            new DownContributor(),
            new ThrowingContributor(),
            new WarningContributor(),
            new UpContributor()
        ];

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "DOWN",
              "components": {
                "alwaysDown": {
                  "status": "DOWN"
                },
                "alwaysOutOfService": {
                  "status": "OUT_OF_SERVICE"
                },
                "alwaysWarning": {
                  "status": "WARNING"
                },
                "alwaysThrowing": {
                  "status": "UNKNOWN"
                },
                "alwaysUnknown": {
                  "status": "UNKNOWN"
                },
                "alwaysUp": {
                  "status": "UP"
                },
                "alwaysUp-1": {
                  "status": "UP"
                },
                "alwaysUp-2": {
                  "status": "UP"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Aggregates_contributors_in_parallel()
    {
        List<IHealthContributor> contributors =
        [
            new SlowContributor(1.Seconds()),
            new SlowContributor(2.Seconds()),
            new SlowContributor(3.Seconds())
        ];

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        stopwatch.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "alwaysSlow": {
                  "status": "UP"
                },
                "alwaysSlow-1": {
                  "status": "UP"
                },
                "alwaysSlow-2": {
                  "status": "UP"
                }
              }
            }
            """);

        stopwatch.Elapsed.Should().BeGreaterThan(500.Milliseconds()).And.BeLessThan(5.Seconds());
    }

    [Fact]
    public async Task Invokes_each_contributor_once()
    {
        List<ObservableContributor> contributors =
        [
            new(),
            new(),
            new()
        ];

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        foreach (ObservableContributor contributor in contributors)
        {
            contributor.InvocationCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task Propagates_cancellation()
    {
        List<IHealthContributor> contributors = [new SlowContributor(5.Seconds())];
        await using ServiceProvider emptyServiceProvider = new ServiceCollection().BuildServiceProvider(true);
        var aggregator = new HealthAggregator();

        using var source = new CancellationTokenSource();
        source.CancelAfter(1.Seconds());

        // ReSharper disable AccessToDisposedClosure
        Func<Task> action = async () => await aggregator.AggregateAsync(contributors, [], emptyServiceProvider, source.Token);
        // ReSharper restore AccessToDisposedClosure

        await action.Should().ThrowExactlyAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task Converts_AspNet_health_check_results()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();

        IHealthChecksBuilder checksBuilder = builder.Services.AddHealthChecks();
        checksBuilder.AddCheck<AspNetThrowingCheck>("aspnet-throwing-check");
        checksBuilder.AddCheck<AspNetUnhealthyCheck>("aspnet-unhealthy-check");
        checksBuilder.AddCheck<AspNetDegradedCheck>("aspnet-degraded-check");
        checksBuilder.AddCheck<AspNetHealthyCheck>("aspnet-healthy-check");

        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "DOWN",
              "components": {
                "aspnet-unhealthy-check": {
                  "status": "DOWN",
                  "description": "unhealthy-description",
                  "details": {
                    "unhealthy-data-key": "unhealthy-data-value"
                  }
                },
                "aspnet-degraded-check": {
                  "status": "WARNING",
                  "description": "degraded-description",
                  "details": {
                    "degraded-data-key": "degraded-data-value"
                  }
                },
                "aspnet-throwing-check": {
                  "status": "UNKNOWN",
                  "details": {
                    "exception": "test-exception"
                  }
                },
                "aspnet-healthy-check": {
                  "status": "UP",
                  "description": "healthy-description",
                  "details": {
                    "healthy-data-key": "healthy-data-value"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_skip_AspNet_health_check()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();

        IHealthChecksBuilder checksBuilder = builder.Services.AddHealthChecks();
        checksBuilder.AddCheck<AspNetUnhealthyCheck>("aspnet-unhealthy-check", tags: ["ExcludeFromHealthActuator"]);
        checksBuilder.AddCheck<AspNetHealthyCheck>("aspnet-healthy-check");

        await using WebApplication host = builder.Build();

        host.MapHealthChecks("/health");
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string actuatorResponseBody = await actuatorResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        actuatorResponseBody.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "aspnet-healthy-check": {
                  "status": "UP",
                  "description": "healthy-description",
                  "details": {
                    "healthy-data-key": "healthy-data-value"
                  }
                }
              }
            }
            """);

        HttpResponseMessage aspNetResponse = await httpClient.GetAsync(new Uri("http://localhost/health"), TestContext.Current.CancellationToken);

        aspNetResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string aspNetResponseBody = await aspNetResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        aspNetResponseBody.Should().Be("Unhealthy");
    }

    [Fact]
    public async Task Can_use_scoped_AspNet_health_check()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        builder.Services.AddHealthChecks().AddDbContextCheck<TestDbContext>();
        builder.Services.AddHealthActuator();
        await using WebApplication host = builder.Build();

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => host.Services.GetRequiredService<TestDbContext>();
        action.Should().ThrowExactly<InvalidOperationException>();

        host.MapHealthChecks("/health");
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string actuatorResponseBody = await actuatorResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        actuatorResponseBody.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "TestDbContext": {
                  "status": "UP"
                }
              }
            }
            """);

        HttpResponseMessage aspNetResponse = await httpClient.GetAsync(new Uri("http://localhost/health"), TestContext.Current.CancellationToken);

        aspNetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string aspNetResponseBody = await aspNetResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        aspNetResponseBody.Should().Be("Healthy");
    }

    private sealed class AspNetHealthyCheck : IHealthCheck
    {
        public async Task<MicrosoftHealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            return MicrosoftHealthCheckResult.Healthy("healthy-description", new Dictionary<string, object>
            {
                ["healthy-data-key"] = "healthy-data-value"
            });
        }
    }

    private sealed class AspNetDegradedCheck : IHealthCheck
    {
        public async Task<MicrosoftHealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            return MicrosoftHealthCheckResult.Degraded("degraded-description", null, new Dictionary<string, object>
            {
                ["degraded-data-key"] = "degraded-data-value"
            });
        }
    }

    private sealed class AspNetUnhealthyCheck : IHealthCheck
    {
        public async Task<MicrosoftHealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            return MicrosoftHealthCheckResult.Unhealthy("unhealthy-description", null, new Dictionary<string, object>
            {
                ["unhealthy-data-key"] = "unhealthy-data-value"
            });
        }
    }

    private sealed class AspNetThrowingCheck : IHealthCheck
    {
        public Task<MicrosoftHealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("test-exception");
        }
    }

    private sealed class TestDbContext(DbContextOptions options)
        : DbContext(options);
}
