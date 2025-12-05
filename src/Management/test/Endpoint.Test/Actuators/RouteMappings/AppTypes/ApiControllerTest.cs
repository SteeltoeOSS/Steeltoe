// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes;

public sealed class ApiControllerTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*",
        ["Management:Endpoints:Mappings:IncludeActuators"] = "false",
        ["Management:Endpoints:SerializerOptions:WriteIndented"] = "true"
    };

    [Fact]
    public async Task Can_get_routes_for_simple_controller()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        builder.Services.AddControllers().HideControllersExcept(typeof(SimpleTestController));
        await using WebApplication host = builder.Build();

        host.MapControllers();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().Be("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.SimpleTestController.GetAll (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [SimpleTest], produces [text/plain || application/json || text/json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.SimpleTestController",
                              "name": "GetAll",
                              "descriptor": "System.Collections.Generic.IEnumerable`1[System.String] GetAll()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "SimpleTest"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                },
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "text/json",
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
            """, IgnoreLineEndingsComparer.Instance);
    }

    [Fact]
    public async Task Can_get_routes_for_controller_with_parameters_and_annotations()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        builder.Services.AddControllers().HideControllersExcept(typeof(AnnotatedTestController));
        await using WebApplication host = builder.Build();

        host.MapControllers();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().Be("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.AnnotatedTestController.GetAll (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [annotated-test/get-all], produces [application/json || application/xml]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.AnnotatedTestController",
                              "name": "GetAll",
                              "descriptor": "System.Collections.Generic.IEnumerable`1[System.String] GetAll()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "annotated-test/get-all"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "application/xml",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": []
                            }
                          }
                        },
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.AnnotatedTestController.UpdateAsync (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{PATCH [annotated-test/update/{id:guid}], headers [X-Media-Version, X-Include-Details], consumes [application/json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.AnnotatedTestController",
                              "name": "UpdateAsync",
                              "descriptor": "System.Threading.Tasks.Task UpdateAsync(System.Guid, System.String, System.String, Product, Boolean)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "annotated-test/update/{id:guid}"
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
                              "headers": [
                                {
                                  "name": "X-Media-Version",
                                  "required": false,
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
                                  "name": "id",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "failOnError",
                                  "value": true,
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
            """, IgnoreLineEndingsComparer.Instance);
    }

    [Fact]
    public async Task Can_get_routes_for_multiple_verbs_in_single_action_method()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        builder.Services.AddControllers().HideControllersExcept(typeof(MultipleVerbsTestController));
        await using WebApplication host = builder.Build();

        host.MapControllers();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().Be("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.MultipleVerbsTestController.GetAll (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [MultipleVerbsTest], produces [text/plain || application/json || text/json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.MultipleVerbsTestController",
                              "name": "GetAll",
                              "descriptor": "System.Collections.Generic.IEnumerable`1[System.String] GetAll()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "MultipleVerbsTest"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                },
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "text/json",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": []
                            }
                          }
                        },
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.MultipleVerbsTestController.GetAll (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{HEAD [MultipleVerbsTest], produces [text/plain || application/json || text/json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.MultipleVerbsTestController",
                              "name": "GetAll",
                              "descriptor": "System.Collections.Generic.IEnumerable`1[System.String] GetAll()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "MultipleVerbsTest"
                              ],
                              "methods": [
                                "HEAD"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                },
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "text/json",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": []
                            }
                          }
                        },
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.MultipleVerbsTestController.GetById (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [MultipleVerbsTest/{id:long}], produces [text/plain || application/json || text/json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.MultipleVerbsTestController",
                              "name": "GetById",
                              "descriptor": "System.String GetById(Int64)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "MultipleVerbsTest/{id:long}"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                },
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "text/json",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": [
                                {
                                  "name": "id",
                                  "required": true,
                                  "negated": false
                                }
                              ]
                            }
                          }
                        },
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.MultipleVerbsTestController.GetById (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{HEAD [MultipleVerbsTest/{id:long}], produces [text/plain || application/json || text/json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.MultipleVerbsTestController",
                              "name": "GetById",
                              "descriptor": "System.String GetById(Int64)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "MultipleVerbsTest/{id:long}"
                              ],
                              "methods": [
                                "HEAD"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                },
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "text/json",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": [
                                {
                                  "name": "id",
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
            """, IgnoreLineEndingsComparer.Instance);
    }

    [Fact]
    public async Task Can_get_routes_for_any_verb_in_single_action_method()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        builder.Services.AddControllers().HideControllersExcept(typeof(AnyVerbTestController));
        await using WebApplication host = builder.Build();

        host.MapControllers();
        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().Be("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.AnyVerbTestController.RespondAtAnyVerb (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{[GET, HEAD, POST, PUT, DELETE, OPTIONS, PATCH] [AnyVerbTest]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.AnyVerbTestController",
                              "name": "RespondAtAnyVerb",
                              "descriptor": "System.Collections.Generic.IEnumerable`1[System.String] RespondAtAnyVerb()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "AnyVerbTest"
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
            """, IgnoreLineEndingsComparer.Instance);
    }

    [Fact]
    public async Task Can_get_routes_using_WebHostBuilder()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

        builder.ConfigureServices(services =>
        {
            services.AddRouteMappingsActuator();
            services.AddControllers().HideControllersExcept(typeof(SimpleTestController));
        });

        builder.Configure(applicationBuilder => applicationBuilder.UseEndpoints(routes => routes.MapControllers()));
        using IWebHost host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().Be("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.SimpleTestController.GetAll (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [SimpleTest], produces [text/plain || application/json || text/json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.SimpleTestController",
                              "name": "GetAll",
                              "descriptor": "System.Collections.Generic.IEnumerable`1[System.String] GetAll()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "SimpleTest"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                },
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "text/json",
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
            """, IgnoreLineEndingsComparer.Instance);
    }

    [Fact]
    public async Task Can_get_routes_using_HostBuilder()
    {
        HostBuilder builder = TestHostBuilderFactory.CreateWeb();
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

        builder.ConfigureServices(services =>
        {
            services.AddRouteMappingsActuator();
            services.AddControllers().HideControllersExcept(typeof(SimpleTestController));
        });

        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.Configure(applicationBuilder => applicationBuilder.UseEndpoints(routes => routes.MapControllers())));

        using IHost host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/mappings"), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseText.Should().Be("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.SimpleTestController.GetAll (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [SimpleTest], produces [text/plain || application/json || text/json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestApiControllers.SimpleTestController",
                              "name": "GetAll",
                              "descriptor": "System.Collections.Generic.IEnumerable`1[System.String] GetAll()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "SimpleTest"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                },
                                {
                                  "mediaType": "application/json",
                                  "negated": false
                                },
                                {
                                  "mediaType": "text/json",
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
            """, IgnoreLineEndingsComparer.Instance);
    }
}
