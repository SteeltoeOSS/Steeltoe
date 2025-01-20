// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.RouteMappingsNew;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappingsNew.AppTypes;

public sealed class ConventionalRoutesTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*",
        ["Management:Endpoints:Mappings:IncludeActuators"] = "false",
        ["Management:Endpoints:SerializerOptions:WriteIndented"] = "true"
    };

    [Fact]
    public async Task Can_get_routes_for_various_patterns()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddRouteMappingsActuator();
        builder.Services.AddMvc(options => options.EnableEndpointRouting = false).HideControllersExcept();
        await using WebApplication host = builder.Build();

        host.UseMvc(routes =>
        {
            routes.AddRoutesToMappingsActuator();
            routes.MapGet("error", _ => Task.CompletedTask);

            routes.MapRoute("download-document-by-id", "Document/{id}", new
            {
                controller = "Document",
                action = "Download"
            }, new RouteValueDictionary
            {
                ["httpMethod"] = new HttpMethodRouteConstraint("GET", "HEAD")
            });

            routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
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
                          "handler": "",
                          "predicate": "{GET [error]}",
                          "details": {
                            "requestMappingConditions": {
                              "patterns": [
                                "error"
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
                        },
                        {
                          "handler": "download-document-by-id",
                          "predicate": "{[GET, HEAD] [Document/{id}]}",
                          "details": {
                            "requestMappingConditions": {
                              "patterns": [
                                "Document/{id}"
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
                                  "name": "id",
                                  "required": true,
                                  "negated": false
                                }
                              ]
                            }
                          }
                        },
                        {
                          "handler": "default",
                          "predicate": "{[GET, HEAD, POST, PUT, DELETE, OPTIONS, PATCH] [{controller=Home}/{action=Index}/{id?}]}",
                          "details": {
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
                                  "value": "Home",
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
            services.AddMvc(options => options.EnableEndpointRouting = false).HideControllersExcept();
        });

        builder.Configure(applicationBuilder => applicationBuilder.UseMvc(routes =>
        {
            routes.AddRoutesToMappingsActuator();
            routes.MapGet("error", _ => Task.CompletedTask);
            routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
        }));

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
                          "handler": "",
                          "predicate": "{GET [error]}",
                          "details": {
                            "requestMappingConditions": {
                              "patterns": [
                                "error"
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
                        },
                        {
                          "handler": "default",
                          "predicate": "{[GET, HEAD, POST, PUT, DELETE, OPTIONS, PATCH] [{controller=Home}/{action=Index}/{id?}]}",
                          "details": {
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
                                  "value": "Home",
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
            services.AddMvc(options => options.EnableEndpointRouting = false).HideControllersExcept();
        });

        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.Configure(applicationBuilder => applicationBuilder.UseMvc(routes =>
        {
            routes.AddRoutesToMappingsActuator();
            routes.MapGet("error", _ => Task.CompletedTask);
            routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
        })));

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
                          "handler": "",
                          "predicate": "{GET [error]}",
                          "details": {
                            "requestMappingConditions": {
                              "patterns": [
                                "error"
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
                        },
                        {
                          "handler": "default",
                          "predicate": "{[GET, HEAD, POST, PUT, DELETE, OPTIONS, PATCH] [{controller=Home}/{action=Index}/{id?}]}",
                          "details": {
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
                                  "value": "Home",
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
