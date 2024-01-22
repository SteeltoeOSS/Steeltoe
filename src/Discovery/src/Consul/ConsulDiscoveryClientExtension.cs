// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Net;
using Steeltoe.Connectors.Services;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;

namespace Steeltoe.Discovery.Consul;

public class ConsulDiscoveryClientExtension : IDiscoveryClientExtension
{
    private const string SpringDiscoveryEnabled = "spring:cloud:discovery:enabled";
    private const string ConsulPrefix = "consul";

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        ConfigureConsulServices(services);
        AddConsulServices(services);
    }

    public bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null)
    {
        return configuration.GetSection(ConsulPrefix).GetChildren().Any();
    }

    internal static void ConfigureConsulServices(IServiceCollection services)
    {
        services.AddOptions<ConsulOptions>()
            .Configure<IConfiguration>((options, configuration) => configuration.GetSection(ConsulOptions.ConfigurationPrefix).Bind(options))
            .PostConfigure(ConsulPostConfigurer.ValidateConsulOptions);

        services.AddOptions<ConsulDiscoveryOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetSection(ConsulDiscoveryOptions.ConfigurationPrefix).Bind(options);

            // Consul is enabled by default. If consul:discovery:enabled was not set then check spring:cloud:discovery:enabled
            if (options.Enabled && configuration.GetValue<bool?>($"{ConsulDiscoveryOptions.ConfigurationPrefix}:enabled") is null &&
                configuration.GetValue<bool?>(SpringDiscoveryEnabled) == false)
            {
                options.Enabled = false;
            }
        }).PostConfigure<IConfiguration, ILoggerFactory>((discoveryOptions, configuration, loggerFactory) =>
        {
            InetOptions inetOptions = configuration.GetSection(InetOptions.ConfigurationPrefix).Get<InetOptions>() ?? new InetOptions();
            ConsulPostConfigurer.UpdateDiscoveryOptions(configuration, discoveryOptions, inetOptions, loggerFactory);
        });

        services.TryAddSingleton(serviceProvider =>
        {
            var clientOptions = serviceProvider.GetRequiredService<IOptions<ConsulDiscoveryOptions>>();

            return new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(clientOptions.Value.CacheTtl)
            };
        });
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
            var instanceInfo = serviceProvider.GetService<IApplicationInstanceInfo>();
            return ConsulRegistration.Create(optionsMonitor, instanceInfo);
        });

        services.AddSingleton<ConsulServiceRegistrar>();
        services.AddSingleton<IDiscoveryClient, ConsulDiscoveryClient>();
        services.AddSingleton<IHealthContributor, ConsulHealthContributor>();
    }
}
