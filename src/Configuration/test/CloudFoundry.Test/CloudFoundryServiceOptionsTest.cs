// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test;

public class CloudFoundryServiceOptionsTest
{
    [Fact]
    public void Constructor_WithNoVcapServicesConfiguration()
    {
        var builder = new ConfigurationBuilder();
        IConfigurationRoot configurationRoot = builder.Build();

        var options = new CloudFoundryServicesOptions(configurationRoot);

        Assert.NotNull(options);
        Assert.NotNull(options.Services);
        Assert.Empty(options.Services);
        Assert.Empty(options.GetServicesList());
    }

    [Fact]
    public void Constructor_WithSingleServiceConfiguration()
    {
        const string configJson = @"
                {
                    ""vcap"": {
                        ""services"" : {
                            ""p-config-server"": [{
                                ""credentials"": {
                                    ""access_token_uri"": ""https://p-spring-cloud-services.uaa.wise.com/oauth/token"",
                                    ""client_id"": ""p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef"",
                                    ""client_secret"": ""e8KF1hXvAnGd"",
                                    ""uri"": ""http://localhost:8888""
                                },
                                ""label"": ""p-config-server"",
                                ""name"": ""My Config Server"",
                                ""plan"": ""standard"",
                                ""tags"": [""configuration"",""spring-cloud""]
                            }]
                        }
                    }
                }";

        MemoryStream memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(memStream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot configurationRoot = builder.Build();

        var options = new CloudFoundryServicesOptions(configurationRoot);

        Assert.NotNull(options.Services);
        Assert.Single(options.Services);

        Assert.NotNull(options.Services["p-config-server"]);
        Assert.Single(options.Services["p-config-server"]);

        Service service = options.GetInstancesOfType("p-config-server").First();
        Assert.Equal("p-config-server", service.Label);
        Assert.Equal("My Config Server", service.Name);
        Assert.Equal("standard", service.Plan);

        Assert.NotNull(service.Tags);
        Assert.Equal(2, service.Tags.Count());
        Assert.Contains("configuration", service.Tags);
        Assert.Contains("spring-cloud", service.Tags);

        Assert.NotNull(service.Credentials);
        Assert.Equal(4, service.Credentials.Count);
        Assert.Equal("https://p-spring-cloud-services.uaa.wise.com/oauth/token", service.Credentials["access_token_uri"].Value);
        Assert.Equal("p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef", service.Credentials["client_id"].Value);
        Assert.Equal("e8KF1hXvAnGd", service.Credentials["client_secret"].Value);
        Assert.Equal("http://localhost:8888", service.Credentials["uri"].Value);
    }

    [Fact]
    public void Constructor_WithComplexSingleServiceConfiguration()
    {
        const string configJson = @"
                {
                    ""vcap"": {
                        ""services"" : {
                            ""p-rabbitmq"": [{
                                ""name"": ""rabbitmq"",
                                ""label"": ""p-rabbitmq"",
                                ""tags"": [
                                    ""rabbitmq"",
                                    ""messaging"",
                                    ""message-queue"",
                                    ""amqp"",
                                    ""stomp"",
                                    ""mqtt"",
                                    ""pivotal""
                                ],
                                ""plan"": ""standard"",
                                ""credentials"": {
                                    ""http_api_uris"": [
                                        ""https://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@pivotal-rabbitmq.system.testcloud.com/api/""
                                    ],
                                    ""ssl"": false,
                                    ""dashboard_url"": ""https://pivotal-rabbitmq.system.testcloud.com/#/login/268371bd-07e5-46f3-aec7-d1633ae20bbb/3fnpvbqm0djq5jl9fp6fc697f4"",
                                    ""password"": ""3fnpvbqm0djq5jl9fp6fc697f4"",
                                    ""protocols"": {
                                        ""management"": {
                                            ""path"": ""/api/"",
                                            ""ssl"": false,
                                            ""hosts"": [""192.168.0.97""],
                                            ""password"": ""3fnpvbqm0djq5jl9fp6fc697f4"",
                                            ""username"": ""268371bd-07e5-46f3-aec7-d1633ae20bbb"",
                                            ""port"": 15672,
                                            ""host"": ""192.168.0.97"",
                                            ""uri"": ""https://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:15672/api/"",
                                            ""uris"": [""https://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:15672/api/""]
                                        },
                                        ""amqp"": {
                                            ""vhost"": ""2260a117-cf28-4725-86dd-37b3b8971052"",
                                            ""username"": ""268371bd-07e5-46f3-aec7-d1633ae20bbb"",
                                            ""password"": ""3fnpvbqm0djq5jl9fp6fc697f4"",
                                            ""port"": 5672,
                                            ""host"": ""192.168.0.97"",
                                            ""hosts"": [ ""192.168.0.97""],
                                            ""ssl"": false,
                                            ""uri"": ""amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:5672/2260a117-cf28-4725-86dd-37b3b8971052"",
                                            ""uris"": [""amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:5672/2260a117-cf28-4725-86dd-37b3b8971052""]
                                        }
                                    },
                                    ""username"": ""268371bd-07e5-46f3-aec7-d1633ae20bbb"",
                                    ""hostname"": ""192.168.0.97"",
                                    ""hostnames"": [
                                        ""192.168.0.97""
                                        ],
                                    ""vhost"": ""2260a117-cf28-4725-86dd-37b3b8971052"",
                                    ""http_api_uri"": ""https://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@pivotal-rabbitmq.system.testcloud.com/api/"",
                                    ""uri"": ""amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97/2260a117-cf28-4725-86dd-37b3b8971052"",
                                    ""uris"": [
                                        ""amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97/2260a117-cf28-4725-86dd-37b3b8971052""
                                    ]
                                }
                            }]
                        }
                    }
                }";

        MemoryStream memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(memStream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot configurationRoot = builder.Build();

        var options = new CloudFoundryServicesOptions(configurationRoot);

        Assert.NotNull(options.Services);
        Assert.Single(options.Services);
        Service service = options.GetInstancesOfType("p-rabbitmq").First();
        Assert.Equal("p-rabbitmq", service.Label);
        Assert.Equal("rabbitmq", service.Name);
        Assert.Equal("standard", service.Plan);

        Assert.NotNull(service.Tags);
        Assert.Equal(7, service.Tags.Count());
        Assert.Contains("rabbitmq", service.Tags);
        Assert.Contains("pivotal", service.Tags);

        Assert.NotNull(service.Credentials);
        Assert.Equal(12, service.Credentials.Count);

        Assert.Equal("https://pivotal-rabbitmq.system.testcloud.com/#/login/268371bd-07e5-46f3-aec7-d1633ae20bbb/3fnpvbqm0djq5jl9fp6fc697f4",
            service.Credentials["dashboard_url"].Value);

        Assert.Equal("268371bd-07e5-46f3-aec7-d1633ae20bbb", service.Credentials["username"].Value);
        Assert.Equal("3fnpvbqm0djq5jl9fp6fc697f4", service.Credentials["password"].Value);
        Assert.Equal("268371bd-07e5-46f3-aec7-d1633ae20bbb", service.Credentials["protocols"]["amqp"]["username"].Value);
        Assert.Equal("3fnpvbqm0djq5jl9fp6fc697f4", service.Credentials["protocols"]["amqp"]["password"].Value);

        Assert.Equal("amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:5672/2260a117-cf28-4725-86dd-37b3b8971052",
            service.Credentials["protocols"]["amqp"]["uris"]["0"].Value);
    }

    [Fact]
    public void Constructor_WithMultipleSameServicesConfiguration()
    {
        const string configJson = @"
                {
                    ""vcap"": {
                        ""services"" : {
                            ""p-mysql"": [
                            {
                                ""name"": ""mySql1"",
                                ""label"": ""p-mysql"",
                                ""tags"": [""mysql"",""relational""],
                                ""plan"": ""100mb-dev"",
                                ""credentials"": {
                                    ""hostname"": ""192.168.0.97"",
                                    ""port"": 3306,
                                    ""name"": ""cf_0f5dda44_e678_4727_993f_30e6d455cc31"",
                                    ""username"": ""9vD0Mtk3wFFuaaaY"",
                                    ""password"": ""Cjn4HsAiKV8sImst"",
                                    ""uri"": ""mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true"",
                                    ""jdbcUrl"": ""jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst""
                                }
                            },
                            {
                                ""name"": ""mySql2"",
                                ""label"": ""p-mysql"",
                                ""tags"": [""mysql"",""relational""],
                                ""plan"": ""100mb-dev"",
                                ""credentials"": {
                                    ""hostname"": ""192.168.0.97"",
                                    ""port"": 3306,
                                    ""name"": ""cf_0f5dda44_e678_4727_993f_30e6d455cc31"",
                                    ""username"": ""9vD0Mtk3wFFuaaaY"",
                                    ""password"": ""Cjn4HsAiKV8sImst"",
                                    ""uri"": ""mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true"",
                                    ""jdbcUrl"": ""jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst""
                                }
                            }]
                        }
                    }
                }";

        MemoryStream memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(memStream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot configurationRoot = builder.Build();

        var options = new CloudFoundryServicesOptions(configurationRoot);

        Assert.NotNull(options.Services);
        Assert.Single(options.Services);
        Assert.NotNull(options.Services["p-mysql"]);

        Assert.Equal(2, options.GetServicesList().Count());

        Service service1 = options.GetServicesList().First(n => n.Name == "mySql1");
        Service service2 = options.GetServicesList().First(n => n.Name == "mySql2");
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Equal("p-mysql", service1.Label);
        Assert.Equal("192.168.0.97", service1.Credentials["hostname"].Value);
        Assert.Equal("3306", service1.Credentials["port"].Value);
        Assert.Equal("cf_0f5dda44_e678_4727_993f_30e6d455cc31", service1.Credentials["name"].Value);
        Assert.Equal("p-mysql", service2.Label);
        Assert.Equal("192.168.0.97", service2.Credentials["hostname"].Value);
        Assert.Equal("3306", service2.Credentials["port"].Value);
        Assert.Equal("cf_0f5dda44_e678_4727_993f_30e6d455cc31", service2.Credentials["name"].Value);
    }

    [Fact]
    public void Constructor_WithIConfigurationRootBinds()
    {
        const string configJson = @"
{
    ""vcap"": {
        ""services"" : {
            ""p-config-server"": [{
                ""credentials"": {
                    ""access_token_uri"": ""https://p-spring-cloud-services.uaa.wise.com/oauth/token"",
                    ""client_id"": ""p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef"",
                    ""client_secret"": ""e8KF1hXvAnGd"",
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
        }
    }
}";

        MemoryStream memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(memStream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot configurationRoot = builder.Build();

        var options = new CloudFoundryServicesOptions(configurationRoot);

        Assert.NotNull(options.Services);
        Assert.Single(options.Services);

        Assert.NotNull(options.Services["p-config-server"]);
        Assert.Single(options.Services["p-config-server"]);

        Service firstService = options.GetServicesList().First();
        Assert.Equal("p-config-server", firstService.Label);
        Assert.Equal("My Config Server", firstService.Name);
        Assert.Equal("standard", firstService.Plan);

        Assert.NotNull(firstService.Tags);
        Assert.Equal(2, firstService.Tags.Count());
        Assert.Contains("configuration", firstService.Tags);
        Assert.Contains("spring-cloud", firstService.Tags);

        Assert.NotNull(firstService.Credentials);
        Assert.Equal(4, firstService.Credentials.Count);
        Assert.Equal("https://p-spring-cloud-services.uaa.wise.com/oauth/token", firstService.Credentials["access_token_uri"].Value);
        Assert.Equal("p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef", firstService.Credentials["client_id"].Value);
        Assert.Equal("e8KF1hXvAnGd", firstService.Credentials["client_secret"].Value);
        Assert.Equal("http://localhost:8888", firstService.Credentials["uri"].Value);
    }

    [Fact]
    public void Constructor_WithIConfigurationBinds()
    {
        const string configJson = @"
                {
                    ""vcap"": {
                        ""services"" : {
                            ""p-config-server"": [{
                                ""credentials"": {
                                    ""access_token_uri"": ""https://p-spring-cloud-services.uaa.wise.com/oauth/token"",
                                    ""client_id"": ""p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef"",
                                    ""client_secret"": ""e8KF1hXvAnGd"",
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
                        }
                    }
                }";

        MemoryStream memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(memStream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot configurationRoot = builder.Build();
        var options = new CloudFoundryServicesOptions(configurationRoot);

        Assert.NotNull(options.Services);
        Assert.Single(options.Services);

        Assert.NotNull(options.Services["p-config-server"]);
        Assert.Single(options.Services["p-config-server"]);

        Service service = options.GetServicesList().First();
        Assert.Equal("p-config-server", service.Label);
        Assert.Equal("My Config Server", service.Name);
        Assert.Equal("standard", service.Plan);

        Assert.NotNull(service.Tags);
        Assert.Equal(2, service.Tags.Count());
        Assert.Contains("configuration", service.Tags);
        Assert.Contains("spring-cloud", service.Tags);

        Assert.NotNull(service.Credentials);
        Assert.Equal(4, service.Credentials.Count);
        Assert.Equal("https://p-spring-cloud-services.uaa.wise.com/oauth/token", service.Credentials["access_token_uri"].Value);
        Assert.Equal("p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef", service.Credentials["client_id"].Value);
        Assert.Equal("e8KF1hXvAnGd", service.Credentials["client_secret"].Value);
        Assert.Equal("http://localhost:8888", service.Credentials["uri"].Value);
    }
}
