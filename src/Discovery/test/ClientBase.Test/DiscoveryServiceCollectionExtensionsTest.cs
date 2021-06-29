﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
using Steeltoe.Connector;
using Steeltoe.Discovery.Client.SimpleClients;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Kubernetes;
using Steeltoe.Discovery.Kubernetes.Discovery;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Xunit;

namespace Steeltoe.Discovery.Client.Test
{
    public class DiscoveryServiceCollectionExtensionsTest : IDisposable
    {
        [Fact]
        public void AddDiscoveryClient_WithEurekaConfig_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = @"
                {
                    ""spring"": {
                        ""application"": {
                            ""name"": ""myName""
                        },
                    },
                    ""eureka"": {
                        ""client"": {
                            ""shouldFetchRegistry"": false,
                            ""shouldRegisterWithEureka"": false,
                            ""serviceUrl"": ""http://localhost:8761/eureka/""
                        }
                    }
                }";

            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();
            services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
            services.AddDiscoveryClient(config);

            var service = services.BuildServiceProvider().GetService<IDiscoveryClient>();
            Assert.NotNull(service);
        }

        [Fact]
        public void AddDiscoveryClient_WithEurekaInetConfig_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "spring:cloud:inet:defaulthostname", "fromtest" },
                { "spring:cloud:inet:skipReverseDnsLookup", "true" },
                { "eureka:client:shouldFetchRegistry", "false" },
                { "eureka:client:shouldRegisterWithEureka", "false" },
                { "eureka:instance:useNetUtils", "true" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();
            services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
            services.AddDiscoveryClient(config);

            var service = services.BuildServiceProvider().GetService<IDiscoveryClient>();
            Assert.NotNull(service);
            var instanceInfo = service.GetLocalServiceInstance();
            Assert.Equal("fromtest", instanceInfo.Host);
        }

        [Fact]
        public void AddDiscoveryClient_WithEurekaClientCertConfig_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>()
            {
                { "spring:application:name", "myName" },
                { "eureka:client:serviceUrl", "http://localhost:8761/eureka/" }
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(appsettings)
                .AddPemFiles("instance.crt", "instance.key")
                .Build();

            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();
            services.Configure<CertificateOptions>(config);
            services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
            services.AddDiscoveryClient(config);

            // act
            var serviceProvider = services.BuildServiceProvider();
            var discoveryClient = serviceProvider.GetService<IDiscoveryClient>();
            var handlerProvider = serviceProvider.GetService<IHttpClientHandlerProvider>();

            // assert
            Assert.NotNull(discoveryClient);
            Assert.NotNull(handlerProvider);
        }

        [Fact]
        public void AddDiscoveryClient_WithNoConfig_AddsNoOpDiscoveryClient()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>() { { "spring:application:name", "myName" } };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection().AddSingleton<IConfiguration>(config);

            // Act
            services.AddDiscoveryClient();
            var client = services.BuildServiceProvider().GetRequiredService<IDiscoveryClient>();

            // assert
            Assert.NotNull(client);
            Assert.IsType<NoOpDiscoveryClient>(client);
            Assert.Empty(client.Services);
            Assert.Empty(client.GetInstances("any"));
        }

        [Fact]
        public void AddDiscoveryClient_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => DiscoveryServiceCollectionExtensions.AddDiscoveryClient(services, config, "foobar"));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddDiscoveryClient_MultipleRegistryServices_ThrowsConnectorException()
        {
            // Arrange
            var env1 = @"
                {
                    ""limits"": {
                    ""fds"": 16384,
                    ""mem"": 1024,
                    ""disk"": 1024
                    },
                    ""application_name"": ""spring-cloud-broker"",
                    ""application_uris"": [
                    ""spring-cloud-broker.apps.testcloud.com""
                    ],
                    ""name"": ""spring-cloud-broker"",
                    ""space_name"": ""p-spring-cloud-services"",
                    ""space_id"": ""65b73473-94cc-4640-b462-7ad52838b4ae"",
                    ""uris"": [
                    ""spring-cloud-broker.apps.testcloud.com""
                    ],
                    ""users"": null,
                    ""version"": ""07e112f7-2f71-4f5a-8a34-db51dbed30a3"",
                    ""application_version"": ""07e112f7-2f71-4f5a-8a34-db51dbed30a3"",
                    ""application_id"": ""798c2495-fe75-49b1-88da-b81197f2bf06""
                }";
            var env2 = @"
                {
                    ""p-service-registry"": [
                    {
                        ""credentials"": {
                            ""uri"": ""https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com"",
                            ""client_id"": ""p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe"",
                            ""client_secret"": ""dCsdoiuklicS"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-service-registry"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myDiscoveryService"",
                        ""tags"": [
                            ""eureka"",
                            ""discovery"",
                            ""registry"",
                            ""spring-cloud""
                        ]
                    },
                    {
                        ""credentials"": {
                            ""uri"": ""https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com"",
                            ""client_id"": ""p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe"",
                            ""client_secret"": ""dCsdoiuklicS"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-service-registry"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myDiscoveryService2"",
                        ""tags"": [
                            ""eureka"",
                            ""discovery"",
                            ""registry"",
                            ""spring-cloud""
                        ]
                    }]
                }";

            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", env1);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => DiscoveryServiceCollectionExtensions.AddDiscoveryClient(services, config));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddDiscoveryClient_WithConsulConfiguration_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = @"
                {
                    ""spring"": {
                        ""application"": {
                            ""name"": ""myName""
                        },
                    },
                    ""consul"": {
                        ""host"": ""foo.bar"",
                        ""discovery"": {
                            ""register"": false,
                            ""deregister"": false,
                            ""instanceid"": ""instanceid"",
                            ""port"": 1234
                        }
                    }
                }";

            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddOptions();
            services.AddDiscoveryClient(config);
            var provider = services.BuildServiceProvider();

            var service = provider.GetService<IDiscoveryClient>();
            Assert.NotNull(service);
            var service1 = provider.GetService<IConsulClient>();
            Assert.NotNull(service1);
            var service2 = provider.GetService<IScheduler>();
            Assert.NotNull(service2);
            var service3 = provider.GetService<IConsulServiceRegistry>();
            Assert.NotNull(service3);
            var service4 = provider.GetService<IConsulRegistration>();
            Assert.NotNull(service4);
            var service5 = provider.GetService<IConsulServiceRegistrar>();
            Assert.NotNull(service5);
            var service6 = provider.GetService<IHealthContributor>();
            Assert.NotNull(service6);
        }

        [Fact]
        public void AddDiscoveryClient_WithConsulInetConfiguration_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "spring:cloud:inet:defaulthostname", "fromtest" },
                { "spring:cloud:inet:skipReverseDnsLookup", "true" },
                { "consul:discovery:useNetUtils", "true" },
                { "consul:discovery:register", "false" },
                { "consul:discovery:deregister", "false" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();
            services.AddDiscoveryClient(config);
            var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IDiscoveryClient>());
            Assert.NotNull(provider.GetService<IConsulClient>());
            Assert.NotNull(provider.GetService<IScheduler>());
            Assert.NotNull(provider.GetService<IConsulServiceRegistry>());
            var reg = provider.GetService<IConsulRegistration>();
            Assert.NotNull(reg);
            Assert.Equal("fromtest", reg.Host);
            Assert.NotNull(provider.GetService<IConsulServiceRegistrar>());
            Assert.NotNull(provider.GetService<IHealthContributor>());
        }

        [Fact]
        public void AddDiscoveryClient_WithKubernetesConfig_AddsDiscoveryClient()
        {
            // arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "spring:cloud:kubernetes:discovery:enabled", "true" },
                { "spring:cloud:kubernetes:namespace", "notdefault" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();

            // act
            var provider = services.AddDiscoveryClient(config).BuildServiceProvider();

            // assert
            var service = provider.GetService<IDiscoveryClient>();
            var options = provider.GetRequiredService<IOptions<KubernetesDiscoveryOptions>>();
            Assert.True(service.GetType().IsAssignableFrom(typeof(KubernetesDiscoveryClient)));
            Assert.Equal("notdefault", options.Value.Namespace);
        }

        [Fact]
        public void AddServiceDiscovery_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection serviceCollection = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryServiceCollectionExtensions.AddServiceDiscovery(serviceCollection, (builder) => { }));
            Assert.Contains(nameof(serviceCollection), ex.Message);
        }

        [Fact]
        public void AddServiceDiscovery_AddsNoOpClientIfBuilderActionNull()
        {
            // Arrange
            var services = new ServiceCollection().AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            // Act
            services.AddServiceDiscovery();
            var client = services.BuildServiceProvider().GetRequiredService<IDiscoveryClient>();
            Assert.NotNull(client);
            Assert.IsType<NoOpDiscoveryClient>(client);
            Assert.Empty(client.Services);
            Assert.Empty(client.GetInstances("any"));
        }

        [Fact]
        public void AddServiceDiscovery_WithConfiguration_AddsAndWorks()
        {
            // arrange
            var appsettings = @"
{
    ""discovery"": {
        ""services"": [
            { ""serviceId"": ""fruitService"", ""host"": ""fruitball"", ""port"": 443, ""isSecure"": true },
            { ""serviceId"": ""fruitService"", ""host"": ""fruitballer"", ""port"": 8081 },
            { ""serviceId"": ""vegetableService"", ""host"": ""vegemite"", ""port"": 443, ""isSecure"": true },
            { ""serviceId"": ""vegetableService"", ""host"": ""carrot"", ""port"": 8081 },
        ]
    }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            var sCollection = new ServiceCollection()
                .AddOptions()
                .AddSingleton<IConfiguration>(
                    new ConfigurationBuilder()
                        .SetBasePath(Path.GetDirectoryName(path))
                        .AddJsonFile(Path.GetFileName(path))
                        .Build());

            var services = sCollection.AddServiceDiscovery(builder => builder.UseConfiguredInstances()).BuildServiceProvider();

            var client = services.GetService<IDiscoveryClient>();
            Assert.NotNull(client);
            Assert.IsType<ConfigurationDiscoveryClient>(client);
            Assert.Contains("fruitService", client.Services);
            Assert.Contains("vegetableService", client.Services);
            Assert.Equal(2, client.GetInstances("fruitService").Count);
            Assert.Equal(2, client.GetInstances("vegetableService").Count);
        }

        [Fact]
        public void AddServiceDiscovery_WithEurekaConfig_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = @"
                {
                    ""spring"": {
                        ""application"": {
                            ""name"": ""myName""
                        },
                    },
                    ""eureka"": {
                        ""client"": {
                            ""shouldFetchRegistry"": false,
                            ""shouldRegisterWithEureka"": false,
                            ""serviceUrl"": ""http://localhost:8761/eureka/""
                        }
                    }
                }";

            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();
            services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
            services.AddServiceDiscovery(builder => builder.UseEureka());

            var service = services.BuildServiceProvider().GetService<IDiscoveryClient>();
            Assert.NotNull(service);
        }

        [Fact]
        public void AddServiceDiscovery_WithEurekaInetConfig_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "spring:cloud:inet:defaulthostname", "fromtest" },
                { "spring:cloud:inet:skipReverseDnsLookup", "true" },
                { "eureka:client:shouldFetchRegistry", "false" },
                { "eureka:client:shouldRegisterWithEureka", "false" },
                { "eureka:instance:useNetUtils", "true" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();
            services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
            services.AddServiceDiscovery(builder => builder.UseEureka());

            var service = services.BuildServiceProvider().GetService<IDiscoveryClient>();
            Assert.NotNull(service);
            var instanceInfo = service.GetLocalServiceInstance();
            Assert.Equal("fromtest", instanceInfo.Host);
        }

        [Fact]
        public void AddServiceDiscovery_WithEurekaClientCertConfig_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>()
            {
                { "spring:application:name", "myName" },
                { "eureka:client:serviceUrl", "http://localhost:8761/eureka/" },
                { "eureka:client:shouldFetchRegistry", "false" },
                { "eureka:client:shouldRegisterWithEureka", "false" }
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(appsettings)
                .AddPemFiles("instance.crt", "instance.key")
                .Build();

            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();
            services.Configure<CertificateOptions>(config);
            services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
            services.AddServiceDiscovery(builder => builder.UseEureka());

            // act
            var serviceProvider = services.BuildServiceProvider();
            var discoveryClient = serviceProvider.GetService<IDiscoveryClient>();
            var handlerProvider = serviceProvider.GetService<IHttpClientHandlerProvider>();

            // assert
            Assert.NotNull(discoveryClient);
            Assert.NotNull(handlerProvider);
        }

        [Fact]
        public void AddServiceDiscovery_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => DiscoveryServiceCollectionExtensions.AddServiceDiscovery(services, builder => builder.UseEureka("foobar")));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddServiceDiscovery_MultipleRegistryServices_ThrowsConnectorException()
        {
            // Arrange
            var env1 = @"
                {
                    ""limits"": {
                    ""fds"": 16384,
                    ""mem"": 1024,
                    ""disk"": 1024
                    },
                    ""application_name"": ""spring-cloud-broker"",
                    ""application_uris"": [
                    ""spring-cloud-broker.apps.testcloud.com""
                    ],
                    ""name"": ""spring-cloud-broker"",
                    ""space_name"": ""p-spring-cloud-services"",
                    ""space_id"": ""65b73473-94cc-4640-b462-7ad52838b4ae"",
                    ""uris"": [
                    ""spring-cloud-broker.apps.testcloud.com""
                    ],
                    ""users"": null,
                    ""version"": ""07e112f7-2f71-4f5a-8a34-db51dbed30a3"",
                    ""application_version"": ""07e112f7-2f71-4f5a-8a34-db51dbed30a3"",
                    ""application_id"": ""798c2495-fe75-49b1-88da-b81197f2bf06""
                }";
            var env2 = @"
                {
                    ""p-service-registry"": [
                    {
                        ""credentials"": {
                            ""uri"": ""https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com"",
                            ""client_id"": ""p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe"",
                            ""client_secret"": ""dCsdoiuklicS"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-service-registry"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myDiscoveryService"",
                        ""tags"": [
                            ""eureka"",
                            ""discovery"",
                            ""registry"",
                            ""spring-cloud""
                        ]
                    },
                    {
                        ""credentials"": {
                            ""uri"": ""https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com"",
                            ""client_id"": ""p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe"",
                            ""client_secret"": ""dCsdoiuklicS"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-service-registry"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myDiscoveryService2"",
                        ""tags"": [
                            ""eureka"",
                            ""discovery"",
                            ""registry"",
                            ""spring-cloud""
                        ]
                    }]
                }";

            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", env1);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddCloudFoundry().Build());

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => DiscoveryServiceCollectionExtensions.AddServiceDiscovery(services, (options) => options.UseEureka()));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddServiceDiscovery_WithConsulConfiguration_AddsDiscoveryClient()
        {
            // Arrange
            var appSettings = new Dictionary<string, string>()
            {
                { "spring:application:name", "myName" },
                { "consul:host", "foo.bar" },
                { "consul:discovery:register", "false" },
                { "consul:discovery:deregister", "false" },
                { "consul:discovery:instanceid", "instanceid" },
                { "consul:discovery:port", "1234" },
            };

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
            services.AddOptions();
            services.AddServiceDiscovery(builder => builder.UseConsul());
            var provider = services.BuildServiceProvider();

            var service = provider.GetService<IDiscoveryClient>();
            Assert.NotNull(service);
            var service1 = provider.GetService<IConsulClient>();
            Assert.NotNull(service1);
            var service2 = provider.GetService<IScheduler>();
            Assert.NotNull(service2);
            var service3 = provider.GetService<IConsulServiceRegistry>();
            Assert.NotNull(service3);
            var service4 = provider.GetService<IConsulRegistration>();
            Assert.NotNull(service4);
            var service5 = provider.GetService<IConsulServiceRegistrar>();
            Assert.NotNull(service5);
            var service6 = provider.GetService<IHealthContributor>();
            Assert.NotNull(service6);
        }

        [Fact]
        public void AddServiceDiscovery_WithConsulInetConfiguration_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "spring:cloud:inet:defaulthostname", "fromtest" },
                { "spring:cloud:inet:skipReverseDnsLookup", "true" },
                { "consul:discovery:useNetUtils", "true" },
                { "consul:discovery:register", "false" },
                { "consul:discovery:deregister", "false" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();
            services.AddServiceDiscovery(builder => builder.UseConsul());
            var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IDiscoveryClient>());
            Assert.NotNull(provider.GetService<IConsulClient>());
            Assert.NotNull(provider.GetService<IScheduler>());
            Assert.NotNull(provider.GetService<IConsulServiceRegistry>());
            var reg = provider.GetService<IConsulRegistration>();
            Assert.NotNull(reg);
            Assert.Equal("fromtest", reg.Host);
            Assert.NotNull(provider.GetService<IConsulServiceRegistrar>());
            Assert.NotNull(provider.GetService<IHealthContributor>());
        }

        [Fact]
        public void AddDiscoveryClient_WithConsulUrlConfiguration_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233" },
                { "consul:discovery:register", "false" },
                { "consul:discovery:deregister", "false" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();
            services.AddDiscoveryClient(config);
            var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IDiscoveryClient>());
            Assert.NotNull(provider.GetService<IConsulClient>());
            Assert.NotNull(provider.GetService<IScheduler>());
            Assert.NotNull(provider.GetService<IConsulServiceRegistry>());
            var reg = provider.GetService<IConsulRegistration>();
            Assert.NotNull(reg);
            Assert.Equal("myapp", reg.Host);
            Assert.Equal(1234, reg.Port);
            Assert.NotNull(provider.GetService<IConsulServiceRegistrar>());
            Assert.NotNull(provider.GetService<IHealthContributor>());
        }

        [Fact]
        public void AddDiscoveryClient_WithConsul_UrlBypassWorks()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233" },
                { "consul:discovery:register", "false" },
                { "consul:discovery:deregister", "false" },
                { "Consul:Discovery:UseAspNetCoreUrls", "false" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

            var provider = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions().AddDiscoveryClient(config).BuildServiceProvider();
            var reg = provider.GetService<IConsulRegistration>();

            Assert.NotNull(reg);
            Assert.NotEqual("myapp", reg.Host);
            Assert.Equal(0, reg.Port);
            Assert.NotNull(provider.GetService<IConsulServiceRegistrar>());
            Assert.NotNull(provider.GetService<IHealthContributor>());
        }

        [Fact]
        public void AddDiscoveryClient_WithConsul_PreferPortOverUrl()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233" },
                { "consul:discovery:register", "false" },
                { "consul:discovery:deregister", "false" },
                { "Consul:Discovery:Port", "8080" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

            var provider = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions().AddDiscoveryClient(config).BuildServiceProvider();
            var reg = provider.GetService<IConsulRegistration>();

            Assert.NotNull(reg);
            Assert.NotEqual("myapp", reg.Host);
            Assert.Equal(8080, reg.Port);
            Assert.NotNull(provider.GetService<IConsulServiceRegistrar>());
            Assert.NotNull(provider.GetService<IHealthContributor>());
        }

        [Fact]
        public void AddServiceDiscovery_WithMultipleConfiguredClients_NotAllowed()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "consul:discovery:cachettl", "1" }, { "eureka:client:cachettl", "1" } }).Build());

            // act
            var exception = Assert.Throws<AmbiguousMatchException>(() => serviceCollection.AddServiceDiscovery(builder =>
                {
                    builder.UseConsul();
                    builder.UseEureka();
                }));

            Assert.Contains("Multiple IDiscoveryClient implementations have been registered", exception.Message);
        }

        [Fact]
        public void AddServiceDiscovery_WithMultipleNotConfiguredClients_NotAllowed()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            // act
            var exception = Assert.Throws<AmbiguousMatchException>(() => serviceCollection.AddServiceDiscovery(builder =>
            {
                builder.UseConsul();
                builder.UseEureka();
            }));

            Assert.Contains("Multiple IDiscoveryClient implementations have been registered", exception.Message);
        }

        [Fact]
        public void AddServiceDiscovery_WithMultipleClients_PicksConfigured()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "eureka:client:cachettl", "1" } }).Build());

            // act
            var provider = serviceCollection.AddServiceDiscovery(builder =>
            {
                builder.UseConsul();
                builder.UseEureka();
            }).BuildServiceProvider();

            // assert
            var service = provider.GetService<IDiscoveryClient>();
            Assert.True(service.GetType().IsAssignableFrom(typeof(EurekaDiscoveryClient)));
        }

        [Fact]
        public void AddServiceDiscovery_WithKubernetesConfig_AddsDiscoveryClient()
        {
            // arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "spring:cloud:kubernetes:discovery:enabled", "true" },
                { "spring:cloud:kubernetes:namespace", "notdefault" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection().AddSingleton<IConfiguration>(config).AddOptions();

            // act
            var provider = services.AddServiceDiscovery(builder => builder.UseKubernetes()).BuildServiceProvider();

            // assert
            var service = provider.GetService<IDiscoveryClient>();
            var options = provider.GetRequiredService<IOptions<KubernetesDiscoveryOptions>>();
            Assert.True(service.GetType().IsAssignableFrom(typeof(KubernetesDiscoveryClient)));
            Assert.Equal("notdefault", options.Value.Namespace);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        internal class TestClientHandlerProvider : IHttpClientHandlerProvider
        {
            public bool Called { get; set; } = false;

            public HttpClientHandler GetHttpClientHandler()
            {
                Called = true;
                return new HttpClientHandler();
            }
        }
    }
}
