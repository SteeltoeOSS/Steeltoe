// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.ITest;

// NOTE: Some of the tests assume a running Spring Cloud Config Server is started
//       with repository data for application: foo, profile: development
//
//       The easiest way to get that to happen is clone the spring-cloud-config
//       repo and run the config-server.
//          eg. git clone https://github.com/spring-cloud/spring-cloud-config.git
//              cd spring-cloud-config\spring-cloud-config-server
//              mvn spring-boot:run
public class ConfigServerConfigurationExtensionsIntegrationTest
{
    [Fact]
    [Trait("Category", "Integration")]
    public void SpringCloudConfigServer_ReturnsExpectedDefaultData()
    {
        string appsettings = @"
                {
                    ""spring"": {
                      ""application"": {
                        ""name"" : ""foo""
                      },
                      ""cloud"": {
                        ""config"": {
                            ""uri"": ""http://localhost:8888"",
                            ""env"": ""development"",
                            ""failfast"": ""true""
                        }
                      }
                    }
                }";

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        IHostEnvironment hostingEnv = HostingHelpers.GetHostingEnvironment();
        configurationBuilder.AddJsonFile(fileName);

        // Act and Assert (expects Spring Cloud Config server to be running)
        configurationBuilder.AddConfigServer(hostingEnv);
        IConfigurationRoot root = configurationBuilder.Build();

        Assert.Equal("spam", root["bar"]);
        Assert.Equal("from foo development", root["foo"]);
        Assert.Equal("Spring Cloud Samples", root["info:description"]);
        Assert.Equal("https://github.com/spring-cloud-samples", root["info:url"]);
        Assert.Equal("http://localhost:8761/eureka/", root["eureka:client:serviceUrl:defaultZone"]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SpringCloudConfigServer_ReturnsExpectedDefaultData_AsInjectedOptions()
    {
        // These settings match the default java config server
        string appsettings = @"
                {
                    ""spring"": {
                      ""application"": {
                        ""name"": ""foo""
                      },
                      ""cloud"": {
                        ""config"": {
                            ""uri"": ""http://localhost:8888"",
                            ""env"": ""development"",
                            ""health"": {
                                ""enabled"": true
                            },
                            ""failfast"": ""true""
                        }
                      }
                    }
                }";

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);

        IWebHostBuilder builder = new WebHostBuilder().UseEnvironment("development").UseStartup<TestServerStartup>().ConfigureAppConfiguration(
            (context, config) =>
            {
                config.SetBasePath(directory).AddJsonFile(fileName).AddConfigServer(context.HostingEnvironment);
            });

        // Act and Assert (TestServer expects Spring Cloud Config server to be running)
        using var server = new TestServer(builder);
        using HttpClient client = server.CreateClient();
        string result = await client.GetStringAsync("http://localhost/Home/VerifyAsInjectedOptions");

        Assert.Equal("spam" + "from foo development" + "Spring Cloud Samples" + "https://github.com/spring-cloud-samples", result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SpringCloudConfigServer_ConfiguredViaCloudfoundryEnv_ReturnsExpectedDefaultData_AsInjectedOptions()
    {
        string vcap_application = @" 
                {
                    ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
                    ""application_name"": ""foo"",
                    ""application_uris"": [
                        ""foo.10.244.0.34.xip.io""
                    ],
                    ""application_version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"",
                    ""limits"": {
                        ""disk"": 1024,
                        ""fds"": 16384,
                        ""mem"": 256
                    },
                    ""name"": ""foo"",
                    ""space_id"": ""06450c72-4669-4dc6-8096-45f9777db68a"",
                    ""space_name"": ""my-space"",
                    ""uris"": [
                        ""foo.10.244.0.34.xip.io"",
                        ""foo.10.244.0.34.xip.io""
                    ],
                    ""users"": null,
                    ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                }";

        string vcap_services = @"
                {
                    ""p-config-server"": [{
                        ""credentials"": {
                            ""access_token_uri"": null,
                            ""client_id"": null,
                            ""client_secret"": null,
                            ""uri"": ""http://localhost:8888""
                        },
                        ""label"": ""p-config-server"",
                        ""name"": ""My Config Server"",
                        ""plan"": ""standard"",
                        ""tags"": [
                            ""configuration"",
                            ""spring-cloud""
                        ]
                    }]
                }";

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);

        string appSettings = @"
                {
                    ""spring"": {
                        ""cloud"": {
                            ""config"": {
                                ""validateCertificates"": false,
                                ""failfast"": ""true""
                            }
                        }
                    }
                }";

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appSettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);

        IWebHostBuilder builder = new WebHostBuilder().UseEnvironment("development").UseStartup<TestServerStartup>().ConfigureAppConfiguration(
            (context, config) =>
            {
                config.SetBasePath(directory).AddJsonFile(fileName).AddConfigServer(context.HostingEnvironment);
            });

        try
        {
            // Act and Assert (TestServer expects Spring Cloud Config server to be running @ localhost:8888)
            using var server = new TestServer(builder);
            using HttpClient client = server.CreateClient();
            string result = await client.GetStringAsync("http://localhost/Home/VerifyAsInjectedOptions");

            Assert.Equal("spam" + "from foo development" + "Spring Cloud Samples" + "https://github.com/spring-cloud-samples", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }
    }

    [Fact(Skip = "Requires matching PCF environment with SCCS provisioned")]
    [Trait("Category", "Integration")]
    public async Task SpringCloudConfigServer_ConfiguredViaCloudfoundryEnv()
    {
        string vcap_application = @" 
                {
                    ""limits"": {
                    ""mem"": 1024,
                    ""disk"": 1024,
                    ""fds"": 16384
                    },
                    ""application_id"": ""c2e03250-62e3-4494-82fb-1bc6e2e25ad0"",
                    ""application_version"": ""ef087dfd-2955-4854-86c1-4a2cf30e05b3"",
                    ""application_name"": ""test"",
                    ""application_uris"": [
                    ""test.apps.testcloud.com""
                    ],
                    ""version"": ""ef087dfd-2955-4854-86c1-4a2cf30e05b3"",
                    ""name"": ""test"",
                    ""space_name"": ""development"",
                    ""space_id"": ""ff257d70-eeed-4487-9d6c-4ac709f76aea"",
                    ""uris"": [
                    ""test.apps.testcloud.com""
                    ],
                    ""users"": null
                }";

        string vcap_services = @"
                {
                    ""p-config-server"": [
                    {
                        ""name"": ""myConfigServer"",
                        ""label"": ""p-config-server"",
                        ""tags"": [
                        ""configuration"",
                        ""spring-cloud""
                        ],
                        ""plan"": ""standard"",
                        ""credentials"": {
                        ""uri"": ""https://config-5b3af2c9-754f-4eb6-9d4b-da50d33d5a5f.apps.testcloud.com"",
                        ""client_id"": ""p-config-server-690772bc-2820-4a2c-9c76-6d8ccf8e8de5"",
                        ""client_secret"": ""Ib9RFhVPuLub"",
                        ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        }
                    }
                    ]
                }";

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);

        string appSettings = @"
                {
                    ""spring"": {
                        ""cloud"": {
                            ""config"": {
                                ""validate_certificates"": false,
                                ""failfast"": ""true""
                            }
                        }
                    }
                }";

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appSettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);

        IWebHostBuilder builder = new WebHostBuilder().UseEnvironment("development").UseStartup<TestServerStartup>().ConfigureAppConfiguration(
            (context, config) =>
            {
                config.SetBasePath(directory).AddJsonFile(fileName).AddConfigServer(context.HostingEnvironment);
            });

        try
        {
            // Act and Assert (TestServer expects Spring Cloud Config server to be running)
            using var server = new TestServer(builder);
            using HttpClient client = server.CreateClient();
            string result = await client.GetStringAsync("http://localhost/Home/VerifyAsInjectedOptions");

            Assert.Equal("spam" + "barcelona" + "Spring Cloud Samples" + "https://github.com/spring-cloud-samples", result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }
    }

    // NOTE: This test assumes a running Spring Cloud Config Server is started
    //      and a Spring Cloud Eureka Server is also running. The Config server must be
    //      configured to register itself with Eureka upon start up.
    //
    //      The easiest way to get that to happen is create a spring boot application:
    //  @SpringBootApplication
    //  @EnableDiscoveryClient
    //  @EnableConfigServer
    //  public class ConfigServerApplication
    //  {
    //    public static void main(String[] args)
    //    {
    //        SpringApplication.run(ConfigServerApplication.class, args);
    //    }
    //  }
    //
    //  Then configure the above Config Server as follows (application.yml)
    //
    //  info:
    //      component: Config Server
    //  spring:
    //      application:
    //          name: configserver
    //      autoconfigure.exclude: org.springframework.boot.autoconfigure.jdbc.DataSourceAutoConfiguration
    //      jmx:
    //          default_domain: cloud.config.server
    //      cloud:
    //          config:
    //              server:
    //                  git:
    //                      uri: https://github.com/spring-cloud-samples/config-repo
    //                  repos:
    //                      - patterns: multi-repo-demo-*
    //                        uri: https://github.com/spring-cloud-samples/config-repo
    //
    //  server:
    //      port: 8888
    //  eureka:
    //      instance:
    //          hostname: localhost
    //          port: 8888
    //      client:
    //          registerWithEureka: true
    //      fetchRegistry: true
    //      serviceUrl:
    //          defaultZone: http://localhost:8761/eureka/
    [Fact(Skip = "Config server image needs to be enhanced to support discovery-first")]
    [Trait("Category", "Integration")]
    public void SpringCloudConfigServer_DiscoveryFirst_ReturnsExpectedDefaultData()
    {
        string appsettings = @"
                {
                    ""spring"": {
                      ""application"": {
                        ""name"" : ""foo""
                      },
                      ""cloud"": {
                        ""config"": {
                            ""uri"": ""http://localhost:8888"",
                            ""env"": ""development"",
                            ""failfast"": ""true"",
                            ""discovery"": {
                                ""enabled"": true
                            }
                        }
                      }
                    },
                    ""eureka"": {
                        ""client"": {
                            ""serviceUrl"": ""http://localhost:8761/eureka/""
                        }
                    }
                }";

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        IHostEnvironment hostingEnv = HostingHelpers.GetHostingEnvironment("development");
        configurationBuilder.AddJsonFile(fileName);

        // Act and Assert (expects Spring Cloud Config server to be running)
        configurationBuilder.AddConfigServer(hostingEnv);
        IConfigurationRoot root = configurationBuilder.Build();

        Assert.Equal("spam", root["bar"]);
        Assert.Equal("from foo development", root["foo"]);
        Assert.Equal("Spring Cloud Samples", root["info:description"]);
        Assert.Equal("https://github.com/spring-cloud-samples", root["info:url"]);
        Assert.Equal("http://localhost:8761/eureka/", root["eureka:client:serviceUrl:defaultZone"]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SpringCloudConfigServer_WithHealthEnabled_ReturnsHealth()
    {
        // These settings match the default java config server
        string appsettings = @"
                {
                    ""spring"": {
                      ""application"": {
                        ""name"": ""foo""
                      },
                      ""cloud"": {
                        ""config"": {
                            ""uri"": ""http://localhost:8888"",
                            ""env"": ""development"",
                            ""health"": {
                                ""enabled"": true
                            },
                            ""failfast"": ""true""
                        }
                      }
                    }
                }";

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);

        IWebHostBuilder builder = new WebHostBuilder().UseStartup<TestServerStartup>().ConfigureAppConfiguration((context, config) =>
        {
            config.SetBasePath(directory).AddJsonFile(fileName).AddConfigServer(context.HostingEnvironment);
        });

        // Act and Assert (TestServer expects Spring Cloud Config server to be running)
        using var server = new TestServer(builder);
        using HttpClient client = server.CreateClient();
        string result = await client.GetStringAsync("http://localhost/Home/Health");

        // after switching to newer config server image, the health response has changed to
        // https://github.com/spring-cloud-samples/config-repo/Config resource 'file [/tmp/config-repo-4389533880216684481/application.yml' via location '' (document #0)"
        Assert.StartsWith(
            "UP,https://github.com/spring-cloud-samples/config-repo/foo-development.properties,https://github.com/spring-cloud-samples/config-repo/foo.properties,https://github.com/spring-cloud-samples/config-repo/Config",
            result);
    }
}
