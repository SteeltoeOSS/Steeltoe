// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Net;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;

namespace Steeltoe.Discovery.Consul.Test.Registry;

internal static class TestRegistrationFactory
{
    public static ConsulDiscoveryOptions CreateDiscoveryOptions(Dictionary<string, string?> appSettings)
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => configurationRoot);
        services.AddApplicationInstanceInfo();

        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();

        var options = new ConsulDiscoveryOptions();
        configurationRoot.GetSection("consul:discovery").Bind(options);

        var domainNameResolver = Substitute.For<IDomainNameResolver>();
        domainNameResolver.ResolveHostName().Returns(_ => "localhost");

        var networkInterfaceProvider = Substitute.For<INetworkInterfaceProvider>();
        var inetUtils = new InetUtils(domainNameResolver, networkInterfaceProvider, new TestOptionsMonitor<InetOptions>(), NullLogger<InetUtils>.Instance);

        var configurer = new PostConfigureConsulDiscoveryOptions(configurationRoot, domainNameResolver, inetUtils, appInfo);
        configurer.PostConfigure(null, options);

        return options;
    }

    public static TestOptionsMonitor<ConsulDiscoveryOptions> CreateOptionsMonitor(Dictionary<string, string?> appSettings)
    {
        ConsulDiscoveryOptions options = CreateDiscoveryOptions(appSettings);
        return TestOptionsMonitor.Create(options);
    }

    public static ConsulServiceInstance CreateServiceInstance(Dictionary<string, string?> appSettings)
    {
        ConsulDiscoveryOptions options = CreateDiscoveryOptions(appSettings);
        return ConsulServiceInstance.FromOptions(options);
    }

    public static ConsulServiceRegistration CreateRegistration(Dictionary<string, string?> appSettings)
    {
        ConsulDiscoveryOptions options = CreateDiscoveryOptions(appSettings);
        ConsulServiceInstance serviceInstance = ConsulServiceInstance.FromOptions(options);
        return new ConsulServiceRegistration(serviceInstance, options);
    }
}
