// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.All;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

public sealed partial class RouteMappingsActuatorTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*"
    };

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IHostEnvironment, FakeWebHostEnvironment>();
        services.AddRouteMappingsActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Func<RouteMappingsEndpointMiddleware> action = serviceProvider.GetRequiredService<RouteMappingsEndpointMiddleware>;
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddRouteMappingsActuator();
        await using WebApplication host = builder.Build();

        RouteMappingsEndpointOptions options = host.Services.GetRequiredService<IOptions<RouteMappingsEndpointOptions>>().Value;

        options.IncludeActuators.Should().BeTrue();
        options.Enabled.Should().BeNull();
        options.Id.Should().Be("mappings");
        options.Path.Should().Be("mappings");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/mappings");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Mappings:IncludeActuators"] = "false",
            ["Management:Endpoints:Mappings:Enabled"] = "true",
            ["Management:Endpoints:Mappings:Id"] = "test-actuator-id",
            ["Management:Endpoints:Mappings:Path"] = "test-actuator-path",
            ["Management:Endpoints:Mappings:RequiredPermissions"] = "full",
            ["Management:Endpoints:Mappings:AllowedVerbs:0"] = "post"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddRouteMappingsActuator();
        await using WebApplication host = builder.Build();

        RouteMappingsEndpointOptions options = host.Services.GetRequiredService<IOptions<RouteMappingsEndpointOptions>>().Value;

        options.IncludeActuators.Should().BeFalse();
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
    public async Task Returns_no_endpoints_when_all_actuators_disabled(HostBuilderType hostBuilderType)
    {
        Dictionary<string, string?> appSettings = new(AppSettings)
        {
            ["Management:Endpoints:Mappings:IncludeActuators"] = "false"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
            builder.ConfigureServices(services => services.AddAllActuators());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Returns_no_endpoints_and_logs_warning_when_conventional_routing_is_used()
    {
        CapturingLoggerProvider loggerProvider =
            new((category, level) => category.StartsWith("Steeltoe.", StringComparison.Ordinal) && level >= LogLevel.Warning);

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
        builder.ConfigureLogging(loggingBuilder => loggingBuilder.AddProvider(loggerProvider));

        builder.ConfigureServices(services =>
        {
            services.AddRouteMappingsActuator();
            services.AddMvc(options => options.EnableEndpointRouting = false).HideControllersExcept();
        });

        builder.Configure(applicationBuilder => applicationBuilder.UseMvcWithDefaultRoute());
        using IWebHost host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": []
                    }
                  }
                }
              }
            }
            """);

        IList<string> logMessages = loggerProvider.GetAll();
        logMessages.Should().Contain($"WARN {typeof(AspNetEndpointProvider).FullName}: Conventional routing is not supported.");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Returns_endpoints_for_both_routes_on_Cloud_Foundry(HostBuilderType hostBuilderType)
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        Dictionary<string, string?> appSettings = new(AppSettings)
        {
            ["Management:Endpoints:CloudFoundry:Enabled"] = "false"
        };

        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, appSettings, services =>
        {
            services.AddEnvironmentActuator();
            services.AddCloudFoundryActuator();
        });

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/env",
                          "predicate": "{GET [/actuator/env], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/env"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "application/vnd.spring-boot.actuator.v3+json",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": []
                            }
                          }
                        },
                        {
                          "handler": "/cloudfoundryapplication/env",
                          "predicate": "{GET [/cloudfoundryapplication/env], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/cloudfoundryapplication/env"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "application/vnd.spring-boot.actuator.v3+json",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": []
                            }
                          }
                        }
                      ]
                    }
                  }
                }
              }
            }
            """);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Hides_endpoints_at_CloudFoundry_path_when_disabled(HostBuilderType hostBuilderType)
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        Dictionary<string, string?> appSettings = new(AppSettings)
        {
            ["Management:CloudFoundry:Enabled"] = "false"
        };

        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, appSettings, services =>
        {
            services.AddEnvironmentActuator();
            services.AddCloudFoundryActuator();
        });

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/env",
                          "predicate": "{GET [/actuator/env], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/env"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "application/vnd.spring-boot.actuator.v3+json",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": []
                            }
                          }
                        }
                      ]
                    }
                  }
                }
              }
            }
            """);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Hides_endpoints_when_all_verbs_disabled(HostBuilderType hostBuilderType)
    {
        Dictionary<string, string?> appSettings = new(AppSettings)
        {
            ["Management:Endpoints:Loggers:AllowedVerbs:0"] = string.Empty
        };

        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, appSettings, services => services.AddLoggersActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": []
                    }
                  }
                }
              }
            }
            """);
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
              "Mappings": {
                "IncludeActuators": false
              }
            }
          }
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Configuration.AddJsonFile(fileProvider, appSettingsJsonFileName, false, true);
        builder.Services.AddAllActuators();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        var responseNode1 = await httpClient.GetFromJsonAsync<JsonNode>(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        responseNode1.Should().NotBeNull();

        responseNode1["contexts"]?["application"]?["mappings"]?["dispatcherServlets"]?["dispatcherServlet"].Should().BeOfType<JsonArray>().Subject.Should()
            .BeEmpty();

        fileProvider.ReplaceFile(appSettingsJsonFileName, """
        {
        }
        """);

        fileProvider.NotifyChanged();

        var responseNode2 = await httpClient.GetFromJsonAsync<JsonNode>(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        responseNode2.Should().NotBeNull();

        responseNode2["contexts"]?["application"]?["mappings"]?["dispatcherServlets"]?["dispatcherServlet"].Should().BeOfType<JsonArray>().Subject.Should()
            .NotBeEmpty();
    }

    private static async Task<JsonNode> GetRouteMappingsAsync(HostBuilderType hostBuilderType, Dictionary<string, string?> appSettings,
        Action<IServiceCollection>? configureServices, bool removeMappingsActuatorInResponse = true)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

            builder.ConfigureServices(services =>
            {
                services.AddRouteMappingsActuator();
                configureServices?.Invoke(services);
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        var responseNode = await httpClient.GetFromJsonAsync<JsonNode>(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        responseNode.Should().NotBeNull();

        return removeMappingsActuatorInResponse ? RemoveRouteMappingsActuatorInResponse(responseNode) : responseNode;
    }

    private static JsonNode RemoveRouteMappingsActuatorInResponse(JsonNode responseNode)
    {
        JsonNode rootNode = responseNode.DeepClone();
        List<JsonNode> nodesToRemove = [];

        if (rootNode["contexts"]?["application"]?["mappings"]?["dispatcherServlets"]?["dispatcherServlet"] is JsonArray parentNode)
        {
            foreach (JsonNode? node in parentNode)
            {
                if (node?["handler"]?.GetValue<string>().EndsWith("/mappings", StringComparison.Ordinal) == true)
                {
                    nodesToRemove.Add(node);
                }
            }

            foreach (JsonNode nodeToRemove in nodesToRemove)
            {
                parentNode.Remove(nodeToRemove);
            }
        }

        return rootNode;
    }
}
