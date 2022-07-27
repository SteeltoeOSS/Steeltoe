// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Net;
using Steeltoe.Connector.Services;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using System;
using System.Linq;

namespace Steeltoe.Discovery.Consul;

public class ConsulDiscoveryClientExtension : IDiscoveryClientExtension
{
    public const string CONSUL_PREFIX = "consul";
    private const string _springDiscoveryEnabled = "spring:cloud:discovery:enabled";

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        ConfigureConsulServices(services);
        AddConsulServices(services);
    }

    public bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null)
        => configuration.GetSection(CONSUL_PREFIX).GetChildren().Any();

    internal static void ConfigureConsulServices(IServiceCollection services)
    {
        services
            .AddOptions<ConsulOptions>()
            .Configure<IConfiguration>((options, config) => config.GetSection(ConsulOptions.CONSUL_CONFIGURATION_PREFIX).Bind(options))
            .PostConfigure(options => ConsulPostConfigurer.ValidateConsulOptions(options));

        services
            .AddOptions<ConsulDiscoveryOptions>()
            .Configure<IConfiguration>((options, config) =>
            {
                config.GetSection(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX).Bind(options);

                // Consul is enabled by default. If consul:discovery:enabled was not set then check spring:cloud:discovery:enabled
                if (options.Enabled &&
                    config.GetValue<bool?>(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX + ":enabled") is null &&
                    config.GetValue<bool?>(_springDiscoveryEnabled) == false)
                {
                    options.Enabled = false;
                }
            })
            .PostConfigure<IConfiguration>((options, config) =>
            {
                var netOptions = config.GetSection(InetOptions.PREFIX).Get<InetOptions>();
                ConsulPostConfigurer.UpdateDiscoveryOptions(config, options, netOptions);
            });

        services.TryAddSingleton(serviceProvider =>
        {
            var clientOptions = serviceProvider.GetRequiredService<IOptions<ConsulDiscoveryOptions>>();
            return new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(clientOptions.Value.CacheTTL) };
        });
    }

    private static void AddConsulServices(IServiceCollection services)
    {
        services.AddSingleton((p) =>
        {
            var consulOptions = p.GetRequiredService<IOptions<ConsulOptions>>();
            return ConsulClientFactory.CreateClient(consulOptions.Value);
        });

        services.AddSingleton<IScheduler, TtlScheduler>();
        services.AddSingleton<IConsulServiceRegistry, ConsulServiceRegistry>();
        services.AddSingleton<IConsulRegistration>((p) =>
        {
            var opts = p.GetRequiredService<IOptions<ConsulDiscoveryOptions>>();
            var appInfo = p.GetService<IApplicationInstanceInfo>();
            return ConsulRegistration.CreateRegistration(opts.Value, appInfo);
        });
        services.AddSingleton<IConsulServiceRegistrar, ConsulServiceRegistrar>();
        services.AddSingleton<IDiscoveryClient, ConsulDiscoveryClient>();
        services.AddSingleton<IHealthContributor, ConsulHealthContributor>();
    }
}