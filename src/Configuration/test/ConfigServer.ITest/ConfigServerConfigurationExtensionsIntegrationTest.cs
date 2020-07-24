// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System.IO;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.ITest
{
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
        public ConfigServerConfigurationExtensionsIntegrationTest()
        {
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void SpringCloudConfigServer_ReturnsExpectedDefaultData()
        {
            // Arrange
            var appsettings = @"
                {
                    ""spring"": {
                      ""application"": {
                        ""name"" : ""foo""
                      },
                      ""cloud"": {
                        ""config"": {
                            ""uri"": ""http://localhost:8888"",
                            ""env"": ""development""
                        }
                      }
                    }
                }";

            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var hostingEnv = HostingHelpers.GetHostingEnvironment();
            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert (expects Spring Cloud Config server to be running)
            configurationBuilder.AddConfigServer(hostingEnv);
            var root = configurationBuilder.Build();

            Assert.Equal("spam", root["bar"]);
            Assert.Equal("from foo development", root["foo"]);
            Assert.Equal("Spring Cloud Samples", root["info:description"]);
            Assert.Equal("https://github.com/spring-cloud-samples", root["info:url"]);
            Assert.Equal("http://localhost:8761/eureka/", root["eureka:client:serviceUrl:defaultZone"]);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async void SpringCloudConfigServer_ReturnsExpectedDefaultData_AsInjectedOptions()
        {
            // These settings match the default java config server
            var appsettings = @"
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
                            }
                        }
                      }
                    }
                }";
            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var builder = new WebHostBuilder()
                .UseEnvironment("development")
                .UseStartup<TestServerStartup>()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(directory)
                    .AddJsonFile(fileName)
                    .AddConfigServer(context.HostingEnvironment);
                });

            // Act and Assert (TestServer expects Spring Cloud Config server to be running)
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetStringAsync("http://localhost/Home/VerifyAsInjectedOptions");

                Assert.Equal(
                    "spam" +
                    "from foo development" +
                    "Spring Cloud Samples" +
                    "https://github.com/spring-cloud-samples", result);
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async void SpringCloudConfigServer_ConfiguredViaCloudfoundryEnv_ReturnsExpectedDefaultData_AsInjectedOptions()
        {
            // Arrange
            var vcap_application = @" 
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

            var vcap_services = @"
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

            System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
            System.Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);

            var appSettings = @"
                {
                    ""spring"": {
                        ""cloud"": {
                            ""config"": {
                                ""validateCertificates"": false
                            }
                        }
                    }
                }";

            var path = TestHelpers.CreateTempFile(appSettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var builder = new WebHostBuilder()
                .UseEnvironment("development")
                .UseStartup<TestServerStartup>()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(directory)
                    .AddJsonFile(fileName)
                    .AddConfigServer(context.HostingEnvironment);
                });
            try
            {
                // Act and Assert (TestServer expects Spring Cloud Config server to be running @ localhost:8888)
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var result = await client.GetStringAsync("http://localhost/Home/VerifyAsInjectedOptions");

                    Assert.Equal(
                        "spam" +
                        "from foo development" +
                        "Spring Cloud Samples" +
                        "https://github.com/spring-cloud-samples", result);
                }
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
                System.Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
            }
        }

        [Fact(Skip = "Requires matching PCF environment with SCCS provisioned")]
        [Trait("Category", "Integration")]
        public async void SpringCloudConfigServer_ConfiguredViaCloudfoundryEnv()
        {
            // Arrange
            var vcap_application = @" 
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

            var vcap_services = @"
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

            System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
            System.Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);

            var appSettings = @"
                {
                    ""spring"": {
                        ""cloud"": {
                            ""config"": {
                                ""validate_certificates"": false
                            }
                        }
                    }
                }";
            var path = TestHelpers.CreateTempFile(appSettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var builder = new WebHostBuilder()
                .UseEnvironment("development")
                .UseStartup<TestServerStartup>()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(directory)
                    .AddJsonFile(fileName)
                    .AddConfigServer(context.HostingEnvironment);
                });
            try
            {
                // Act and Assert (TestServer expects Spring Cloud Config server to be running)
                using (var server = new TestServer(builder))
                {
                    var client = server.CreateClient();
                    var result = await client.GetStringAsync("http://localhost/Home/VerifyAsInjectedOptions");

                    Assert.Equal(
                        "spam" +
                        "barcelona" +
                        "Spring Cloud Samples" +
                        "https://github.com/spring-cloud-samples", result);
                }
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
                System.Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
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
        [Fact]
        [Trait("Category", "Integration")]
        public void SpringCloudConfigServer_DiscoveryFirst_ReturnsExpectedDefaultData()
        {
            // Arrange
            var appsettings = @"
                {
                    ""spring"": {
                      ""application"": {
                        ""name"" : ""foo""
                      },
                      ""cloud"": {
                        ""config"": {
                            ""uri"": ""http://localhost:8888"",
                            ""env"": ""development"",
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

            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var hostingEnv = HostingHelpers.GetHostingEnvironment("development");
            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert (expects Spring Cloud Config server to be running)
            configurationBuilder.AddConfigServer(hostingEnv);
            var root = configurationBuilder.Build();

            Assert.Equal("spam", root["bar"]);
            Assert.Equal("from foo development", root["foo"]);
            Assert.Equal("Spring Cloud Samples", root["info:description"]);
            Assert.Equal("https://github.com/spring-cloud-samples", root["info:url"]);
            Assert.Equal("http://localhost:8761/eureka/", root["eureka:client:serviceUrl:defaultZone"]);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async void SpringCloudConfigServer_WithHealthEnabled_ReturnsHealth()
        {
            // These settings match the default java config server
            var appsettings = @"
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
                            }
                        }
                      }
                    }
                }";
            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var builder = new WebHostBuilder()
                .UseStartup<TestServerStartup>()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(directory)
                    .AddJsonFile(fileName)
                    .AddConfigServer(context.HostingEnvironment);
                });

            // Act and Assert (TestServer expects Spring Cloud Config server to be running)
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetStringAsync("http://localhost/Home/Health");

                // changed to StartsWith on 7/23 because SCCS is appending " document #0)" to application.yml ??
                Assert.StartsWith("UP,https://github.com/spring-cloud-samples/config-repo/foo-development.properties,https://github.com/spring-cloud-samples/config-repo/foo.properties,https://github.com/spring-cloud-samples/config-repo/application.yml", result);
            }
        }
    }
}
