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

namespace Steeltoe.Discovery.Consul
{
    public class ConsulDiscoveryClientExtension : IDiscoveryClientExtension
    {
        public const string CONSUL_PREFIX = "consul";

        /// <inheritdoc />
        public void ApplyServices(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var netOptions = config.GetSection(InetOptions.PREFIX).Get<InetOptions>();
            ConfigureConsulServices(services, config, netOptions);
            AddConsulServices(services);
        }

        public bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null)
        {
            return configuration.GetSection(CONSUL_PREFIX).GetChildren().Any();
        }

        private static void ConfigureConsulServices(IServiceCollection services, IConfiguration config, InetOptions netOptions)
        {
            var consulSection = config.GetSection(ConsulOptions.CONSUL_CONFIGURATION_PREFIX);
            services.Configure<ConsulOptions>(consulSection);
            services.PostConfigure<ConsulOptions>(options => ConsulPostConfigurer.ValidateConsulOptions(options));
            var consulDiscoverySection = config.GetSection(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX);
            services.Configure<ConsulDiscoveryOptions>(consulDiscoverySection);
            services.PostConfigure<ConsulDiscoveryOptions>(options => ConsulPostConfigurer.UpdateDiscoveryOptions(config, options, netOptions));
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
                var appInfo = services.GetApplicationInstanceInfo();
                return ConsulRegistration.CreateRegistration(opts.Value, appInfo);
            });
            services.AddSingleton<IConsulServiceRegistrar, ConsulServiceRegistrar>();
            services.AddSingleton<IDiscoveryClient, ConsulDiscoveryClient>();
            services.AddSingleton<IHealthContributor, ConsulHealthContributor>();
        }
    }
}
