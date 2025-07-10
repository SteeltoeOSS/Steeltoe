// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Configuration.ConfigServer.Integration.Test;

// NOTE: Some of the tests assume a running Spring Cloud Config Server is started
//       with repository data for application: foo, profile: development
//
//       The easiest way to get that to happen is clone the spring-cloud-config
//       repo and run the config-server.
//          e.g. git clone https://github.com/spring-cloud/spring-cloud-config.git
//               cd spring-cloud-config\spring-cloud-config-server
//               mvn spring-boot:run
public sealed class ConfigServerConfigurationExtensionsIntegrationTest
{
    [Fact]
    [Trait("Category", "Integration")]
    public void SpringCloudConfigServer_ReturnsExpectedDefaultData()
    {
        const string appSettings = """
            {
                "spring": {
                  "application": {
                    "name" : "foo"
                  },
                  "cloud": {
                    "config": {
                        "uri": "http://localhost:8888",
                        "env": "development",
                        "failFast": "true"
                    }
                  }
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);

        configurationBuilder.AddConfigServer();
        IConfigurationRoot root = configurationBuilder.Build();

        root["bar"].Should().Be("spam");
        root["foo"].Should().Be("from foo development");
        root["info:description"].Should().Be("Spring Cloud Samples");
        root["info:url"].Should().Be("https://github.com/spring-cloud-samples");
        root["eureka:client:serviceUrl:defaultZone"].Should().Be("http://localhost:8761/eureka/");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SpringCloudConfigServer_ReturnsExpectedDefaultData_AsInjectedOptions()
    {
        // These settings match the default java config server
        const string appSettings = """
            {
                "spring": {
                  "application": {
                    "name": "foo"
                  },
                  "cloud": {
                    "config": {
                        "uri": "http://localhost:8888",
                        "env": "development",
                        "health": {
                            "enabled": true
                        },
                        "failFast": "true"
                    }
                  }
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseEnvironment("development");
        builder.UseStartup<TestServerStartup>();

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(fileName);
        });

        builder.AddConfigServer();

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        string result = await client.GetStringAsync(new Uri("http://localhost/Home/VerifyAsInjectedOptions"), TestContext.Current.CancellationToken);

        result.Should().Be("spamfrom foo developmentSpring Cloud Sampleshttps://github.com/spring-cloud-samples");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SpringCloudConfigServer_ConfiguredViaCloudfoundryEnv_ReturnsExpectedDefaultData_AsInjectedOptions()
    {
        const string vcapApplication = """
            {
                "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3",
                "application_name": "foo",
                "application_uris": [
                    "foo.10.244.0.34.xip.io"
                ],
                "application_version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca",
                "limits": {
                    "disk": 1024,
                    "fds": 16384,
                    "mem": 256
                },
                "name": "foo",
                "space_id": "06450c72-4669-4dc6-8096-45f9777db68a",
                "space_name": "my-space",
                "uris": [
                    "foo.10.244.0.34.xip.io",
                    "foo.10.244.0.34.xip.io"
                ],
                "users": null,
                "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"
            }
            """;

        const string vcapServices = """
            {
                "p-config-server": [{
                    "credentials": {
                        "access_token_uri": null,
                        "client_id": null,
                        "client_secret": null,
                        "uri": "http://localhost:8888"
                    },
                    "label": "p-config-server",
                    "name": "My Config Server",
                    "plan": "standard",
                    "tags": [
                        "configuration",
                        "spring-cloud"
                    ]
                }]
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);

        const string appSettings = """
            {
                "spring": {
                    "cloud": {
                        "config": {
                            "validateCertificates": false,
                            "failFast": "true"
                        }
                    }
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseEnvironment("development");
        builder.UseStartup<TestServerStartup>();

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(fileName);
        });

        builder.AddConfigServer();

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        string result = await client.GetStringAsync(new Uri("http://localhost/Home/VerifyAsInjectedOptions"), TestContext.Current.CancellationToken);

        result.Should().Be("spamfrom foo developmentSpring Cloud Sampleshttps://github.com/spring-cloud-samples");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void SpringCloudConfigServer_DiscoveryFirst_ReturnsExpectedDefaultData()
    {
        const string appSettings = """
            {
                "spring": {
                  "application": {
                    "name" : "foo"
                  },
                  "cloud": {
                    "config": {
                        "env": "development",
                        "failFast": "true",
                        "discovery": {
                            "enabled": true
                        }
                    }
                  }
                },
                "eureka": {
                    "client": {
                        "enabled": true,
                        "serviceUrl": "http://localhost:8761/eureka/"
                    }
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.Add(FastTestConfigurations.Discovery);
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot root = configurationBuilder.Build();

        root["bar"].Should().Be("spam");
        root["foo"].Should().Be("from foo development");
        root["info:description"].Should().Be("Spring Cloud Samples");
        root["info:url"].Should().Be("https://github.com/spring-cloud-samples");
        root["eureka:client:serviceUrl:defaultZone"].Should().Be("http://localhost:8761/eureka/");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SpringCloudConfigServer_WithHealthEnabled_ReturnsHealth()
    {
        // These settings match the default java config server
        const string appSettings = """
            {
                "spring": {
                  "application": {
                    "name": "foo"
                  },
                  "cloud": {
                    "config": {
                        "uri": "http://localhost:8888",
                        "env": "development",
                        "health": {
                            "enabled": true
                        },
                        "failFast": "true"
                    }
                  }
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<TestServerStartup>();

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(fileName);
        });

        builder.AddConfigServer();

        using IWebHost host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = host.GetTestClient();
        string result = await client.GetStringAsync(new Uri("http://localhost/Home/Health"), TestContext.Current.CancellationToken);

        // after switching to newer config server image, the health response has changed to
        // https://github.com/spring-cloud-samples/config-repo/Config resource 'file [/tmp/config-repo-4389533880216684481/application.yml' via location '' (document #0)"
        result.Should().StartWith(
            "UP,https://github.com/spring-cloud-samples/config-repo/foo-development.properties,https://github.com/spring-cloud-samples/config-repo/foo.properties,https://github.com/spring-cloud-samples/config-repo/Config");
    }
}
