// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Consul.Registry;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.HttpClients.Test;

public sealed class RegisterMultipleDiscoveryClientsTest
{
    private static readonly Dictionary<string, string?> FastDiscovery = new()
    {
        ["eureka:client:ShouldRegisterWithEureka"] = "false",
        ["eureka:client:ShouldFetchRegistry"] = "false",
        ["Consul:Discovery:Register"] = "false"
    };

    [Fact]
    public async Task WithEurekaConfiguration_AddsDiscoveryClient()
    {
        const string appSettings = """
            {
                "spring": {
                    "application": {
                        "name": "myName"
                    },
                },
                "eureka": {
                    "client": {
                        "shouldFetchRegistry": false,
                        "shouldRegisterWithEureka": false,
                        "serviceUrl": "http://localhost:8761/eureka/"
                    }
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        IConfiguration configuration = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddOptions();
        services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];

        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);
    }

    [Fact]
    public async Task WithEurekaInetConfiguration_AddsDiscoveryClient()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "myName",
            ["spring:cloud:inet:defaultHostname"] = "from-test",
            ["spring:cloud:inet:skipReverseDnsLookup"] = "true",
            ["eureka:client:shouldFetchRegistry"] = "false",
            ["eureka:client:shouldRegisterWithEureka"] = "false",
            ["eureka:instance:UseNetworkInterfaces"] = "true"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton<IHostApplicationLifetime>(new TestApplicationLifetime());
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];

        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);

        IServiceInstance? serviceInstance = discoveryClients[0].GetLocalServiceInstance();
        Assert.NotNull(serviceInstance);
        Assert.Equal("from-test", serviceInstance.Host);
    }

    [Fact]
    public async Task WithEurekaClientCertificateConfiguration_AddsDiscoveryClient()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Certificates:Eureka:CertificateFilePath"] = "instance.crt",
            ["Certificates:Eureka:PrivateKeyFilePath"] = "instance.key"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);

        builder.Services.AddOptions();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond("application/json", "{}");

        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();
        _ = await discoveryClient.FetchFullRegistryAsync(CancellationToken.None);

        Assert.NotNull(handler.ClientCertificates);
        Assert.NotEmpty(handler.ClientCertificates);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task WithGlobalCertificateConfiguration_AddsDiscoveryClient()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Certificates:CertificateFilePath"] = "instance.crt",
            ["Certificates:PrivateKeyFilePath"] = "instance.key"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);

        builder.Services.AddOptions();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8761/eureka/apps").Respond("application/json", "{}");

        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();
        _ = await discoveryClient.FetchFullRegistryAsync(CancellationToken.None);

        Assert.NotNull(handler.ClientCertificates);
        Assert.NotEmpty(handler.ClientCertificates);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SingleEurekaVCAP_AddsEurekaDiscoveryClient()
    {
        const string vcapApplication = """
            {
                "limits": {
                    "fds": 16384,
                    "mem": 1024,
                    "disk": 1024
                },
                "application_name": "spring-cloud-broker",
                "application_uris": [
                  "spring-cloud-broker.apps.test-cloud.com"
                ],
                "name": "spring-cloud-broker",
                "space_name": "p-spring-cloud-services",
                "space_id": "65b73473-94cc-4640-b462-7ad52838b4ae",
                "uris": [
                    "spring-cloud-broker.apps.test-cloud.com"
                ],
                "users": null,
                "version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_id": "798c2495-fe75-49b1-88da-b81197f2bf06"
            }
            """;

        const string vcapServices = """
            {
                "p-service-registry": [
                {
                    "credentials": {
                        "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com",
                        "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                        "client_secret": "dCsdoiuklicS",
                        "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                    },
                    "syslog_drain_url": null,
                    "label": "p-service-registry",
                    "provider": null,
                    "plan": "standard",
                    "name": "myDiscoveryService",
                    "tags": [
                        "eureka",
                        "discovery",
                        "registry",
                        "spring-cloud"
                    ]
                }]
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);

        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:client:enabled"] = "false"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        builder.AddCloudFoundry();
        builder.AddCloudFoundryServiceBindings();
        IConfiguration configuration = builder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];

        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);

        var clientOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();
        EurekaClientOptions clientOptions = clientOptionsMonitor.CurrentValue;

        clientOptions.EurekaServerServiceUrls.Should().Be("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com/eureka/");
        clientOptions.ClientId.Should().Be("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe");
        clientOptions.ClientSecret.Should().Be("dCsdoiuklicS");
        clientOptions.AccessTokenUri.Should().Be("https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token");
        clientOptions.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleEurekaVCAPs_AddsEurekaDiscoveryClientForFirstEntry()
    {
        const string vcapApplication = """
            {
                "limits": {
                    "fds": 16384,
                    "mem": 1024,
                    "disk": 1024
                },
                "application_name": "spring-cloud-broker",
                "application_uris": [
                  "spring-cloud-broker.apps.test-cloud.com"
                ],
                "name": "spring-cloud-broker",
                "space_name": "p-spring-cloud-services",
                "space_id": "65b73473-94cc-4640-b462-7ad52838b4ae",
                "uris": [
                    "spring-cloud-broker.apps.test-cloud.com"
                ],
                "users": null,
                "version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_id": "798c2495-fe75-49b1-88da-b81197f2bf06"
            }
            """;

        const string vcapServices = """
            {
                "p-service-registry": [
                {
                    "credentials": {
                        "uri": "https://eureka1.apps.test-cloud.com"
                    },
                    "syslog_drain_url": null,
                    "label": "p-service-registry",
                    "provider": null,
                    "plan": "standard",
                    "name": "myDiscoveryService1",
                    "tags": [
                        "eureka",
                        "discovery",
                        "registry",
                        "spring-cloud"
                    ]
                },
                {
                    "credentials": {
                        "uri": "https://eureka2.apps.test-cloud.com"
                    },
                    "syslog_drain_url": null,
                    "label": "p-service-registry",
                    "provider": null,
                    "plan": "standard",
                    "name": "myDiscoveryService2",
                    "tags": [
                        "eureka",
                        "discovery",
                        "registry",
                        "spring-cloud"
                    ]
                }]
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);

        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:client:enabled"] = "false"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        builder.AddCloudFoundry();
        builder.AddCloudFoundryServiceBindings();
        IConfiguration configuration = builder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];

        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);

        var clientOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();
        EurekaClientOptions clientOptions = clientOptionsMonitor.CurrentValue;

        clientOptions.EurekaServerServiceUrls.Should().Be("https://eureka1.apps.test-cloud.com/eureka/");
        clientOptions.Enabled.Should().BeTrue();
    }

    [Fact]
    public void MultipleEurekaVCAPs_LogsWarning()
    {
        const string env1 = """
            {
                "limits": {
                    "fds": 16384,
                    "mem": 1024,
                    "disk": 1024
                },
                "application_name": "spring-cloud-broker",
                "application_uris": [
                    "spring-cloud-broker.apps.test-cloud.com"
                ],
                "name": "spring-cloud-broker",
                "space_name": "p-spring-cloud-services",
                "space_id": "65b73473-94cc-4640-b462-7ad52838b4ae",
                "uris": [
                    "spring-cloud-broker.apps.test-cloud.com"
                ],
                "users": null,
                "version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_id": "798c2495-fe75-49b1-88da-b81197f2bf06"
            }
            """;

        const string env2 = """
            {
                "p-service-registry": [
                {
                    "credentials": {
                        "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com",
                        "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                        "client_secret": "dCsdoiuklicS",
                        "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                    },
                    "syslog_drain_url": null,
                    "label": "p-service-registry",
                    "provider": null,
                    "plan": "standard",
                    "name": "myDiscoveryService",
                    "tags": [
                        "eureka",
                        "discovery",
                        "registry",
                        "spring-cloud"
                    ]
                },
                {
                    "credentials": {
                        "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com",
                        "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                        "client_secret": "dCsdoiuklicS",
                        "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                    },
                    "syslog_drain_url": null,
                    "label": "p-service-registry",
                    "provider": null,
                    "plan": "standard",
                    "name": "myDiscoveryService2",
                    "tags": [
                        "eureka",
                        "discovery",
                        "registry",
                        "spring-cloud"
                    ]
                }]
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", env1);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", env2);

        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Steeltoe.", StringComparison.Ordinal));
        using var loggerFactory = new LoggerFactory([capturingLoggerProvider]);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCloudFoundryServiceBindings(_ => false, new EnvironmentServiceBindingsReader(), loggerFactory);
        _ = configurationBuilder.Build();

        IList<string> logMessages = capturingLoggerProvider.GetAll();

        logMessages.Should().BeEquivalentTo(
            $"WARN {typeof(EurekaCloudFoundryPostProcessor).FullName}: Multiple Eureka service bindings found, which is not supported. Using the first binding from VCAP_SERVICES.");
    }

    [Fact]
    public async Task EurekaWithAccessTokenUri_SendsAuthTokenRequestFirst()
    {
        const string vcapApplication = """
            {
                "limits": {
                    "fds": 16384,
                    "mem": 1024,
                    "disk": 1024
                },
                "application_name": "spring-cloud-broker",
                "application_uris": [
                  "spring-cloud-broker.apps.test-cloud.com"
                ],
                "name": "spring-cloud-broker",
                "space_name": "p-spring-cloud-services",
                "space_id": "65b73473-94cc-4640-b462-7ad52838b4ae",
                "uris": [
                    "spring-cloud-broker.apps.test-cloud.com"
                ],
                "users": null,
                "version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_id": "798c2495-fe75-49b1-88da-b81197f2bf06"
            }
            """;

        const string vcapServices = """
            {
                "p-service-registry": [
                {
                    "credentials": {
                        "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com",
                        "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                        "client_secret": "dCsdoiuklicS",
                        "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                    },
                    "syslog_drain_url": null,
                    "label": "p-service-registry",
                    "provider": null,
                    "plan": "standard",
                    "name": "myDiscoveryService",
                    "tags": [
                        "eureka",
                        "discovery",
                        "registry",
                        "spring-cloud"
                    ]
                }]
            }
            """;

        const string accessTokenResponse = """
            {
                "access_token": "secret"
            }
            """;

        const string applicationsResponse = """
            {
                "applications": {
                    "versions__delta":"1",
                    "apps__hashcode":"UP_1_",
                    "application":[{
                        "name":"FOO",
                        "instance":[{
                            "instanceId":"localhost:foo",
                            "hostName":"localhost",
                            "app":"FOO",
                            "ipAddr":"192.168.56.1",
                            "status":"UP",
                            "overriddenStatus":"UNKNOWN",
                            "port":{"$":8080,"@enabled":"true"},
                            "securePort":{"$":443,"@enabled":"false"},
                            "countryId":1,
                            "dataCenterInfo":{"@class":"com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo","name":"MyOwn"},
                            "leaseInfo":{"renewalIntervalInSecs":30,"durationInSecs":90,"registrationTimestamp":1457714988223,"lastRenewalTimestamp":1457716158319,"evictionTimestamp":0,"serviceUpTimestamp":1457714988223},
                            "metadata":{"@class":"java.util.Collections$EmptyMap"},
                            "homePageUrl":"http://localhost:8080/",
                            "statusPageUrl":"http://localhost:8080/info",
                            "healthCheckUrl":"http://localhost:8080/health",
                            "vipAddress":"foo",
                            "isCoordinatingDiscoveryServer":"false",
                            "lastUpdatedTimestamp":"1457714988223",
                            "lastDirtyTimestamp":"1457714988172",
                            "actionType":"ADDED"
                        }]
                    }]
                }
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddCloudFoundry();
        builder.Configuration.AddCloudFoundryServiceBindings();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Client:ShouldFetchRegistry"] = "false"
        });

        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token")
            .WithHeaders("Authorization", "Basic cC1zZXJ2aWNlLXJlZ2lzdHJ5LTA2ZTI4ZWZkLTI0YmUtNGNlMy05Nzg0LTg1NGVkOGQyYWNiZTpkQ3Nkb2l1a2xpY1M=")
            .WithFormData("grant_type=client_credentials").Respond("application/json", accessTokenResponse);

        handler.Mock.Expect(HttpMethod.Get, "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com/eureka/apps")
            .WithHeaders("Authorization", "Bearer secret").WithHeaders("X-Discovery-AllowRedirect", "false").Respond("application/json", applicationsResponse);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        ApplicationInfoCollection apps = await discoveryClient.FetchFullRegistryAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.NotNull(apps);
        apps.Should().ContainSingle();
    }

    [Fact]
    public async Task EurekaWithUsernamePasswordInURL_SendsWithAuthHeader()
    {
        const string applicationsResponse = """
            {
                "applications": {
                    "versions__delta":"1",
                    "apps__hashcode":"UP_1_",
                    "application":[{
                        "name":"FOO",
                        "instance":[{
                            "instanceId":"localhost:foo",
                            "hostName":"localhost",
                            "app":"FOO",
                            "ipAddr":"192.168.56.1",
                            "status":"UP",
                            "overriddenStatus":"UNKNOWN",
                            "port":{"$":8080,"@enabled":"true"},
                            "securePort":{"$":443,"@enabled":"false"},
                            "countryId":1,
                            "dataCenterInfo":{"@class":"com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo","name":"MyOwn"},
                            "leaseInfo":{"renewalIntervalInSecs":30,"durationInSecs":90,"registrationTimestamp":1457714988223,"lastRenewalTimestamp":1457716158319,"evictionTimestamp":0,"serviceUpTimestamp":1457714988223},
                            "metadata":{"@class":"java.util.Collections$EmptyMap"},
                            "homePageUrl":"http://localhost:8080/",
                            "statusPageUrl":"http://localhost:8080/info",
                            "healthCheckUrl":"http://localhost:8080/health",
                            "vipAddress":"foo",
                            "isCoordinatingDiscoveryServer":"false",
                            "lastUpdatedTimestamp":"1457714988223",
                            "lastDirtyTimestamp":"1457714988172",
                            "actionType":"ADDED"
                        }]
                    }]
                }
            }
            """;

        string username = WebUtility.UrlEncode("u$er?N@me");
        string password = WebUtility.UrlEncode(":p@ssw0rd=");

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["Eureka:Client:AccessTokenUri"] = "https://api.auth-server.com/get-token",
            ["Eureka:Client:ServiceUrl"] = $"https://{username}:{password}@api.eureka-server.com/eureka"
        });

        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Get, "https://api.eureka-server.com/eureka/apps").WithHeaders("Authorization", "Basic dSRlcj9OQG1lOjpwQHNzdzByZD0=")
            .WithHeaders("X-Discovery-AllowRedirect", "false").Respond("application/json", applicationsResponse);

        await using WebApplication webApplication = builder.Build();
        webApplication.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        var discoveryClient = webApplication.Services.GetRequiredService<EurekaDiscoveryClient>();

        ApplicationInfoCollection apps = await discoveryClient.FetchFullRegistryAsync(CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        Assert.NotNull(apps);
        apps.Should().ContainSingle();
    }

    [Fact]
    public async Task WithConsulConfiguration_AddsDiscoveryClient()
    {
        const string appSettings = """
            {
                "spring": {
                    "application": {
                        "name": "myName"
                    },
                },
                "consul": {
                    "host": "foo.bar",
                    "discovery": {
                        "register": false,
                        "deregister": false,
                        "instanceId": "instance-id",
                        "port": 1234
                    }
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddOptions();
        services.AddConsulDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];

        Assert.Single(discoveryClients);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClients[0]);

        _ = serviceProvider.GetRequiredService<IConsulClient>();
        _ = serviceProvider.GetRequiredService<TtlScheduler>();
        _ = serviceProvider.GetRequiredService<ConsulServiceRegistry>();
        _ = serviceProvider.GetRequiredService<ConsulRegistration>();
        _ = serviceProvider.GetRequiredService<ConsulServiceRegistrar>();
        _ = serviceProvider.GetRequiredService<IHealthContributor>();
    }

    [Fact]
    public async Task WithConsulInetConfiguration_AddsDiscoveryClient()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "myName",
            ["spring:cloud:inet:defaultHostname"] = "from-test",
            ["spring:cloud:inet:skipReverseDnsLookup"] = "true",
            ["consul:discovery:UseNetworkInterfaces"] = "true",
            ["consul:discovery:register"] = "false",
            ["consul:discovery:deregister"] = "false"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddOptions();
        services.AddConsulDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];

        Assert.Single(discoveryClients);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClients[0]);

        _ = serviceProvider.GetRequiredService<IConsulClient>();
        _ = serviceProvider.GetRequiredService<TtlScheduler>();
        _ = serviceProvider.GetRequiredService<ConsulServiceRegistry>();

        var registration = serviceProvider.GetRequiredService<ConsulRegistration>();
        Assert.Equal("from-test", registration.Host);

        _ = serviceProvider.GetRequiredService<ConsulServiceRegistrar>();
        _ = serviceProvider.GetRequiredService<IHealthContributor>();
    }

    [Fact]
    public async Task WithConsulUrlConfiguration_AddsDiscoveryClient()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "myName",
            ["urls"] = "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233",
            ["consul:discovery:register"] = "false",
            ["consul:discovery:deregister"] = "false"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddOptions();
        services.AddConsulDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];

        Assert.Single(discoveryClients);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClients[0]);

        _ = serviceProvider.GetRequiredService<IConsulClient>();
        _ = serviceProvider.GetRequiredService<TtlScheduler>();
        _ = serviceProvider.GetRequiredService<ConsulServiceRegistry>();

        var registration = serviceProvider.GetRequiredService<ConsulRegistration>();
        Assert.Equal(1234, registration.Port);

        _ = serviceProvider.GetRequiredService<ConsulServiceRegistrar>();
        _ = serviceProvider.GetRequiredService<IHealthContributor>();
    }

    [Fact]
    public async Task WithConsul_UrlBypassWorks()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "myName",
            ["urls"] = "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233",
            ["consul:discovery:register"] = "false",
            ["consul:discovery:deregister"] = "false",
            ["Consul:Discovery:UseAspNetCoreUrls"] = "false"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddOptions();
        services.AddConsulDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var registration = serviceProvider.GetRequiredService<ConsulRegistration>();

        Assert.NotEqual("myapp", registration.Host);
        Assert.Equal(0, registration.Port);

        _ = serviceProvider.GetRequiredService<ConsulServiceRegistrar>();
        _ = serviceProvider.GetRequiredService<IHealthContributor>();
    }

    [Fact]
    public async Task WithConsul_PreferPortOverUrl()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "myName",
            ["urls"] = "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233",
            ["consul:discovery:register"] = "false",
            ["consul:discovery:deregister"] = "false",
            ["Consul:Discovery:Port"] = "8080"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddOptions();
        services.AddConsulDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var registration = serviceProvider.GetRequiredService<ConsulRegistration>();

        Assert.NotEqual("myapp", registration.Host);
        Assert.Equal(8080, registration.Port);

        _ = serviceProvider.GetRequiredService<ConsulServiceRegistrar>();
        _ = serviceProvider.GetRequiredService<IHealthContributor>();
    }

    [Fact]
    public async Task WithAppConfiguration_AddsAndWorks()
    {
        const string appSettings = """
            {
                "discovery": {
                    "services": [
                        { "serviceId": "fruitService", "host": "fruit-ball", "port": 443, "isSecure": true },
                        { "serviceId": "fruitService", "host": "fruit-baller", "port": 8081 },
                        { "serviceId": "vegetableService", "host": "vegemite", "port": 443, "isSecure": true },
                        { "serviceId": "vegetableService", "host": "carrot", "port": 8081 },
                    ]
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appSettings);

        IConfiguration configuration = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)!).AddJsonFile(Path.GetFileName(path)).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton(configuration);
        services.AddConfigurationDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];

        Assert.Single(discoveryClients);
        Assert.IsType<ConfigurationDiscoveryClient>(discoveryClients[0]);

        Assert.Contains("fruitService", await discoveryClients[0].GetServiceIdsAsync(CancellationToken.None));
        Assert.Contains("vegetableService", await discoveryClients[0].GetServiceIdsAsync(CancellationToken.None));

        IList<IServiceInstance> fruitInstances = await discoveryClients[0].GetInstancesAsync("fruitService", CancellationToken.None);
        Assert.Equal(2, fruitInstances.Count);

        IList<IServiceInstance> vegetableInstances = await discoveryClients[0].GetInstancesAsync("vegetableService", CancellationToken.None);
        Assert.Equal(2, vegetableInstances.Count);
    }

    [Fact]
    public async Task WithMultipleClients_AddsDiscoveryClients()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(FastDiscovery).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddConfigurationDiscoveryClient();
        services.AddConsulDiscoveryClient();
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];
        discoveryClients.Should().HaveCount(3);

        serviceProvider.GetServices<IHealthContributor>().OfType<ConsulHealthContributor>().Should().ContainSingle();
        serviceProvider.GetServices<IHealthContributor>().OfType<EurekaServerHealthContributor>().Should().ContainSingle();
        serviceProvider.GetServices<IHealthContributor>().OfType<EurekaApplicationsHealthContributor>().Should().BeEmpty();
    }

    private sealed class TestApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
    }
}
