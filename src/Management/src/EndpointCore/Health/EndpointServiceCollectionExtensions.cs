// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Availability;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Health
{
    public static class EndpointServiceCollectionExtensions
    {
        /// <summary>
        /// Adds components of the Health actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add health to</param>
        /// <param name="config">Application configuration (this actuator looks for a settings starting with management:endpoints:health)</param>
        public static void AddHealthActuator(this IServiceCollection services, IConfiguration config = null)
        {
            var serviceProvider = services.BuildServiceProvider();
            config ??= serviceProvider.GetRequiredService<IConfiguration>();
            services.AddHealthActuator(config, new HealthRegistrationsAggregator(), DefaultHealthContributors);
        }

        /// <summary>
        /// Adds components of the Health actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add health to</param>
        /// <param name="config">Application configuration (this actuator looks for a settings starting with management:endpoints:health)</param>
        /// <param name="contributors">Contributors to application health</param>
        public static void AddHealthActuator(this IServiceCollection services, IConfiguration config, params Type[] contributors)
        {
            services.AddHealthActuator(config, new HealthRegistrationsAggregator(), contributors);
        }

        /// <summary>
        /// Adds components of the Health actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add health to</param>
        /// <param name="config">Application configuration (this actuator looks for a settings starting with management:endpoints:health)</param>
        /// <param name="aggregator">Custom health aggregator</param>
        /// <param name="contributors">Contributors to application health</param>
        public static void AddHealthActuator(this IServiceCollection services, IConfiguration config, IHealthAggregator aggregator, params Type[] contributors)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (aggregator == null)
            {
                throw new ArgumentNullException(nameof(aggregator));
            }

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(config)));
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new CloudFoundryManagementOptions(config)));

            var options = new HealthEndpointOptions(config);
            services.TryAddSingleton<IHealthOptions>(options);
            services.RegisterEndpointOptions(options);
            AddHealthContributors(services, contributors);

            services.TryAddSingleton(aggregator);
            services.TryAddScoped<HealthEndpointCore>();
            services.TryAddSingleton<ApplicationAvailability>();
        }

        public static void AddHealthContributors(IServiceCollection services, params Type[] contributors)
        {
            var descriptors = new List<ServiceDescriptor>();
            foreach (var c in contributors)
            {
                descriptors.Add(new ServiceDescriptor(typeof(IHealthContributor), c, ServiceLifetime.Scoped));
            }

            services.TryAddEnumerable(descriptors);
        }

        internal static Type[] DefaultHealthContributors => new Type[] { typeof(DiskSpaceContributor), typeof(LivenessHealthContributor), typeof(ReadinessHealthContributor) };
    }
}
