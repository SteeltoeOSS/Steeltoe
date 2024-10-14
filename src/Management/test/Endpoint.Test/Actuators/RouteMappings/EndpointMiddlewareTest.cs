// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

public sealed class EndpointMiddlewareTest : BaseTest
{
    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<RouteMappingsEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/mappings", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));

        Assert.Equal("/cloudfoundryapplication/mappings",
            endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));

        Assert.Single(endpointOptions.AllowedVerbs);
        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }

    [Fact]
    public async Task HandleMappingsRequestAsync_MvcNotUsed_NoRoutes_ReturnsExpected()
    {
        IOptionsMonitor<RouteMappingsEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<RouteMappingsEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var routerMappings = new RouterMappings();
        var mockActionDescriptorCollectionProvider = new Mock<IActionDescriptorCollectionProvider>();

        mockActionDescriptorCollectionProvider.Setup(provider => provider.ActionDescriptors)
            .Returns(new ActionDescriptorCollection(Array.Empty<ActionDescriptor>(), 0));

        List<IApiDescriptionProvider> mockApiDescriptionProviders = [new Mock<IApiDescriptionProvider>().Object];

        var handler = new RouteMappingsEndpointHandler(endpointOptionsMonitor, mockActionDescriptorCollectionProvider.Object, mockApiDescriptionProviders,
            routerMappings, NullLoggerFactory.Instance);

        var middleware = new RouteMappingsEndpointMiddleware(handler, managementOptionsMonitor, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/mappings");
        await middleware.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string? json = await reader.ReadLineAsync();

        json.Should().BeJson("""
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
    public async Task MappingsActuator_EndpointRouting_ReturnsExpectedData()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:actuator:exposure:include:0", "*" }
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/mappings"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "{{typeof(HomeController).FullName}}": [
                        {
                          "handler": "{{typeof(Person).FullName}} Index()",
                          "predicate": "{[/Home/Index],methods=[GET],produces=[text/plain || application/json || text/json],consumes=[text/plain || application/json || text/json]}",
                          "details": {
                            "requestMappingConditions": {
                              "patterns": [
                                "/Home/Index"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [
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
            """);

        response = await client.GetAsync(new Uri("http://localhost/actuator/refresh"));
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

        response = await client.PostAsync(new Uri("http://localhost/actuator/refresh"), null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MappingsActuator_EndpointRouting_CanTurnOffAllVerbs()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:actuator:exposure:include:0", "*" },
            { "management:endpoints:refresh:allowedVerbs:0", string.Empty }
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/mappings"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "{{typeof(HomeController).FullName}}": [
                        {
                          "handler": "{{typeof(Person).FullName}} Index()",
                          "predicate": "{[/Home/Index],methods=[GET],produces=[text/plain || application/json || text/json],consumes=[text/plain || application/json || text/json]}",
                          "details": {
                            "requestMappingConditions": {
                              "patterns": [
                                "/Home/Index"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [
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
            """);

        response = await client.GetAsync(new Uri("http://localhost/actuator/refresh"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await client.PostAsync(new Uri("http://localhost/actuator/refresh"), null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MappingsActuator_ConventionalRouting_ReturnsExpectedData()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:actuator:exposure:include:0", "*" },
            { "TestUsesEndpointRouting", "False" }
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/mappings"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "{{typeof(HomeController).FullName}}": [
                        {
                          "handler": "{{typeof(Person).FullName}} Index()",
                          "predicate": "{[/Home/Index],methods=[GET],produces=[text/plain || application/json || text/json],consumes=[text/plain || application/json || text/json]}",
                          "details": {
                            "requestMappingConditions": {
                              "patterns": [
                                "/Home/Index"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [
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
                      ],
                      "CoreRouteHandler": [
                        {
                          "handler": "CoreRouteHandler",
                          "predicate": "{[{controller=Home}/{action=Index}/{id?}],methods=[GET || PUT || POST || DELETE || HEAD || OPTIONS]}"
                        },
                        {
                          "handler": "CoreRouteHandler",
                          "predicate": "{[/actuator/mappings],methods=[Get]}"
                        },
                        {
                          "handler": "CoreRouteHandler",
                          "predicate": "{[/actuator/refresh],methods=[Post]}"
                        }            
                      ]
                    }
                  }
                }
              }
            }
            """);

        response = await client.GetAsync(new Uri("http://localhost/actuator/refresh"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await client.PostAsync(new Uri("http://localhost/actuator/refresh"), null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MappingsActuator_ConventionalRouting_CanTurnOffAllVerbs()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:actuator:exposure:include:0", "*" },
            { "management:endpoints:refresh:allowedVerbs:0", string.Empty },
            { "TestUsesEndpointRouting", "False" }
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/mappings"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "{{typeof(HomeController).FullName}}": [
                        {
                          "handler": "{{typeof(Person).FullName}} Index()",
                          "predicate": "{[/Home/Index],methods=[GET],produces=[text/plain || application/json || text/json],consumes=[text/plain || application/json || text/json]}",
                          "details": {
                            "requestMappingConditions": {
                              "patterns": [
                                "/Home/Index"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [
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
                      ],
                      "CoreRouteHandler": [
                        {
                          "handler": "CoreRouteHandler",
                          "predicate": "{[{controller=Home}/{action=Index}/{id?}],methods=[GET || PUT || POST || DELETE || HEAD || OPTIONS]}"
                        },
                        {
                          "handler": "CoreRouteHandler",
                          "predicate": "{[/actuator/mappings],methods=[Get]}"
                        }            
                      ]
                    }
                  }
                }
              }
            }
            """);

        response = await client.GetAsync(new Uri("http://localhost/actuator/refresh"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        response = await client.PostAsync(new Uri("http://localhost/actuator/refresh"), null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MappingsActuator_ConventionalRouting_ThrowsForCallback()
    {
        Action<IEndpointConventionBuilder> configureEndpointsCallback = _ =>
        {
        };

        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();

        builder.ConfigureServices(services =>
        {
            services.AddMvc(options => options.EnableEndpointRouting = false);
            services.AddMappingsActuator();
        });

        builder.Configure(app =>
        {
            app.UseActuators(configureEndpointsCallback);
        });

        using IWebHost host = builder.Build();

        Func<Task> action = async () => await host.StartAsync();

        await action.Should().ThrowExactlyAsync<NotSupportedException>().WithMessage("Customizing endpoints is only supported when using endpoint routing.");
    }

    private HttpContext CreateRequest(string method, string path)
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Response.Body = new MemoryStream();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        return context;
    }
}
