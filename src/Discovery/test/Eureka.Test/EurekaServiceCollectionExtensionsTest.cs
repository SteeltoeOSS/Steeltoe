// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Management.Endpoint.Actuators.All;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddEurekaDiscoveryClient_NoExceptionWithoutManagementOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider provider = services.BuildServiceProvider(true);

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.Equal("/health", options.Value.HealthCheckUrlPath);
        Assert.Equal("/info", options.Value.StatusPageUrlPath);
    }

    [Fact]
    public async Task AddEurekaDiscoveryClient_UsesManagementOptions()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:health:path", "/non-default" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddAllActuators();
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider provider = services.BuildServiceProvider(true);

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.Equal("/actuator/non-default", options.Value.HealthCheckUrlPath);
        Assert.Equal("/actuator/info", options.Value.StatusPageUrlPath);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    public async Task AddEurekaDiscoveryClient_UsesServerTimeout()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "Eureka:Client:EurekaServer:ConnectTimeoutSeconds", "1" },
            { "Eureka:Client:EurekaServer:RetryCount", "1" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var timer = new Stopwatch();
        timer.Start();

        IDiscoveryClient[] discoveryClients = serviceProvider.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);

        timer.Stop();
        Assert.InRange(timer.ElapsedMilliseconds, 0, 3500);
    }

    [Fact]
    public async Task AddEurekaDiscoveryClient_IgnoresCloudFoundryManagementOptions()
    {
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
                }
              ]
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", """
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
            """);

        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);

        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:health:path", "/non-default" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddAllActuators();
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider provider = services.BuildServiceProvider(true);

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.Equal("/actuator/non-default", options.Value.HealthCheckUrlPath);
        Assert.Equal("/actuator/info", options.Value.StatusPageUrlPath);
    }

    [Fact]
    public async Task AddEurekaDiscoveryClient_DoesNotOverrideUserPathSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "eureka:instance:healthCheckUrlPath", "/customHealth" },
            { "eureka:instance:statusPageUrlPath", "/customStatus" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddAllActuators();
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider provider = services.BuildServiceProvider(true);

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.Equal("/customHealth", options.Value.HealthCheckUrlPath);
        Assert.Equal("/customStatus", options.Value.StatusPageUrlPath);
    }

    [Fact]
    public async Task AddEurekaDiscoveryClient_DoesNotRegisterEurekaDiscoveryClientMultipleTimes()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:Enabled"] = "false"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddEurekaDiscoveryClient();
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<IDiscoveryClient>().OfType<EurekaDiscoveryClient>().Should().ContainSingle();
        serviceProvider.GetServices<EurekaDiscoveryClient>().Should().ContainSingle();
        serviceProvider.GetServices<IHealthContributor>().OfType<EurekaServerHealthContributor>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddEurekaDiscoveryClient_RegistersHostedService()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:Enabled"] = "false"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IHostedService[] hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        hostedServices.OfType<DiscoveryClientHostedService>().Should().ContainSingle();
    }
}
