// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Steeltoe.Management.Endpoint.RazorPagesWebApp.Test.Pages;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings.AppTypes;

public sealed class RazorPagesExternalAppTest(WebApplicationFactory<IndexModel> factory) : IClassFixture<WebApplicationFactory<IndexModel>>
{
    private readonly WebApplicationFactory<IndexModel> _factory = factory;

    [Fact]
    public async Task Can_get_routes_for_razor_pages()
    {
        using HttpClient client = _factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/mappings"));

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
                          "handler": "/Error",
                          "predicate": "{GET [Error]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.RazorPagesWebApp.Test.Pages.ErrorModel",
                              "name": "OnGet",
                              "descriptor": "Void OnGet()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "Error"
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
                          "handler": "/Index",
                          "predicate": "{GET [Index]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.RazorPagesWebApp.Test.Pages.IndexModel",
                              "name": "OnGet",
                              "descriptor": "Void OnGet()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "Index"
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
                          "handler": "/Index",
                          "predicate": "{GET []}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.RazorPagesWebApp.Test.Pages.IndexModel",
                              "name": "OnGet",
                              "descriptor": "Void OnGet()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                ""
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
                          "handler": "/Privacy",
                          "predicate": "{GET [Privacy]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.RazorPagesWebApp.Test.Pages.PrivacyModel",
                              "name": "OnGet",
                              "descriptor": "Void OnGet()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "Privacy"
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
                          "handler": "/TestCasesModel",
                          "predicate": "{GET [custom-route/{languageId:int?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.RazorPagesWebApp.Test.Pages.TestCasesModel",
                              "name": "OnGet",
                              "descriptor": "Void OnGet(System.Nullable`1[System.Int32], System.String, Int32, System.Nullable`1[System.Int32])"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "custom-route/{languageId:int?}"
                              ],
                              "methods": [
                                "GET"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "languageId",
                                  "value": 99,
                                  "required": false,
                                  "negated": false
                                },
                                {
                                  "name": "filter",
                                  "required": false,
                                  "negated": false
                                },
                                {
                                  "name": "pageNumber",
                                  "value": 1,
                                  "required": false,
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
                        },
                        {
                          "handler": "/TestCasesModel",
                          "predicate": "{POST [custom-route/{languageId:int?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.RazorPagesWebApp.Test.Pages.TestCasesModel",
                              "name": "OnPostAsync",
                              "descriptor": "System.Threading.Tasks.Task OnPostAsync()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "custom-route/{languageId:int?}"
                              ],
                              "methods": [
                                "POST"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "languageId",
                                  "required": false,
                                  "negated": false
                                }
                              ]
                            }
                          }
                        },
                        {
                          "handler": "/TestCasesModel",
                          "predicate": "{PATCH [custom-route/{languageId:int?}], headers [X-Media-Version]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.RazorPagesWebApp.Test.Pages.TestCasesModel",
                              "name": "OnPatchAsync",
                              "descriptor": "System.Threading.Tasks.Task`1[Microsoft.AspNetCore.Mvc.IActionResult] OnPatchAsync(System.Guid, System.String, System.Nullable`1[System.Text.Json.JsonElement])"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "custom-route/{languageId:int?}"
                              ],
                              "methods": [
                                "PATCH"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [
                                {
                                  "name": "X-Media-Version",
                                  "required": true,
                                  "negated": false
                                }
                              ],
                              "params": [
                                {
                                  "name": "languageId",
                                  "required": false,
                                  "negated": false
                                },
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
                          "handler": "/TestCasesModel",
                          "predicate": "{DELETE [custom-route/{languageId:int?}]}",
                          "details": {
                            "handlerMethod": {
                              "className": "Steeltoe.Management.Endpoint.RazorPagesWebApp.Test.Pages.TestCasesModel",
                              "name": "OnDeleteAllAsync",
                              "descriptor": "System.Threading.Tasks.Task OnDeleteAllAsync()"
                            },
                            "requestMappingConditions": {
                              "patterns": [
                                "custom-route/{languageId:int?}"
                              ],
                              "methods": [
                                "DELETE"
                              ],
                              "consumes": [],
                              "produces": [],
                              "headers": [],
                              "params": [
                                {
                                  "name": "languageId",
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
