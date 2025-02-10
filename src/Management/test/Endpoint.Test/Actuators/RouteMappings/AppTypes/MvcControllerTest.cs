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
using Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes;

public sealed class MvcControllerTest
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
        builder.Services.AddControllersWithViews().HideControllersExcept(typeof(SimpleTestController));
        await using WebApplication host = builder.Build();

        host.MapControllerRoute("default", "{controller=SimpleTest}/{action=Index}/{id?}");
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
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController.Index (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [{controller=SimpleTest}/{action=Index}/{id?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController",
                              "name": "Index",
                              "descriptor": "Microsoft.AspNetCore.Mvc.IActionResult Index()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "{controller=SimpleTest}/{action=Index}/{id?}"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "controller",
                                  "value": "SimpleTest",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "action",
                                  "value": "Index",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "id",
                                  "required": false,
                                  "negated": false
                                }
                              ]
                            }
                          }
                        },
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController.PostAsync (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{POST [{controller=SimpleTest}/{action=Index}/{id?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController",
                              "name": "PostAsync",
                              "descriptor": "System.Threading.Tasks.Task PostAsync()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "{controller=SimpleTest}/{action=Index}/{id?}"
                              ],
                              "methods": [
                                "POST"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "controller",
                                  "value": "SimpleTest",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "action",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "id",
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
    public async Task Can_get_routes_for_controller_with_parameters_and_annotations()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        builder.Services.AddControllers().HideControllersExcept(typeof(AnnotatedTestController));
        await using WebApplication host = builder.Build();

        host.MapDefaultControllerRoute();
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
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.AnnotatedTestController.GetAllAsync (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [annotated-test/get-all], produces [application/xhtml+xml || text/html]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.AnnotatedTestController",
                              "name": "GetAllAsync",
                              "descriptor": "System.Threading.Tasks.Task`1[Microsoft.AspNetCore.Mvc.IActionResult] GetAllAsync(Microsoft.Extensions.Configuration.IConfiguration, System.Threading.CancellationToken)"
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
                                  "mediaType": "application/xhtml+xml",
                                  "negated": false
                                },
                                {
                                  "mediaType": "text/html",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": []
                            }
                          }
                        },
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.AnnotatedTestController.UpdateAsync (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{PATCH [annotated-test/update/{id:guid}], headers [X-Media-Version, X-Include-Details], consumes [application/json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.AnnotatedTestController",
                              "name": "UpdateAsync",
                              "descriptor": "System.Threading.Tasks.Task UpdateAsync(System.Guid, System.Nullable`1[System.Int32], System.String, System.String, Product, Boolean)"
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
                                  "name": "id",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "languageId",
                                  "required": false,
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
                        },
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.AnnotatedTestController.Delete (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{DELETE [annotated-test], produces [text/plain || text/javascript]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.AnnotatedTestController",
                              "name": "Delete",
                              "descriptor": "System.String Delete(System.String, System.String)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "annotated-test"
                              ],
                              "methods": [
                                "DELETE"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "text/plain",
                                  "negated": false
                                },
                                {
                                  "mediaType": "text/javascript",
                                  "negated": false
                                }
                              ],
                              "headers": [],
                              "params": [
                                {
                                  "name": "id",
                                  "negated": false
                                },
                                {
                                  "name": "alternateId",
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
    public async Task Can_get_routes_for_multiple_verbs_in_single_action_method()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        builder.Services.AddControllers().HideControllersExcept(typeof(MultipleVerbsTestController));
        await using WebApplication host = builder.Build();

        host.MapDefaultControllerRoute();
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
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.MultipleVerbsTestController.GetAll (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{[GET, HEAD] [{controller=Home}/{action=Index}/{id?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.MultipleVerbsTestController",
                              "name": "GetAll",
                              "descriptor": "Microsoft.AspNetCore.Mvc.IActionResult GetAll()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "{controller=Home}/{action=Index}/{id?}"
                              ],
                              "methods": [
                                "GET",
                                "HEAD"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "controller",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "action",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "id",
                                  "required": false,
                                  "negated": false
                                }
                              ]
                            }
                          }
                        },
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.MultipleVerbsTestController.GetById (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [{id:long}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.MultipleVerbsTestController",
                              "name": "GetById",
                              "descriptor": "Microsoft.AspNetCore.Mvc.IActionResult GetById(Int64)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "{id:long}"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [],
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
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.MultipleVerbsTestController.GetById (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{HEAD [{id:long}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.MultipleVerbsTestController",
                              "name": "GetById",
                              "descriptor": "Microsoft.AspNetCore.Mvc.IActionResult GetById(Int64)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "{id:long}"
                              ],
                              "methods": [
                                "HEAD"
                              ],
                              "consumes": [],
                              "produces": [],
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
            """);
    }

    [Fact]
    public async Task Can_get_routes_for_any_verb_in_single_action_method()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        builder.Services.AddControllers().HideControllersExcept(typeof(AnyVerbTestController));
        await using WebApplication host = builder.Build();

        host.MapDefaultControllerRoute();
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
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.AnyVerbTestController.RespondAtAnyVerb (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{[GET, HEAD, POST, PUT, DELETE, OPTIONS, PATCH] [{controller=Home}/{action=Index}/{id?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.AnyVerbTestController",
                              "name": "RespondAtAnyVerb",
                              "descriptor": "Microsoft.AspNetCore.Mvc.IActionResult RespondAtAnyVerb()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "{controller=Home}/{action=Index}/{id?}"
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
                              "params": [
                                {
                                  "name": "controller",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "action",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "id",
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
    public async Task Can_get_routes_using_WebHostBuilder()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

        builder.ConfigureServices(services =>
        {
            services.AddRouteMappingsActuator();
            services.AddControllers().HideControllersExcept(typeof(SimpleTestController));
        });

        builder.Configure(applicationBuilder => applicationBuilder.UseEndpoints(routes => routes.MapDefaultControllerRoute()));

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
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController.Index (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [{controller=Home}/{action=Index}/{id?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController",
                              "name": "Index",
                              "descriptor": "Microsoft.AspNetCore.Mvc.IActionResult Index()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "{controller=Home}/{action=Index}/{id?}"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "controller",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "action",
                                  "value": "Index",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "id",
                                  "required": false,
                                  "negated": false
                                }
                              ]
                            }
                          }
                        },
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController.PostAsync (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{POST [{controller=Home}/{action=Index}/{id?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController",
                              "name": "PostAsync",
                              "descriptor": "System.Threading.Tasks.Task PostAsync()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "{controller=Home}/{action=Index}/{id?}"
                              ],
                              "methods": [
                                "POST"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "controller",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "action",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "id",
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
            webHostBuilder.Configure(applicationBuilder => applicationBuilder.UseEndpoints(routes => routes.MapDefaultControllerRoute())));

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
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController.Index (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{GET [{controller=Home}/{action=Index}/{id?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController",
                              "name": "Index",
                              "descriptor": "Microsoft.AspNetCore.Mvc.IActionResult Index()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "{controller=Home}/{action=Index}/{id?}"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "controller",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "action",
                                  "value": "Index",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "id",
                                  "required": false,
                                  "negated": false
                                }
                              ]
                            }
                          }
                        },
                        {
                          "handler": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController.PostAsync (Steeltoe.Management.Endpoint.Test)",
                          "predicate": "{POST [{controller=Home}/{action=Index}/{id?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes.TestMvcControllers.SimpleTestController",
                              "name": "PostAsync",
                              "descriptor": "System.Threading.Tasks.Task PostAsync()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "{controller=Home}/{action=Index}/{id?}"
                              ],
                              "methods": [
                                "POST"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "controller",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "action",
                                  "required": true,
                                  "negated": false
                                },
                                {
                                  "name": "id",
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
}
