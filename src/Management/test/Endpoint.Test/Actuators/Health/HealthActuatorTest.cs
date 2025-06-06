// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors.FileSystem;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.Contributors.FileSystem;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

public sealed class HealthActuatorTest
{
    private const long OneGigabyte = 1024 * 1024 * 1024;

    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "health"
    };

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddHealthActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Func<HealthEndpointMiddleware> action = serviceProvider.GetRequiredService<HealthEndpointMiddleware>;
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddHealthActuator();
        await using WebApplication host = builder.Build();

        HealthEndpointOptions options = host.Services.GetRequiredService<IOptions<HealthEndpointOptions>>().Value;

        options.ShowComponents.Should().Be(ShowValues.Never);
        options.ShowDetails.Should().Be(ShowValues.Never);
        options.Claim.Should().BeNull();
        options.Role.Should().BeNull();
        options.Groups.Should().BeEmpty();
        options.Enabled.Should().BeNull();
        options.Id.Should().Be("health");
        options.Path.Should().Be("health");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeFalse();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/health/{**_}");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:ShowDetails"] = "WhenAuthorized",
            ["Management:Endpoints:Health:Claim:Type"] = "test-claim-type",
            ["Management:Endpoints:Health:Claim:Value"] = "test-claim-value",
            ["Management:Endpoints:Health:Role"] = "test-role",
            ["Management:Endpoints:Health:Groups:TEST-GROUP-1:include"] = "test-id-1",
            ["Management:Endpoints:Health:Groups:TEST-GROUP-1:ShowDetails"] = "Never",
            ["Management:Endpoints:Health:Groups:test-group-2:include"] = "test-id-2",
            ["Management:Endpoints:Health:Groups:test-group-2:ShowComponents"] = "WhenAuthorized",
            ["Management:Endpoints:Health:Enabled"] = "true",
            ["Management:Endpoints:Health:Id"] = "test-actuator-id",
            ["Management:Endpoints:Health:Path"] = "test-actuator-path",
            ["Management:Endpoints:Health:RequiredPermissions"] = "full",
            ["Management:Endpoints:Health:AllowedVerbs:0"] = "post"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        await using WebApplication host = builder.Build();

        HealthEndpointOptions options = host.Services.GetRequiredService<IOptions<HealthEndpointOptions>>().Value;

        options.ShowComponents.Should().Be(ShowValues.Always);
        options.ShowDetails.Should().Be(ShowValues.WhenAuthorized);
        options.Claim.Should().NotBeNull();
        options.Claim.Type.Should().Be("test-claim-type");
        options.Claim.Value.Should().Be("test-claim-value");
        options.Role.Should().Be("test-role");
        options.Groups.Should().HaveCount(2);

        HealthGroupOptions group1 = options.Groups.Should().ContainKey("test-group-1").WhoseValue;
        group1.Include.Should().Be("test-id-1");
        group1.ShowComponents.Should().BeNull();
        group1.ShowDetails.Should().Be(ShowValues.Never);

        HealthGroupOptions group2 = options.Groups.Should().ContainKey("TEST-GROUP-2").WhoseValue;
        group2.Include.Should().Be("test-id-2");
        group2.ShowComponents.Should().Be(ShowValues.WhenAuthorized);
        group2.ShowDetails.Should().BeNull();

        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeFalse();
        options.GetPathMatchPattern("/alt-actuators").Should().Be("/alt-actuators/test-actuator-path/{**_}");
    }

    [Fact]
    public async Task Configures_claim_when_only_role_specified()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Health:Role"] = "test-role"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        await using WebApplication host = builder.Build();

        HealthEndpointOptions options = host.Services.GetRequiredService<IOptions<HealthEndpointOptions>>().Value;

        options.Claim.Should().NotBeNull();
        options.Claim.Type.Should().Be("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
        options.Claim.Value.Should().Be("test-role");
        options.Role.Should().Be("test-role");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_expected_data(HostBuilderType hostBuilderType)
    {
        string diskSpacePath = Platform.IsWindows ? @"D:\Apps\Data" : "/mnt/apps/data";
        List<FakeDriveInfoWrapper> drives = [new(16 * OneGigabyte, 128 * OneGigabyte, Platform.IsWindows ? @"D:\" : "/")];
        var diskSpaceProvider = new FakeDiskSpaceProvider(Platform.IsWindows, drives, [], [diskSpacePath]);

        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:ShowDetails"] = "Always",
            ["Management:Endpoints:Health:DiskSpace:Path"] = diskSpacePath,
            ["Management:Endpoints:Health:DiskSpace:Threshold"] = $"{5 * OneGigabyte}"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDiskSpaceProvider>(diskSpaceProvider);
                services.AddHealthActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson($$"""
            {
              "status": "UP",
              "components": {
                "diskSpace": {
                  "status": "UP",
                  "details": {
                    "total": {{128 * OneGigabyte}},
                    "free": {{16 * OneGigabyte}},
                    "threshold": {{5 * OneGigabyte}},
                    "path": {{JsonValue.Create(diskSpacePath).ToJsonString()}},
                    "exists": true
                  }
                },
                "ping": {
                  "status": "UP"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Endpoint_returns_expected_data_with_availability_probes()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:ShowDetails"] = "Always",
            ["Management:Endpoints:Health:Liveness:Enabled"] = "true",
            ["Management:Endpoints:Health:Readiness:Enabled"] = "true",
            ["Management:Endpoints:Health:DiskSpace:Enabled"] = "false",
            ["Management:Endpoints:Health:Ping:Enabled"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
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
                "livenessState": {
                  "status": "UP"
                },
                "readinessState": {
                  "status": "UP"
                }
              },
              "groups": [
                "liveness",
                "readiness"
              ]
            }
            """);
    }

    [Fact]
    public async Task Can_override_status_code_in_configuration()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:UseStatusCodeFromResponse"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddHealthContributor<DownContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "DOWN"
            }
            """);
    }

    [Fact]
    public async Task Can_override_status_code_in_request_header()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddHealthContributor<DownContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        var requestMessage = new HttpRequestMessage
        {
            RequestUri = new Uri("http://localhost/actuator/health")
        };

        requestMessage.Headers.Add("X-Use-Status-Code-From-Response", "false");

        HttpResponseMessage response = await httpClient.SendAsync(requestMessage, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "DOWN"
            }
            """);
    }

    [Fact]
    public async Task Fails_on_invalid_group_name()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response =
            await httpClient.GetAsync(new Uri("http://localhost/actuator/health/unknown-group"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_only_liveness_group()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:Liveness:Enabled"] = "true"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<UpContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health/LIVENESS"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "livenessState": {
                  "status": "UP"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Returns_only_readiness_group()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:Readiness:Enabled"] = "true"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<UpContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health/READINESS"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "readinessState": {
                  "status": "UP"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Returns_only_custom_group()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:Groups:AspNet:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:Groups:AspNet:Include"] = "alwaysUp,privateMemory"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<UpContributor>();
        builder.Services.AddHealthChecks().AddPrivateMemoryHealthCheck(1 * OneGigabyte).AddWorkingSetHealthCheck(1 * OneGigabyte);
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health/ASPNET"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "alwaysUp": {
                  "status": "UP"
                },
                "privatememory": {
                  "status": "UP"
                }
              }
            }
            """);
    }

    [Theory]
    // default configuration
    [InlineData(null, null, null, null, false, false, false)]
    // misconfiguration
    [InlineData(null, "Always", null, null, false, false, false)]
    [InlineData(null, "WhenAuthorized", null, null, false, false, false)]
    [InlineData(null, "WhenAuthorized", null, null, true, false, false)]
    [InlineData(null, null, null, "Always", false, false, false)]
    [InlineData(null, null, null, "WhenAuthorized", false, false, false)]
    [InlineData(null, null, null, "WhenAuthorized", true, false, false)]
    // only endpoint configuration
    [InlineData("Always", null, null, null, false, true, false)]
    [InlineData("WhenAuthorized", null, null, null, false, false, false)]
    [InlineData("WhenAuthorized", null, null, null, true, true, false)]
    [InlineData("Always", "Always", null, null, false, true, true)]
    [InlineData("Always", "WhenAuthorized", null, null, false, true, false)]
    [InlineData("Always", "WhenAuthorized", null, null, true, true, true)]
    [InlineData("WhenAuthorized", "Always", null, null, false, false, false)]
    [InlineData("WhenAuthorized", "Always", null, null, true, true, true)]
    [InlineData("WhenAuthorized", "WhenAuthorized", null, null, true, true, true)]
    // only group configuration
    [InlineData(null, null, "Always", null, false, true, false)]
    [InlineData(null, null, "WhenAuthorized", null, false, false, false)]
    [InlineData(null, null, "WhenAuthorized", null, true, true, false)]
    [InlineData(null, null, "Always", "Always", false, true, true)]
    [InlineData(null, null, "WhenAuthorized", "WhenAuthorized", false, false, false)]
    [InlineData(null, null, "WhenAuthorized", "WhenAuthorized", true, true, true)]
    // group supplements endpoint
    [InlineData("Always", null, null, "Always", true, true, true)]
    [InlineData("Always", null, null, "WhenAuthorized", false, true, false)]
    [InlineData("Always", null, null, "WhenAuthorized", true, true, true)]
    [InlineData("WhenAuthorized", null, null, "WhenAuthorized", false, false, false)]
    [InlineData("WhenAuthorized", null, null, "WhenAuthorized", true, true, true)]
    [InlineData(null, "Always", "Always", null, false, true, true)]
    [InlineData(null, "WhenAuthorized", "Always", null, false, true, false)]
    [InlineData(null, "WhenAuthorized", "Always", null, true, true, true)]
    [InlineData(null, "Always", "WhenAuthorized", null, false, false, false)]
    [InlineData(null, "Always", "WhenAuthorized", null, true, true, true)]
    // group overrides endpoint
    [InlineData("Never", "Never", "Always", null, false, true, false)]
    [InlineData("Never", "Never", "WhenAuthorized", null, false, false, false)]
    [InlineData("Never", "Never", "WhenAuthorized", null, true, true, false)]
    [InlineData("Never", "Never", "Always", "Always", false, true, true)]
    [InlineData("Never", "Never", "WhenAuthorized", "WhenAuthorized", false, false, false)]
    [InlineData("Never", "Never", "WhenAuthorized", "WhenAuthorized", true, true, true)]
    [InlineData("Always", null, "Never", null, false, false, false)]
    [InlineData("WhenAuthorized", null, "Never", null, false, false, false)]
    [InlineData("WhenAuthorized", null, "Never", null, true, false, false)]
    [InlineData("Always", null, "WhenAuthorized", null, false, false, false)]
    [InlineData("Always", null, "WhenAuthorized", null, true, true, false)]
    [InlineData("Always", "Always", "WhenAuthorized", null, false, false, false)]
    [InlineData("Always", "Always", "WhenAuthorized", null, true, true, true)]
    [InlineData("Always", "Always", null, "Never", false, true, false)]
    [InlineData("WhenAuthorized", "WhenAuthorized", null, "Never", false, false, false)]
    [InlineData("WhenAuthorized", "WhenAuthorized", null, "Never", true, true, false)]
    public async Task Returns_expected_level_of_detail_at_group(string? endpointShowComponents, string? endpointShowDetails, string? groupShowComponents,
        string? groupShowDetails, bool hasClaim, bool returnComponents, bool returnDetails)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:Claim:Type"] = "test-claim-type",
            ["Management:Endpoints:Health:Claim:Value"] = "test-claim-value",
            ["Management:Endpoints:Health:ShowComponents"] = endpointShowComponents,
            ["Management:Endpoints:Health:ShowDetails"] = endpointShowDetails,
            ["Management:Endpoints:Health:Groups:test-group:Include"] = "alwaysComplexDetails",
            ["Management:Endpoints:Health:Groups:test-group:ShowComponents"] = groupShowComponents,
            ["Management:Endpoints:Health:Groups:test-group:ShowDetails"] = groupShowDetails
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        builder.Services.AddHealthContributor<ComplexDetailsContributor>();
        await using WebApplication host = builder.Build();

        host.Use(async (httpContext, next) =>
        {
            if (hasClaim)
            {
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("test-claim-type", "test-claim-value")]));
            }

            await next(httpContext);
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health/test-group"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var root = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);

        root.Should().NotBeNull();
        root["status"].Should().NotBeNull().And.Subject.ToString().Should().Be("UP");
        root.Should().NotContainKey("groups");

        if (returnComponents)
        {
            JsonObject components = root.Should().ContainKey("components").WhoseValue.Should().BeOfType<JsonObject>().Subject;
            JsonObject firstComponent = components.Should().ContainSingle().Which.Value.Should().BeOfType<JsonObject>().Subject;

            if (returnDetails)
            {
                JsonObject details = firstComponent.Should().ContainKey("details").WhoseValue.Should().BeOfType<JsonObject>().Subject;
                details.Should().NotBeEmpty();
            }
            else
            {
                firstComponent.Should().NotContainKey("details");
            }
        }
        else
        {
            root.Should().NotContainKey("components");
        }
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
              "Health": {
                "Groups": {
                  "ping-group": {
                    "include": "ping",
                    "ShowComponents": "Always"
                  }
                }
              }
            }
          }
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Configuration.AddJsonFile(fileProvider, appSettingsJsonFileName, false, true);
        builder.Services.AddHealthActuator();
        WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response1 =
            await httpClient.GetAsync(new Uri("http://localhost/actuator/health/ping-group"), TestContext.Current.CancellationToken);

        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody1.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "ping": {
                  "status": "UP"
                }
              }
            }
            """);

        fileProvider.ReplaceFile(appSettingsJsonFileName, """
        {
          "Management": {
            "Endpoints": {
              "Health": {
                "Groups": {
                  "ping-group": {
                    "include": "ping"
                  }
                }
              }
            }
          }
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 =
            await httpClient.GetAsync(new Uri("http://localhost/actuator/health/ping-group"), TestContext.Current.CancellationToken);

        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody2.Should().BeJson("""
            {
              "status": "UP"
            }
            """);
    }
}
