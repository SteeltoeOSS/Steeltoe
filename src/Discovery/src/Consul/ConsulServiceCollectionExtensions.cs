// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;

namespace Steeltoe.Discovery.Consul;

public static class ConsulServiceCollectionExtensions
{
    private const string SpringDiscoveryEnabled = "spring:cloud:discovery:enabled";

    /// <summary>
    /// Configures to use <see cref="ConsulDiscoveryClient" /> for service discovery.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    public static IServiceCollection AddConsulDiscoveryClient(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        ConfigureConsulServices(services);
        AddConsulServices(services);

        return services;
    }

    private static void ConfigureConsulServices(IServiceCollection services)
    {
        services.RegisterDefaultApplicationInstanceInfo();
        services.TryAddSingleton<InetUtils>();

        ConfigureConsulOptions(services);
        ConfigureConsulDiscoveryOptions(services);
    }

    private static void ConfigureConsulOptions(IServiceCollection services)
    {
        services.ConfigureReloadableOptions<ConsulOptions>(ConsulOptions.ConfigurationPrefix);
        services.AddSingleton<IValidateOptions<ConsulOptions>, ValidateConsulOptions>();
    }

    private static void ConfigureConsulDiscoveryOptions(IServiceCollection services)
    {
        services.ConfigureReloadableOptions<ConsulDiscoveryOptions>(ConsulDiscoveryOptions.ConfigurationPrefix, (options, serviceProvider) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // Consul is enabled by default. If consul:discovery:enabled was not set then check spring:cloud:discovery:enabled
            if (options.Enabled && configuration.GetValue<bool?>($"{ConsulDiscoveryOptions.ConfigurationPrefix}:enabled") is null &&
                configuration.GetValue<bool?>(SpringDiscoveryEnabled) == false)
            {
                options.Enabled = false;
            }
        });

        services.ConfigureReloadableOptions<InetOptions>(InetOptions.ConfigurationPrefix);

        services.AddSingleton<IPostConfigureOptions<ConsulDiscoveryOptions>, PostConfigureConsulDiscoveryOptions>();
    }

    private static void AddConsulServices(IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            var consulOptions = serviceProvider.GetRequiredService<IOptions<ConsulOptions>>();
            return ConsulClientFactory.CreateClient(consulOptions.Value);
        });

        services.AddSingleton<TtlScheduler>();
        services.AddSingleton<ConsulServiceRegistry>();

        services.AddSingleton(serviceProvider =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConsulDiscoveryOptions>>();
            var instanceInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();
            return ConsulRegistration.Create(optionsMonitor, instanceInfo);
        });

        services.AddSingleton<ConsulServiceRegistrar>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IDiscoveryClient), typeof(ConsulDiscoveryClient)));
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), typeof(DiscoveryClientHostedService)));
        services.AddSingleton<IHealthContributor, ConsulHealthContributor>();
    }
}
