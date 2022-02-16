// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.HealthChecks;
using System;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    /// <summary>
    /// Extension methods for adding services related to Spring Cloud Config Server.
    /// </summary>
    public static class ConfigServerServiceCollectionExtensions
    {
        [Obsolete("IConfiguration parameter is not needed anymore")]
        public static IServiceCollection ConfigureConfigServerClientOptions(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddOptions();

            var section = config.GetSection(ConfigServerClientSettingsOptions.CONFIGURATION_PREFIX);
            services.Configure<ConfigServerClientSettingsOptions>(section);

            return services;
        }

        public static IServiceCollection ConfigureConfigServerClientOptions(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services
                .AddOptions<ConfigServerClientSettingsOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    config.GetSection(ConfigServerClientSettingsOptions.CONFIGURATION_PREFIX).Bind(options);
                });

            return services;
        }

        /// <summary>
        /// Add the ConfigServerHealthContributor as a IHealthContributor to the service container.
        /// </summary>
        /// <param name="services">the service container</param>
        /// <returns>the service collection</returns>
        public static IServiceCollection AddConfigServerHealthContributor(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IHealthContributor, ConfigServerHealthContributor>();

            return services;
        }

        /// <summary>
        /// Configures ConfigServerClientSettingsOptions, ConfigServerHostedService, Config Server Health Contributor
        /// and ensures IConfigurationRoot is available
        /// </summary>
        /// <param name="services">Your <see cref="IServiceCollection"/></param>
        public static void AddConfigServerServices(this IServiceCollection services)
        {
            services.ConfigureConfigServerClientOptions();
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IConfiguration>() as IConfigurationRoot);
            services.TryAddSingleton<IHostedService, ConfigServerHostedService>();
            services.AddConfigServerHealthContributor();
        }
    }
}
