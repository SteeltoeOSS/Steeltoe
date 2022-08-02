// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaDiscoveryClientBuilderExtensionsTest
{
    [Fact]
    public void ApplyServicesNoExceptionWithoutManagementOptions()
    {
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(config);
        serviceCollection.RegisterDefaultApplicationInstanceInfo();
        var extension = new EurekaDiscoveryClientExtension();

        extension.ApplyServices(serviceCollection);
        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.NotNull(options);
        Assert.Equal("/health", options.Value.HealthCheckUrlPath);
        Assert.Equal("/info", options.Value.StatusPageUrlPath);
    }

    [Fact]
    public void ApplyServicesUsesManagementOptions()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "management:endpoints:health:path", "/non-default" }
        };

        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(config);
        serviceCollection.RegisterDefaultApplicationInstanceInfo();
        serviceCollection.AddAllActuators(config);
        var extension = new EurekaDiscoveryClientExtension();

        extension.ApplyServices(serviceCollection);
        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.NotNull(options);
        Assert.Equal("/actuator/non-default", options.Value.HealthCheckUrlPath);
        Assert.Equal("/actuator/info", options.Value.StatusPageUrlPath);
    }

    [Fact]
    public void ApplyServicesUsesServerTimeout()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "Eureka:Client:EurekaServer:ConnectTimeoutSeconds", "1" },
            { "Eureka:Client:EurekaServer:RetryCount", "1" }
        };

        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(config);
        serviceCollection.RegisterDefaultApplicationInstanceInfo();
        serviceCollection.AddAllActuators(config);
        var extension = new EurekaDiscoveryClientExtension();

        extension.ApplyServices(serviceCollection);
        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var timer = new Stopwatch();
        timer.Start();
        provider.GetService<IDiscoveryClient>();
        timer.Stop();
        Assert.InRange(timer.ElapsedMilliseconds, 0, 3500);
    }

    [Fact]
    public void ApplyServicesIgnoresCFManagementOptions()
    {
        string vcap_services = @"
                {
                    ""p-service-registry"": [{
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
                    }]
                }";

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);

        var appSettings = new Dictionary<string, string>
        {
            { "management:endpoints:health:path", "/non-default" }
        };

        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(config);
        serviceCollection.RegisterDefaultApplicationInstanceInfo();
        serviceCollection.AddAllActuators(config);
        var extension = new EurekaDiscoveryClientExtension();

        extension.ApplyServices(serviceCollection);
        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.NotNull(options);
        Assert.Equal("/actuator/non-default", options.Value.HealthCheckUrlPath);
        Assert.Equal("/actuator/info", options.Value.StatusPageUrlPath);
    }

    [Fact]
    public void ApplyServicesDoesNotOverrideUserPathSettings()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "eureka:instance:healthcheckurlpath", "/customHealth" },
            { "eureka:instance:statuspageurlpath", "/customStatus" }
        };

        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(config);
        serviceCollection.RegisterDefaultApplicationInstanceInfo();
        serviceCollection.AddAllActuators(config);
        var extension = new EurekaDiscoveryClientExtension();

        extension.ApplyServices(serviceCollection);
        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<EurekaInstanceOptions>>();
        Assert.NotNull(options);
        Assert.Equal("/customHealth", options.Value.HealthCheckUrlPath);
        Assert.Equal("/customStatus", options.Value.StatusPageUrlPath);
    }
}
