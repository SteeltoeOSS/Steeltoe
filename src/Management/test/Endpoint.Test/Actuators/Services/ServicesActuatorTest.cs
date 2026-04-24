// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Services;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Services;

public sealed class ServicesActuatorTest
{
    private static readonly string SteeltoeTestAssemblyVersion = typeof(ServicesActuatorTest).Assembly.GetName().Version!.ToString();
    private static readonly string MicrosoftOptionsAssemblyName = typeof(Options).Assembly.ToString();
    private static readonly string MicrosoftLoggingAssemblyName = typeof(LoggerFilterOptions).Assembly.ToString();
    private static readonly string MicrosoftLoggingAbstractionsAssemblyName = typeof(ILogger).Assembly.ToString();

    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "beans"
    };

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddServicesActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => serviceProvider.GetRequiredService<ServicesEndpointMiddleware>();

        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddServicesActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ServicesEndpointOptions options = serviceProvider.GetRequiredService<IOptions<ServicesEndpointOptions>>().Value;

        options.Enabled.Should().BeNull();
        options.Id.Should().Be("beans");
        options.Path.Should().Be("beans");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/beans");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Services:Enabled"] = "true",
            ["Management:Endpoints:Services:Id"] = "test-actuator-id",
            ["Management:Endpoints:Services:Path"] = "test-actuator-path",
            ["Management:Endpoints:Services:RequiredPermissions"] = "full",
            ["Management:Endpoints:Services:AllowedVerbs:0"] = "post"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddServicesActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ServicesEndpointOptions options = serviceProvider.GetRequiredService<IOptions<ServicesEndpointOptions>>().Value;

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
    public async Task Endpoint_returns_expected_data(HostBuilderType hostBuilderType)
    {
        var testServices = new ServiceCollection();
        testServices.AddLogging();
        testServices.AddOptions();

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IServiceCollection>(testServices);
                services.AddServicesActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/beans"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "Microsoft.Extensions.Options.IOptions`1": {
                      "scope": "Singleton",
                      "type": "Microsoft.Extensions.Options.UnnamedOptionsManager`1[TOptions]",
                      "resource": "{{MicrosoftOptionsAssemblyName}}",
                      "dependencies": [
                        "Microsoft.Extensions.Options.IOptionsFactory`1[TOptions]"
                      ]
                    },
                    "Microsoft.Extensions.Options.IOptionsSnapshot`1": {
                      "scope": "Scoped",
                      "type": "Microsoft.Extensions.Options.OptionsManager`1[TOptions]",
                      "resource": "{{MicrosoftOptionsAssemblyName}}",
                      "dependencies": [
                        "Microsoft.Extensions.Options.IOptionsFactory`1[TOptions]"
                      ]
                    },
                    "Microsoft.Extensions.Options.IOptionsMonitor`1": {
                      "scope": "Singleton",
                      "type": "Microsoft.Extensions.Options.OptionsMonitor`1[TOptions]",
                      "resource": "{{MicrosoftOptionsAssemblyName}}",
                      "dependencies": [
                        "Microsoft.Extensions.Options.IOptionsFactory`1[TOptions]",
                        "System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Options.IOptionsChangeTokenSource`1[TOptions]]",
                        "Microsoft.Extensions.Options.IOptionsMonitorCache`1[TOptions]"
                      ]
                    },
                    "Microsoft.Extensions.Options.IOptionsFactory`1": {
                      "scope": "Transient",
                      "type": "Microsoft.Extensions.Options.OptionsFactory`1[TOptions]",
                      "resource": "{{MicrosoftOptionsAssemblyName}}",
                      "dependencies": [
                        "System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Options.IConfigureOptions`1[TOptions]]",
                        "System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Options.IPostConfigureOptions`1[TOptions]]",
                        "System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Options.IValidateOptions`1[TOptions]]"
                      ]
                    },
                    "Microsoft.Extensions.Options.IOptionsMonitorCache`1": {
                      "scope": "Singleton",
                      "type": "Microsoft.Extensions.Options.OptionsCache`1[TOptions]",
                      "resource": "{{MicrosoftOptionsAssemblyName}}",
                      "dependencies": []
                    },
                    "Microsoft.Extensions.Logging.ILoggerFactory": {
                      "scope": "Singleton",
                      "type": "Microsoft.Extensions.Logging.LoggerFactory",
                      "resource": "{{MicrosoftLoggingAbstractionsAssemblyName}}",
                      "dependencies": [
                        "System.Collections.Generic.IEnumerable`1[Microsoft.Extensions.Logging.ILoggerProvider]",
                        "Microsoft.Extensions.Logging.LoggerFilterOptions",
                        "Microsoft.Extensions.Options.IOptionsMonitor`1[Microsoft.Extensions.Logging.LoggerFilterOptions]",
                        "Microsoft.Extensions.Options.IOptions`1[Microsoft.Extensions.Logging.LoggerFactoryOptions]",
                        "Microsoft.Extensions.Logging.IExternalScopeProvider"
                      ]
                    },
                    "Microsoft.Extensions.Logging.ILogger`1": {
                      "scope": "Singleton",
                      "type": "Microsoft.Extensions.Logging.Logger`1[T]",
                      "resource": "{{MicrosoftLoggingAbstractionsAssemblyName}}",
                      "dependencies": [
                        "Microsoft.Extensions.Logging.ILoggerFactory"
                      ]
                    },
                    "Microsoft.Extensions.Options.IConfigureOptions`1[[Microsoft.Extensions.Logging.LoggerFilterOptions, {{MicrosoftLoggingAssemblyName}}]]": {
                      "scope": "Singleton",
                      "type": "Microsoft.Extensions.Logging.DefaultLoggerLevelConfigureOptions",
                      "resource": "{{MicrosoftOptionsAssemblyName}}",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_transient_for_implementation_type()
    {
        var testServices = new ServiceCollection();
        testServices.AddTransient<ExampleService>();

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+ExampleService": {
                      "scope": "Transient",
                      "type": "{{typeof(ServicesActuatorTest)}}+ExampleService",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_keyed_transient_for_implementation_type()
    {
        var testServices = new ServiceCollection();
        testServices.AddKeyedTransient<ExampleService>("exampleKey");

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+ExampleService": {
                      "scope": "Transient",
                      "type": "{{typeof(ServicesActuatorTest)}}+ExampleService",
                      "key": "exampleKey",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_transient_for_interface_type_with_implementation_type()
    {
        var testServices = new ServiceCollection();
        testServices.AddTransient<IExampleService, ExampleService>();

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+IExampleService": {
                      "scope": "Transient",
                      "type": "{{typeof(ServicesActuatorTest)}}+ExampleService",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_keyed_transient_for_interface_type_with_implementation_type()
    {
        var testServices = new ServiceCollection();
        testServices.AddKeyedTransient<IExampleService, ExampleService>("exampleKey");

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+IExampleService": {
                      "scope": "Transient",
                      "type": "{{typeof(ServicesActuatorTest)}}+ExampleService",
                      "key": "exampleKey",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_scoped_for_implementation_factory()
    {
        var testServices = new ServiceCollection();
        testServices.AddScoped(_ => new ExampleService());

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+ExampleService": {
                      "scope": "Scoped",
                      "type": "{{typeof(ServicesActuatorTest)}}+ExampleService",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_keyed_scoped_for_implementation_factory()
    {
        var testServices = new ServiceCollection();
        testServices.AddKeyedScoped<ExampleService>("exampleKey", (_, _) => new ExampleService());

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+ExampleService": {
                      "scope": "Scoped",
                      "type": "{{typeof(ServicesActuatorTest)}}+ExampleService",
                      "key": "exampleKey",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_scoped_for_interface_type_with_implementation_factory()
    {
        var testServices = new ServiceCollection();
        testServices.AddScoped<IExampleService>(_ => new ExampleService());

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+IExampleService": {
                      "scope": "Scoped",
                      "type": "{{typeof(ServicesActuatorTest)}}+IExampleService",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_keyed_scoped_for_interface_type_with_implementation_factory()
    {
        var testServices = new ServiceCollection();
        testServices.AddKeyedScoped<IExampleService>("exampleKey", (_, _) => new ExampleService());

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+IExampleService": {
                      "scope": "Scoped",
                      "type": "{{typeof(ServicesActuatorTest)}}+IExampleService",
                      "key": "exampleKey",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_singleton_for_implementation_instance()
    {
        var testServices = new ServiceCollection();
        testServices.AddSingleton(new ExampleService());

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+ExampleService": {
                      "scope": "Singleton",
                      "type": "{{typeof(ServicesActuatorTest)}}+ExampleService",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_keyed_singleton_for_implementation_instance()
    {
        var testServices = new ServiceCollection();
        testServices.AddKeyedSingleton("exampleKey", new ExampleService());

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+ExampleService": {
                      "scope": "Singleton",
                      "type": "{{typeof(ServicesActuatorTest)}}+ExampleService",
                      "key": "exampleKey",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_singleton_for_interface_with_implementation_instance()
    {
        var testServices = new ServiceCollection();
        testServices.AddSingleton<IExampleService>(new ExampleService());

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+IExampleService": {
                      "scope": "Singleton",
                      "type": "{{typeof(ServicesActuatorTest)}}+ExampleService",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Can_resolve_keyed_singleton_for_interface_with_implementation_instance()
    {
        var testServices = new ServiceCollection();
        testServices.AddKeyedSingleton<IExampleService>("exampleKey", new ExampleService());

        string responseBody = await GetActuatorResponseAsync(testServices);

        responseBody.Should().BeJson($$"""
            {
              "contexts": {
                "application": {
                  "beans": {
                    "{{typeof(ServicesActuatorTest)}}+IExampleService": {
                      "scope": "Singleton",
                      "type": "{{typeof(ServicesActuatorTest)}}+ExampleService",
                      "key": "exampleKey",
                      "resource": "Steeltoe.Management.Endpoint.Test, Version={{SteeltoeTestAssemblyVersion}}, Culture=neutral, PublicKeyToken=null",
                      "dependencies": []
                    }
                  }
                }
              }
            }
            """);
    }

    private static async Task<string> GetActuatorResponseAsync(ServiceCollection testServices)
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddSingleton<IServiceCollection>(testServices);
        builder.Services.AddServicesActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/beans"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private interface IExampleService;

    internal sealed class ExampleService : IExampleService;
}
