// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.All;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;
using Steeltoe.Management.Endpoint.Actuators.RouteMappingsNew;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappingsNew;

public sealed class ActuatorRoutesTest
{
    private static readonly JsonSerializerOptions TestSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*",
        ["Management:Endpoints:SerializerOptions:WriteIndented"] = "true"
    };

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Can_get_routes_for_all_actuators_disabled(HostBuilderType hostBuilderType)
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

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();

        responseText.Should().Be("""
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

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Can_get_routes_for_CloudFoundry_actuator(HostBuilderType hostBuilderType)
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddRouteMappingsActuator();
                services.AddCloudFoundryActuator();
            });
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var responseNode = await httpClient.GetFromJsonAsync<JsonNode>(new Uri("http://localhost/actuator/mappings"));

        responseNode.Should().NotBeNull();
        JsonNode remainingResponseNode = ExcludeRoutesToMappingsActuator(responseNode!);

        remainingResponseNode.ToJsonString(TestSerializerOptions).Should().Be("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP: Get /cloudfoundryapplication",
                          "predicate": "{GET [/cloudfoundryapplication]}",
                          "details": {
                            "requestMappingConditions": {
                              "patterns": [
                                "/cloudfoundryapplication"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [],
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
    public async Task Can_get_routes_for_DbMigrations_actuator(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddRouteMappingsActuator();
                services.AddDbMigrationsActuator();
            });
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var responseNode = await httpClient.GetFromJsonAsync<JsonNode>(new Uri("http://localhost/actuator/mappings"));

        responseNode.Should().NotBeNull();
        JsonNode remainingResponseNode = ExcludeRoutesToMappingsActuator(responseNode!);

        remainingResponseNode.ToJsonString(TestSerializerOptions).Should().Be("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP: Get /actuator/dbmigrations",
                          "predicate": "{GET [/actuator/dbmigrations]}",
                          "details": {
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/dbmigrations"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [],
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

    // TODO: Add tests for the remaining actuators from ActuatorsHostBuilderTest.
    // Consider refactoring InnerMap to middleware, so that actuators can provide metadata.

    private static JsonNode ExcludeRoutesToMappingsActuator(JsonNode responseNode)
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
