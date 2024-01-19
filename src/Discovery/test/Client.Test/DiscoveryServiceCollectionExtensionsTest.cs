// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.CloudFoundry;
using Steeltoe.Discovery.Client.SimpleClients;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Eureka.Transport;
using Xunit;

namespace Steeltoe.Discovery.Client.Test;

public sealed class DiscoveryServiceCollectionExtensionsTest
{
    private static readonly Dictionary<string, string> FastEureka = new()
    {
        { "eureka:client:ShouldRegisterWithEureka", "false" },
        { "eureka:client:ShouldFetchRegistry", "false" }
    };

    [Fact]
    public void AddDiscoveryClient_WithEurekaConfig_AddsDiscoveryClient()
    {
        const string appsettings = @"
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

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        IConfiguration configuration = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection().AddSingleton(configuration).AddOptions();
        services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
        services.AddDiscoveryClient(configuration);

        var client = services.BuildServiceProvider(true).GetService<IDiscoveryClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddDiscoveryClient_WithEurekaInetConfig_AddsDiscoveryClient()
    {
        var appsettings = new Dictionary<string, string>(FastEureka)
        {
            { "spring:application:name", "myName" },
            { "spring:cloud:inet:defaulthostname", "fromtest" },
            { "spring:cloud:inet:skipReverseDnsLookup", "true" },
            { "eureka:instance:useNetUtils", "true" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
        IServiceCollection services = new ServiceCollection().AddSingleton(configuration).AddOptions();
        services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
        services.AddDiscoveryClient(configuration);

        var client = services.BuildServiceProvider(true).GetService<IDiscoveryClient>();
        Assert.NotNull(client);
        IServiceInstance instanceInfo = client.GetLocalServiceInstance();
        Assert.Equal("fromtest", instanceInfo.Host);
    }

    [Fact]
    public void AddDiscoveryClient_WithEurekaClientCertConfig_AddsDiscoveryClient()
    {
        var appsettings = new Dictionary<string, string>(FastEureka);

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).AddPemFiles("instance.crt", "instance.key").Build();

        IServiceCollection services = new ServiceCollection().AddSingleton(configuration).AddOptions();
        services.AddSingleton<IConfigureOptions<CertificateOptions>, PemConfigureCertificateOptions>();
        services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
        services.AddDiscoveryClient(configuration);

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var discoveryClient = (EurekaDiscoveryClient)serviceProvider.GetService<IDiscoveryClient>();
        var eurekaHttpClient = discoveryClient.HttpClient;

        var httpClient = (HttpClient)eurekaHttpClient.GetType().GetRuntimeFields().FirstOrDefault(n => n.Name == "httpClient").GetValue(eurekaHttpClient);

        var handler = httpClient.GetType().BaseType.GetRuntimeFields().FirstOrDefault(f => f.Name == "_handler").GetValue(httpClient) as DelegatingHandler;
        object innerHandler = GetInnerHttpHandler(handler);

        Assert.NotNull(discoveryClient);
        Assert.IsType<ClientCertificateHttpHandler>(innerHandler);
    }

    [Fact]
    public async Task AddDiscoveryClient_WithNoConfig_AddsNoOpDiscoveryClient()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "spring:application:name", "myName" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
        IServiceCollection services = new ServiceCollection().AddSingleton(configuration);

        services.AddDiscoveryClient(configuration);
        var client = services.BuildServiceProvider(true).GetRequiredService<IDiscoveryClient>();

        Assert.NotNull(client);
        Assert.IsType<NoOpDiscoveryClient>(client);
        Assert.Empty(await client.GetServiceIdsAsync(CancellationToken.None));
        Assert.Empty(await client.GetInstancesAsync("any", CancellationToken.None));
    }

    [Fact]
    public void AddDiscoveryClient_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddDiscoveryClient(configuration, "foobar"));
        Assert.Contains("foobar", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDiscoveryClient_MultipleRegistryServices_ThrowsConnectorException()
    {
        const string env1 = @"
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

        const string env2 = @"
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

        IServiceCollection services = new ServiceCollection();

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", env1);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", env2);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfiguration configuration = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddDiscoveryClient(configuration));
        Assert.Contains("Multiple", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDiscoveryClient_WithConsulConfiguration_AddsDiscoveryClient()
    {
        const string appsettings = @"
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

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddOptions();
        services.AddDiscoveryClient(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var service = serviceProvider.GetService<IDiscoveryClient>();
        Assert.NotNull(service);
        var service1 = serviceProvider.GetService<IConsulClient>();
        Assert.NotNull(service1);
        var service2 = serviceProvider.GetService<TtlScheduler>();
        Assert.NotNull(service2);
        var service3 = serviceProvider.GetService<ConsulServiceRegistry>();
        Assert.NotNull(service3);
        var service4 = serviceProvider.GetService<ConsulRegistration>();
        Assert.NotNull(service4);
        var service5 = serviceProvider.GetService<ConsulServiceRegistrar>();
        Assert.NotNull(service5);
        var service6 = serviceProvider.GetService<IHealthContributor>();
        Assert.NotNull(service6);
    }

    [Fact]
    public void AddDiscoveryClient_WithConsulInetConfiguration_AddsDiscoveryClient()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "spring:application:name", "myName" },
            { "spring:cloud:inet:defaulthostname", "fromtest" },
            { "spring:cloud:inet:skipReverseDnsLookup", "true" },
            { "consul:discovery:useNetUtils", "true" },
            { "consul:discovery:register", "false" },
            { "consul:discovery:deregister", "false" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        IServiceCollection services = new ServiceCollection().AddSingleton(configuration).AddOptions();
        services.AddDiscoveryClient(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Assert.NotNull(serviceProvider.GetService<IDiscoveryClient>());
        Assert.NotNull(serviceProvider.GetService<IConsulClient>());
        Assert.NotNull(serviceProvider.GetService<TtlScheduler>());
        Assert.NotNull(serviceProvider.GetService<ConsulServiceRegistry>());
        var reg = serviceProvider.GetService<ConsulRegistration>();
        Assert.NotNull(reg);
        Assert.Equal("fromtest", reg.Host);
        Assert.NotNull(serviceProvider.GetService<ConsulServiceRegistrar>());
        Assert.NotNull(serviceProvider.GetService<IHealthContributor>());
    }

    [Fact]
    public void AddServiceDiscovery_ThrowsIfServiceCollectionNull()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        const IServiceCollection services = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddServiceDiscovery(configuration, _ =>
        {
        }));

        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddServiceDiscovery_AddsNoOpClientIfBuilderActionNull()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        IServiceCollection services = new ServiceCollection().AddSingleton(configuration);

        services.AddServiceDiscovery(configuration);
        var client = services.BuildServiceProvider(true).GetRequiredService<IDiscoveryClient>();
        Assert.NotNull(client);
        Assert.IsType<NoOpDiscoveryClient>(client);
        Assert.Empty(await client.GetServiceIdsAsync(CancellationToken.None));
        Assert.Empty(await client.GetInstancesAsync("any", CancellationToken.None));
    }

    [Fact]
    public async Task AddServiceDiscovery_WithConfiguration_AddsAndWorks()
    {
        const string appsettings = @"
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

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);

        IConfiguration configuration = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile(Path.GetFileName(path)).Build();
        IServiceCollection services = new ServiceCollection().AddOptions().AddSingleton(configuration);

        ServiceProvider serviceProvider = services.AddServiceDiscovery(configuration, builder => builder.UseConfiguredInstances()).BuildServiceProvider(true);

        var client = serviceProvider.GetService<IDiscoveryClient>();
        Assert.NotNull(client);
        Assert.IsType<ConfigurationDiscoveryClient>(client);
        Assert.Contains("fruitService", await client.GetServiceIdsAsync(CancellationToken.None));
        Assert.Contains("vegetableService", await client.GetServiceIdsAsync(CancellationToken.None));

        IList<IServiceInstance> fruitInstances = await client.GetInstancesAsync("fruitService", CancellationToken.None);
        Assert.Equal(2, fruitInstances.Count);

        IList<IServiceInstance> vegetableInstances = await client.GetInstancesAsync("vegetableService", CancellationToken.None);
        Assert.Equal(2, vegetableInstances.Count);
    }

    [Fact]
    public void AddServiceDiscovery_WithEurekaConfig_AddsDiscoveryClient()
    {
        const string appsettings = @"
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

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        IConfiguration configuration = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection().AddSingleton(configuration).AddOptions();
        services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
        services.AddServiceDiscovery(configuration, builder => builder.UseEureka());

        var client = services.BuildServiceProvider(true).GetService<IDiscoveryClient>();
        Assert.NotNull(client);
    }

    [Fact]
    public void AddServiceDiscovery_WithEurekaInetConfig_AddsDiscoveryClient()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "spring:application:name", "myName" },
            { "spring:cloud:inet:defaulthostname", "fromtest" },
            { "spring:cloud:inet:skipReverseDnsLookup", "true" },
            { "eureka:client:shouldFetchRegistry", "false" },
            { "eureka:client:shouldRegisterWithEureka", "false" },
            { "eureka:instance:useNetUtils", "true" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
        IServiceCollection services = new ServiceCollection().AddSingleton(configuration).AddOptions();
        services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
        services.AddServiceDiscovery(configuration, builder => builder.UseEureka());

        var client = services.BuildServiceProvider(true).GetService<IDiscoveryClient>();
        Assert.NotNull(client);
        IServiceInstance instanceInfo = client.GetLocalServiceInstance();
        Assert.Equal("fromtest", instanceInfo.Host);
    }

    [Fact]
    public void AddServiceDiscovery_WithEurekaClientCertConfig_AddsDiscoveryClient()
    {
        var appsettings = new Dictionary<string, string>(FastEureka);

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).AddPemFiles("instance.crt", "instance.key").Build();

        IServiceCollection services = new ServiceCollection().AddSingleton(configuration).AddOptions();
        services.AddSingleton<IConfigureOptions<CertificateOptions>, PemConfigureCertificateOptions>();
        services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
        services.AddServiceDiscovery(configuration, builder => builder.UseEureka());

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var discoveryClient = (EurekaDiscoveryClient)serviceProvider.GetService<IDiscoveryClient>();
        var eurekaHttpClient = discoveryClient.HttpClient;

        var httpClient = (HttpClient)eurekaHttpClient.GetType().GetRuntimeFields().FirstOrDefault(n => n.Name == "httpClient").GetValue(eurekaHttpClient);

        var handler = httpClient.GetType().BaseType.GetRuntimeFields().FirstOrDefault(f => f.Name == "_handler").GetValue(httpClient) as DelegatingHandler;
        object innerHandler = GetInnerHttpHandler(handler);

        Assert.NotNull(discoveryClient);
        Assert.IsType<ClientCertificateHttpHandler>(innerHandler);
    }

    [Fact]
    public void AddServiceDiscovery_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        services.AddSingleton(configuration);

        services.AddServiceDiscovery(configuration, builder => builder.UseEureka("foobar"));
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var ex = Assert.Throws<ConnectorException>(() => serviceProvider.GetService<IDiscoveryClient>());
        Assert.Contains("foobar", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddServiceDiscovery_MultipleRegistryServices_ThrowsConnectorException()
    {
        const string env1 = @"
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

        const string env2 = @"
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

        IServiceCollection services = new ServiceCollection();

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", env1);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", env2);

        IConfiguration configuration = new ConfigurationBuilder().AddCloudFoundry().Build();
        services.AddSingleton(configuration);

        services.AddServiceDiscovery(configuration, options => options.UseEureka());
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var ex = Assert.Throws<ConnectorException>(() => serviceProvider.GetService<IDiscoveryClient>());
        Assert.Contains("Multiple", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddServiceDiscovery_WithConsulConfiguration_AddsDiscoveryClient()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "spring:application:name", "myName" },
            { "consul:host", "foo.bar" },
            { "consul:discovery:register", "false" },
            { "consul:discovery:deregister", "false" },
            { "consul:discovery:instanceid", "instanceid" },
            { "consul:discovery:port", "1234" }
        };

        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        services.AddSingleton(configuration);
        services.AddOptions();
        services.AddServiceDiscovery(configuration, builder => builder.UseConsul());
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var service = serviceProvider.GetService<IDiscoveryClient>();
        Assert.NotNull(service);
        var service1 = serviceProvider.GetService<IConsulClient>();
        Assert.NotNull(service1);
        var service2 = serviceProvider.GetService<TtlScheduler>();
        Assert.NotNull(service2);
        var service3 = serviceProvider.GetService<ConsulServiceRegistry>();
        Assert.NotNull(service3);
        var service4 = serviceProvider.GetService<ConsulRegistration>();
        Assert.NotNull(service4);
        var service5 = serviceProvider.GetService<ConsulServiceRegistrar>();
        Assert.NotNull(service5);
        var service6 = serviceProvider.GetService<IHealthContributor>();
        Assert.NotNull(service6);
    }

    [Fact]
    public void AddServiceDiscovery_WithConsulInetConfiguration_AddsDiscoveryClient()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "spring:application:name", "myName" },
            { "spring:cloud:inet:defaulthostname", "fromtest" },
            { "spring:cloud:inet:skipReverseDnsLookup", "true" },
            { "consul:discovery:useNetUtils", "true" },
            { "consul:discovery:register", "false" },
            { "consul:discovery:deregister", "false" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        IServiceCollection services = new ServiceCollection().AddSingleton(configuration).AddOptions();
        services.AddServiceDiscovery(configuration, builder => builder.UseConsul());
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Assert.NotNull(serviceProvider.GetService<IDiscoveryClient>());
        Assert.NotNull(serviceProvider.GetService<IConsulClient>());
        Assert.NotNull(serviceProvider.GetService<TtlScheduler>());
        Assert.NotNull(serviceProvider.GetService<ConsulServiceRegistry>());
        var reg = serviceProvider.GetService<ConsulRegistration>();
        Assert.NotNull(reg);
        Assert.Equal("fromtest", reg.Host);
        Assert.NotNull(serviceProvider.GetService<ConsulServiceRegistrar>());
        Assert.NotNull(serviceProvider.GetService<IHealthContributor>());
    }

    [Fact]
    public void AddDiscoveryClient_WithConsulUrlConfiguration_AddsDiscoveryClient()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "spring:application:name", "myName" },
            { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233" },
            { "consul:discovery:register", "false" },
            { "consul:discovery:deregister", "false" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        IServiceCollection services = new ServiceCollection().AddSingleton(configuration).AddOptions();
        services.AddDiscoveryClient(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Assert.NotNull(serviceProvider.GetService<IDiscoveryClient>());
        Assert.NotNull(serviceProvider.GetService<IConsulClient>());
        Assert.NotNull(serviceProvider.GetService<TtlScheduler>());
        Assert.NotNull(serviceProvider.GetService<ConsulServiceRegistry>());
        var reg = serviceProvider.GetService<ConsulRegistration>();
        Assert.NotNull(reg);
        Assert.Equal("myapp", reg.Host);
        Assert.Equal(1234, reg.Port);
        Assert.NotNull(serviceProvider.GetService<ConsulServiceRegistrar>());
        Assert.NotNull(serviceProvider.GetService<IHealthContributor>());
    }

    [Fact]
    public void AddDiscoveryClient_WithConsul_UrlBypassWorks()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "spring:application:name", "myName" },
            { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233" },
            { "consul:discovery:register", "false" },
            { "consul:discovery:deregister", "false" },
            { "Consul:Discovery:UseAspNetCoreUrls", "false" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(configuration).AddOptions().AddDiscoveryClient(configuration)
            .BuildServiceProvider(true);

        var reg = serviceProvider.GetService<ConsulRegistration>();

        Assert.NotNull(reg);
        Assert.NotEqual("myapp", reg.Host);
        Assert.Equal(0, reg.Port);
        Assert.NotNull(serviceProvider.GetService<ConsulServiceRegistrar>());
        Assert.NotNull(serviceProvider.GetService<IHealthContributor>());
    }

    [Fact]
    public void AddDiscoveryClient_WithConsul_PreferPortOverUrl()
    {
        var appsettings = new Dictionary<string, string>
        {
            { "spring:application:name", "myName" },
            { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233" },
            { "consul:discovery:register", "false" },
            { "consul:discovery:deregister", "false" },
            { "Consul:Discovery:Port", "8080" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(configuration).AddOptions().AddDiscoveryClient(configuration)
            .BuildServiceProvider(true);

        var reg = serviceProvider.GetService<ConsulRegistration>();

        Assert.NotNull(reg);
        Assert.NotEqual("myapp", reg.Host);
        Assert.Equal(8080, reg.Port);
        Assert.NotNull(serviceProvider.GetService<ConsulServiceRegistrar>());
        Assert.NotNull(serviceProvider.GetService<IHealthContributor>());
    }

    [Fact]
    public void AddServiceDiscovery_WithMultipleConfiguredClients_NotAllowed()
    {
        var serviceCollection = new ServiceCollection();

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "consul:discovery:cachettl", "1" },
            { "eureka:client:cachettl", "1" }
        }).Build();

        serviceCollection.AddSingleton(configuration);

        var exception = Assert.Throws<AmbiguousMatchException>(() => serviceCollection.AddServiceDiscovery(configuration, builder =>
        {
            builder.UseConsul();
            builder.UseEureka();
        }));

        Assert.Contains("Multiple IDiscoveryClient implementations have been registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddServiceDiscovery_WithMultipleNotConfiguredClients_NotAllowed()
    {
        var serviceCollection = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        serviceCollection.AddSingleton(configuration);

        var exception = Assert.Throws<AmbiguousMatchException>(() => serviceCollection.AddServiceDiscovery(configuration, builder =>
        {
            builder.UseConsul();
            builder.UseEureka();
        }));

        Assert.Contains("Multiple IDiscoveryClient implementations have been registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddServiceDiscovery_WithMultipleClients_PicksConfigured()
    {
        var serviceCollection = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(FastEureka).Build();
        serviceCollection.AddSingleton(configuration);

        ServiceProvider serviceProvider = serviceCollection.AddServiceDiscovery(configuration, builder =>
        {
            builder.UseConsul();
            builder.UseEureka();
        }).BuildServiceProvider(true);

        var service = serviceProvider.GetService<IDiscoveryClient>();
        Assert.True(service.GetType().IsAssignableFrom(typeof(EurekaDiscoveryClient)));
    }

    private object GetInnerHttpHandler(object handler)
    {
        while (handler is not null)
        {
            handler = handler.GetType().GetProperty("InnerHandler")?.GetValue(handler);

            if (handler is HttpClientHandler)
            {
                break;
            }
        }

        return handler;
    }
}
