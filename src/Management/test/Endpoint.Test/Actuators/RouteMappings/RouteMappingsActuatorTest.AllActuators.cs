// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

public sealed partial class RouteMappingsActuatorTest
{
    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Returns_endpoints_for_CloudFoundry_actuator(HostBuilderType hostBuilderType)
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddCloudFoundryActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/cloudfoundryapplication",
                          "predicate": "{GET [/cloudfoundryapplication], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/cloudfoundryapplication"
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
    public async Task Returns_endpoints_for_DbMigrations_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddDbMigrationsActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/dbmigrations",
                          "predicate": "{GET [/actuator/dbmigrations], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/dbmigrations"
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
    public async Task Returns_endpoints_for_Environment_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddEnvironmentActuator());

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
    public async Task Returns_endpoints_for_Health_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddHealthActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/health/{**_}",
                          "predicate": "{GET [/actuator/health/{**_}], headers [X-Use-Status-Code-From-Response], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/health/{**_}"
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
                              "headers": [
                                {
                                  "name": "X-Use-Status-Code-From-Response",
                                  "required": false,
                                  "negated": false
                                }
                              ],
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
    public async Task Returns_endpoints_for_HeapDump_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddHeapDumpActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/heapdump",
                          "predicate": "{GET [/actuator/heapdump], produces [application/octet-stream]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/heapdump"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [
                                {
                                  "mediaType": "application/octet-stream",
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
    public async Task Returns_endpoints_for_HttpExchanges_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddHttpExchangesActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/httpexchanges",
                          "predicate": "{GET [/actuator/httpexchanges], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/httpexchanges"
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
    public async Task Returns_endpoints_for_Hypermedia_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddHypermediaActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator",
                          "predicate": "{GET [/actuator], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator"
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
    public async Task Returns_endpoints_for_Info_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddInfoActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/info",
                          "predicate": "{GET [/actuator/info], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/info"
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
    public async Task Returns_endpoints_for_Loggers_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddLoggersActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/loggers/{**_}",
                          "predicate": "{GET [/actuator/loggers/{**_}], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/loggers/{**_}"
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
                          "handler": "/actuator/loggers/{**_}",
                          "predicate": "{POST [/actuator/loggers/{**_}], produces [application/vnd.spring-boot.actuator.v3+json], consumes [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/loggers/{**_}"
                              ],
                              "methods": [
                                "POST"
                              ],
                              "consumes": [
                                {
                                  "mediaType": "application/vnd.spring-boot.actuator.v3+json",
                                  "negated": false
                                }
                              ],
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
    public async Task Returns_endpoints_for_Refresh_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddRefreshActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/refresh",
                          "predicate": "{POST [/actuator/refresh], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/refresh"
                              ],
                              "methods": [
                                "POST"
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
    public async Task Returns_endpoints_for_RouteMappings_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, null, false);

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/mappings",
                          "predicate": "{GET [/actuator/mappings], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/mappings"
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
    public async Task Returns_endpoints_for_Services_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddServicesActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/beans",
                          "predicate": "{GET [/actuator/beans], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/beans"
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
    public async Task Returns_endpoints_for_ThreadDump_actuator(HostBuilderType hostBuilderType)
    {
        JsonNode responseNode = await GetRouteMappingsAsync(hostBuilderType, AppSettings, services => services.AddThreadDumpActuator());

        responseNode.ToJsonString().Should().BeJson("""
            {
              "contexts": {
                "application": {
                  "mappings": {
                    "dispatcherServlets": {
                      "dispatcherServlet": [
                        {
                          "handler": "/actuator/threaddump",
                          "predicate": "{GET [/actuator/threaddump], produces [application/vnd.spring-boot.actuator.v3+json]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.Middleware.EndpointMiddleware`2",
                              "name": "InvokeAsync",
                              "descriptor": "System.Threading.Tasks.Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext, Microsoft.AspNetCore.Http.RequestDelegate)"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "/actuator/threaddump"
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
}
