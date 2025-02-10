// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes;

public sealed class MinimalApiTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*",
        ["Management:Endpoints:Mappings:IncludeActuators"] = "false",
        ["Management:Endpoints:SerializerOptions:WriteIndented"] = "true"
    };

    [Fact]
    public async Task Can_get_routes_for_handler_method()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        await using WebApplication host = builder.Build();

        host.MapGet("/api/ping", HandlePingRequest);

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
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP: GET /api/ping => HandlePingRequest",
                          "predicate": "{GET [/api/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest",
                              "name": "HandlePingRequest",
                              "descriptor": "System.String HandlePingRequest()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/ping"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
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

    [Fact]
    public async Task Can_get_routes_for_inline_lambda()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        await using WebApplication host = builder.Build();

        host.MapGet("/api/ping", () => "pong");

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
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP: GET /api/ping",
                          "predicate": "{GET [/api/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest+<>c",
                              "name": "<Can_get_routes_for_inline_lambda>b__2_0",
                              "descriptor": "System.String <Can_get_routes_for_inline_lambda>b__2_0()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/ping"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
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

    [Fact]
    public async Task Can_get_routes_for_inline_lambda_with_parameters_and_annotations()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        await using WebApplication host = builder.Build();

        // @formatter:wrap_chained_method_calls chop_always
        host.MapGet("/routes/{featureName?}", (IServiceProvider serviceProvider, string? featureName, [FromBody] string requestBody,
                [FromHeader(Name = "X-Media-Version")] string mediaVersion, [FromHeader(Name = "X-Include-Details")] string? includeDetails, int pageNumber,
                int pageSize = 10) =>
            {
                _ = serviceProvider;
            })
            .WithName("TestName")
            .WithGroupName("TestGroupName")
            .WithDisplayName("TestDisplayName")
            .WithSummary("TestSummary")
            .WithDescription("TestDescription")
            .WithTags("TestTag1", "TestTag2")
            .Accepts(typeof(string), true, "application/json", "*/*")
            .Produces(200, null, "application/vnd.spring-boot.actuator.v3+json", "application/vnd.spring-boot.actuator.v2+json")
            .Produces(404, null, "application/json");
        // @formatter:wrap_chained_method_calls restore

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
                      "dispatcherServlet": [
                        {
                          "handler": "TestDisplayName",
                          "predicate": "{GET [/routes/{featureName?}], headers [X-Media-Version, X-Include-Details], produces [application/vnd.spring-boot.actuator.v3+json || application/vnd.spring-boot.actuator.v2+json || application/json], consumes [application/json || */*]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest+<>c",
                              "name": "<Can_get_routes_for_inline_lambda_with_parameters_and_annotations>b__3_0",
                              "descriptor": "Void <Can_get_routes_for_inline_lambda_with_parameters_and_annotations>b__3_0(System.IServiceProvider, System.String, System.String, System.String, System.String, Int32, Int32)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/routes/{featureName?}"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "*/*",
                                  "negated": false
                                }
                              ],
                              "produces": [
                                {
                                  "mediaType": "application/vnd.spring-boot.actuator.v3+json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "application/vnd.spring-boot.actuator.v2+json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                }
                              ],
                              "headers": [
                                {
                                  "name": "X-Media-Version",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "X-Include-Details",
                                  "required": false,
                                  "negated": false
                                }
                              ],
                              "params": [
                                {
                                  "name": "featureName",
                                  "required": false,
                                  "negated": false
                                },
                                {
                                  "name": "pageNumber",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "pageSize",
                                  "value": 10,
                                  "required": false,
                                  "negated": false
                                }
                              ]
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

    [Fact]
    public async Task Can_get_routes_for_multiple_verbs_in_single_endpoint()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        await using WebApplication host = builder.Build();

        List<string> httpMethods =
        [
            "GET",
            "HEAD"
        ];

        host.MapMethods("/api/ping", httpMethods, HandlePingRequest);

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
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP: GET, HEAD /api/ping => HandlePingRequest",
                          "predicate": "{GET [/api/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest",
                              "name": "HandlePingRequest",
                              "descriptor": "System.String HandlePingRequest()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/ping"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": []
                            }
                          }
                        },
                        {
                          "handler": "HTTP: GET, HEAD /api/ping => HandlePingRequest",
                          "predicate": "{HEAD [/api/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest",
                              "name": "HandlePingRequest",
                              "descriptor": "System.String HandlePingRequest()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/ping"
                              ],
                              "methods": [
                                "HEAD"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
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

    [Fact]
    public async Task Can_get_routes_for_any_verb_in_single_endpoint()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        await using WebApplication host = builder.Build();

        host.MapMethods("/api/ping", [], HandlePingRequest);

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
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP:  /api/ping => HandlePingRequest",
                          "predicate": "{[GET, HEAD, POST, PUT, DELETE, OPTIONS, PATCH] [/api/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest",
                              "name": "HandlePingRequest",
                              "descriptor": "System.String HandlePingRequest()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/ping"
                              ],
                              "methods": [
                                "GET",
                                "HEAD",
                                "POST",
                                "PUT",
                                "DELETE",
                                "OPTIONS",
                                "PATCH"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
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

    [Fact]
    public async Task Can_get_routes_for_separate_verbs_in_single_endpoint()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        await using WebApplication host = builder.Build();

        host.MapGet("/api/ping", HandlePingRequest);
        host.MapPost("/api/ping", HandlePingRequest);

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
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP: GET /api/ping => HandlePingRequest",
                          "predicate": "{GET [/api/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest",
                              "name": "HandlePingRequest",
                              "descriptor": "System.String HandlePingRequest()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/ping"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": []
                            }
                          }
                        },
                        {
                          "handler": "HTTP: POST /api/ping => HandlePingRequest",
                          "predicate": "{POST [/api/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest",
                              "name": "HandlePingRequest",
                              "descriptor": "System.String HandlePingRequest()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/ping"
                              ],
                              "methods": [
                                "POST"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
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

    [Fact]
    public async Task Can_get_routes_for_groups_using_same_handler_method()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        await using WebApplication host = builder.Build();

        host.MapGroup("/api/").MapGet("/ping", HandlePingRequest);

        host.MapGroup("/api/en-us").MapGet("ping", HandlePingRequest);

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
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP: GET /api/ping => HandlePingRequest",
                          "predicate": "{GET [/api/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest",
                              "name": "HandlePingRequest",
                              "descriptor": "System.String HandlePingRequest()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/ping"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": []
                            }
                          }
                        },
                        {
                          "handler": "HTTP: GET /api/en-us/ping => HandlePingRequest",
                          "predicate": "{GET [/api/en-us/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest",
                              "name": "HandlePingRequest",
                              "descriptor": "System.String HandlePingRequest()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/en-us/ping"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
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

    [Fact]
    public async Task Can_get_routes_for_groups_using_inline_lambdas()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        await using WebApplication host = builder.Build();

        host.MapGroup("/api/v1").MapDelete("products/{productId}", (Guid productId) =>
        {
            _ = productId;
        });

        host.MapGroup("/api/v2").MapPatch("products/{productId}", (Guid productId, [FromBody] Product product) =>
        {
            _ = productId;
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
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP: DELETE /api/v1/products/{productId}",
                          "predicate": "{DELETE [/api/v1/products/{productId}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest+<>c",
                              "name": "<Can_get_routes_for_groups_using_inline_lambdas>b__8_0",
                              "descriptor": "Void <Can_get_routes_for_groups_using_inline_lambdas>b__8_0(System.Guid)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/v1/products/{productId}"
                              ],
                              "methods": [
                                "DELETE"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "productId",
                                  "required": true,
                                  "negated": false
                                }
                              ]
                            }
                          }
                        },
                        {
                          "handler": "HTTP: PATCH /api/v2/products/{productId}",
                          "predicate": "{PATCH [/api/v2/products/{productId}], consumes [application/json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest+<>c",
                              "name": "<Can_get_routes_for_groups_using_inline_lambdas>b__8_1",
                              "descriptor": "Void <Can_get_routes_for_groups_using_inline_lambdas>b__8_1(System.Guid, Product)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/v2/products/{productId}"
                              ],
                              "methods": [
                                "PATCH"
                              ],
                              "consumes": [
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                }
                              ],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "productId",
                                  "required": true,
                                  "negated": false
                                }
                              ]
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

    [Fact]
    public async Task Can_get_routes_using_WebHostBuilder()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
        builder.ConfigureServices(services => services.AddRouteMappingsActuator());
        builder.Configure(applicationBuilder => applicationBuilder.UseEndpoints(routes => routes.MapGet("/api/ping", () => "pong")));
        using IWebHost host = builder.Build();

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
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP: GET /api/ping",
                          "predicate": "{GET [/api/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest+<>c",
                              "name": "<Can_get_routes_using_WebHostBuilder>b__9_4",
                              "descriptor": "System.String <Can_get_routes_using_WebHostBuilder>b__9_4()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/ping"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
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

    [Fact]
    public async Task Can_get_routes_using_HostBuilder()
    {
        HostBuilder builder = TestHostBuilderFactory.CreateWeb();
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));
        builder.ConfigureServices(services => services.AddRouteMappingsActuator());

        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.Configure(applicationBuilder => applicationBuilder.UseEndpoints(routes => routes.MapGet("/api/ping", () => "pong"))));

        using IHost host = builder.Build();

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
                      "dispatcherServlet": [
                        {
                          "handler": "HTTP: GET /api/ping",
                          "predicate": "{GET [/api/ping], produces [text/plain]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.MinimalApiTest+<>c",
                              "name": "<Can_get_routes_using_HostBuilder>b__10_5",
                              "descriptor": "System.String <Can_get_routes_using_HostBuilder>b__10_5()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/api/ping"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
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

    private static string HandlePingRequest()
    {
        return "pong";
    }

    // ReSharper disable once MemberCanBePrivate.Global
    internal sealed class Product
    {
        public string? Name { get; set; }
        public decimal Price { get; set; }
    }
}
