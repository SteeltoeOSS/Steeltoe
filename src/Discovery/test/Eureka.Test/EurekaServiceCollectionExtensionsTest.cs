// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Management.Endpoint;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaServiceCollectionExtensionsTest
{
    [Fact]
    public void AddEurekaDiscoveryClient_NoExceptionWithoutManagementOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IServer, TestServer>();
        services.AddEurekaDiscoveryClient();

        ServiceProvider provider = services.BuildServiceProvider(true);

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.Equal("/health", options.Value.HealthCheckUrlPath);
        Assert.Equal("/info", options.Value.StatusPageUrlPath);
    }

    [Fact]
    public void AddEurekaDiscoveryClient_UsesManagementOptions()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:health:path", "/non-default" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IServer, TestServer>();
        services.AddAllActuators();
        services.AddEurekaDiscoveryClient();

        ServiceProvider provider = services.BuildServiceProvider(true);

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.Equal("/actuator/non-default", options.Value.HealthCheckUrlPath);
        Assert.Equal("/actuator/info", options.Value.StatusPageUrlPath);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    public void AddEurekaDiscoveryClient_UsesServerTimeout()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "Eureka:Client:EurekaServer:ConnectTimeoutSeconds", "1" },
            { "Eureka:Client:EurekaServer:RetryCount", "1" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IServer, TestServer>();
        services.AddEurekaDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var timer = new Stopwatch();
        timer.Start();

        IDiscoveryClient[] discoveryClients = serviceProvider.GetRequiredService<IEnumerable<IDiscoveryClient>>().ToArray();
        Assert.Single(discoveryClients);

        timer.Stop();
        Assert.InRange(timer.ElapsedMilliseconds, 0, 3500);
    }

    [Fact]
    public void AddEurekaDiscoveryClient_IgnoresCloudFoundryManagementOptions()
    {
        const string vcapServices = """
            {
              "p-service-registry": [
                {
                  "credentials": {
                    "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com",
                    "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                    "client_secret": "dCsdoiuklicS",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token"
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

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);

        var appSettings = new Dictionary<string, string?>
        {
            { "management:endpoints:health:path", "/non-default" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IServer, TestServer>();
        services.AddAllActuators();
        services.AddEurekaDiscoveryClient();

        ServiceProvider provider = services.BuildServiceProvider(true);

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.Equal("/actuator/non-default", options.Value.HealthCheckUrlPath);
        Assert.Equal("/actuator/info", options.Value.StatusPageUrlPath);
    }

    [Fact]
    public void AddEurekaDiscoveryClient_DoesNotOverrideUserPathSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "eureka:instance:healthcheckurlpath", "/customHealth" },
            { "eureka:instance:statuspageurlpath", "/customStatus" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IServer, TestServer>();
        services.AddAllActuators();
        services.AddEurekaDiscoveryClient();

        ServiceProvider provider = services.BuildServiceProvider(true);

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.Equal("/customHealth", options.Value.HealthCheckUrlPath);
        Assert.Equal("/customStatus", options.Value.StatusPageUrlPath);
    }

    [Fact]
    public void AddEurekaDiscoveryClient_DoesNotRegisterEurekaDiscoveryClientMultipleTimes()
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

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IDiscoveryClient[] discoveryClients = serviceProvider.GetRequiredService<IEnumerable<IDiscoveryClient>>().ToArray();
        discoveryClients.OfType<EurekaDiscoveryClient>().Should().HaveCount(1);

        EurekaDiscoveryClient[] eurekaDiscoveryClients = serviceProvider.GetRequiredService<IEnumerable<EurekaDiscoveryClient>>().ToArray();
        eurekaDiscoveryClients.Should().HaveCount(1);
    }

    [Fact]
    public void AddEurekaDiscoveryClient_RegistersHostedService()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:Enabled"] = "false"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IServer, TestServer>();
        services.AddEurekaDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IHostedService[] hostedServices = serviceProvider.GetRequiredService<IEnumerable<IHostedService>>().ToArray();
        hostedServices.OfType<DiscoveryClientHostedService>().Should().HaveCount(1);
    }
}
