// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Encryption;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Environment;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:TestApp"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "*"
    };

    [Fact]
    public async Task HandleEnvironmentRequestAsync_ReturnsExpected()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(AppSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IOptionsMonitor<EnvironmentEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<EnvironmentEndpointOptions, ConfigureEnvironmentEndpointOptions>();

        IOptionsMonitor<ManagementOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>(AppSettings);

        IHostEnvironment hostEnvironment = TestHostEnvironmentFactory.Create();

        var handler = new EnvironmentEndpointHandler(optionsMonitor, configurationRoot, hostEnvironment, NullLoggerFactory.Instance);
        var middleware = new EnvironmentEndpointMiddleware(handler, managementOptions, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/env");
        await middleware.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        string? json = await reader.ReadLineAsync();

        json.Should().BeJson("""
            {
              "activeProfiles": [
                "Test"
              ],
              "propertySources": [
                {
                  "name": "MemoryConfigurationProvider",
                  "properties": {
                    "Logging:Console:IncludeScopes": {
                      "value": "false"
                    },
                    "Logging:LogLevel:Default": {
                      "value": "Warning"
                    },
                    "Logging:LogLevel:Steeltoe": {
                      "value": "Information"
                    },
                    "Logging:LogLevel:TestApp": {
                      "value": "Information"
                    },
                    "management:endpoints:actuator:exposure:include:0": {
                      "value": "*"
                    },
                    "management:endpoints:enabled": {
                      "value": "true"
                    }
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task EnvironmentActuator_ReturnsExpectedData()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/env"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson("""
            {
              "activeProfiles": [
                "Production"
              ],
              "propertySources": [
                {
                  "name": "ChainedConfigurationProvider",
                  "properties": {
                    "applicationName": {
                      "value": "Steeltoe.Management.Endpoint.Test"
                    }
                  }
                },
                {
                  "name": "EnvironmentVariablesConfigurationProvider",
                  "properties": {
                    "applicationName": {
                      "value": "Steeltoe.Management.Endpoint.Test"
                    },
                    "environment": {},
                    "urls": {}
                  }
                },
                {
                  "name": "MemoryConfigurationProvider",
                  "properties": {
                    "Logging:Console:IncludeScopes": {
                      "value": "false"
                    },
                    "Logging:LogLevel:Default": {
                      "value": "Warning"
                    },
                    "Logging:LogLevel:Steeltoe": {
                      "value": "Information"
                    },
                    "Logging:LogLevel:TestApp": {
                      "value": "Information"
                    },
                    "management:endpoints:actuator:exposure:include:0": {
                      "value": "*"
                    },
                    "management:endpoints:enabled": {
                      "value": "true"
                    }
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task EnvironmentActuator_withPlaceholderDecryption_ReturnsExpectedData()
    {
        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(AppSettings);
            configuration.AddPlaceholderResolver();
            configuration.AddDecryption();
        });

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/env"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson("""
            {
              "activeProfiles": [
                "Production"
              ],
              "propertySources": [
                {
                  "name": "DecryptionConfigurationProvider",
                  "properties": {}
                },
                {
                  "name": "PlaceholderConfigurationProvider",
                  "properties": {}
                },
                {
                  "name": "ChainedConfigurationProvider",
                  "properties": {
                    "applicationName": {
                      "value": "Steeltoe.Management.Endpoint.Test"
                    }
                  }
                },
                {
                  "name": "EnvironmentVariablesConfigurationProvider",
                  "properties": {
                    "applicationName": {
                      "value": "Steeltoe.Management.Endpoint.Test"
                    },
                    "environment": {},
                    "urls": {}
                  }
                },
                {
                  "name": "MemoryConfigurationProvider",
                  "properties": {
                    "Logging:Console:IncludeScopes": {
                      "value": "false"
                    },
                    "Logging:LogLevel:Default": {
                      "value": "Warning"
                    },
                    "Logging:LogLevel:Steeltoe": {
                      "value": "Information"
                    },
                    "Logging:LogLevel:TestApp": {
                      "value": "Information"
                    },
                    "management:endpoints:actuator:exposure:include:0": {
                      "value": "*"
                    },
                    "management:endpoints:enabled": {
                      "value": "true"
                    }
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<EnvironmentEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/env", endpointOptions.GetPathMatchPattern(managementOptions.Path));
        Assert.Equal("/cloudfoundryapplication/env", endpointOptions.GetPathMatchPattern(ConfigureManagementOptions.DefaultCloudFoundryPath));
        Assert.Single(endpointOptions.AllowedVerbs);
        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }

    [Fact]
    public async Task EnvironmentActuator_UpdatesSanitizerRulesOnConfigurationChange()
    {
        var fileProvider = new MemoryFileProvider();
        const string appSettingsJsonFileName = "appsettings.json";

        fileProvider.IncludeFile(appSettingsJsonFileName, """
        {
          "Management": {
            "Endpoints": {
              "Enabled": true,
              "Actuator": {
                "Exposure": {
                  "Include": [
                    "env"
                  ]
                }
              },
              "Env": {
                "KeysToSanitize": [
                  "Password"
                ]
              }
            }
          },
          "TestSettings": {
            "Password": "secret-password",
            "AccessToken": "secret-token"
          }
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile(fileProvider, appSettingsJsonFileName, false, true);
        builder.Services.AddEnvironmentActuator();

        await using WebApplication app = builder.Build();
        await app.StartAsync();

        using HttpClient httpClient = app.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/env"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson("""
            {
              "activeProfiles": [
                "Production"
              ],
              "propertySources": [
                {
                  "name": "JsonConfigurationProvider: [appsettings.json]",
                  "properties": {
                    "Management:Endpoints:Actuator:Exposure:Include:0": {
                      "value": "env"
                    },
                    "Management:Endpoints:Enabled": {
                      "value": "True"
                    },
                    "Management:Endpoints:Env:KeysToSanitize:0": {
                      "value": "Password"
                    },
                    "TestSettings:AccessToken": {
                      "value": "secret-token"
                    },
                    "TestSettings:Password": {
                      "value": "******"
                    }
                  }
                }
              ]
            }
            """);

        fileProvider.ReplaceFile(appSettingsJsonFileName, """
        {
          "Management": {
            "Endpoints": {
              "Enabled": true,
              "Actuator": {
                "Exposure": {
                  "Include": [
                    "env"
                  ]
                }
              },
              "Env": {
                "KeysToSanitize": [
                  "AccessToken"
                ]
              }
            }
          },
          "TestSettings": {
            "Password": "secret-password",
            "AccessToken": "secret-token"
          }
        }
        """);

        fileProvider.NotifyChanged();

        response = await httpClient.GetAsync(new Uri("http://localhost/actuator/env"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson("""
            {
              "activeProfiles": [
                "Production"
              ],
              "propertySources": [
                {
                  "name": "JsonConfigurationProvider: [appsettings.json]",
                  "properties": {
                    "Management:Endpoints:Actuator:Exposure:Include:0": {
                      "value": "env"
                    },
                    "Management:Endpoints:Enabled": {
                      "value": "True"
                    },
                    "Management:Endpoints:Env:KeysToSanitize:0": {
                      "value": "AccessToken"
                    },
                    "TestSettings:AccessToken": {
                      "value": "******"
                    },
                    "TestSettings:Password": {
                      "value": "secret-password"
                    }
                  }
                }
              ]
            }
            """);
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
