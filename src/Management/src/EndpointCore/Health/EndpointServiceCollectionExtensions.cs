// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
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
        public static void AddHealthActuator(this IServiceCollection services, IConfiguration config)
        {
            services.AddHealthActuator(config, new HealthRegistrationsAggregator(), GetDefaultHealthContributors());
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

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(config, Platform.IsCloudFoundry)));
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new CloudFoundryManagementOptions(config, Platform.IsCloudFoundry)));

            var options = new HealthEndpointOptions(config);
            services.TryAddSingleton<IHealthOptions>(options);
            services.RegisterEndpointOptions(options);
            AddHealthContributors(services, contributors);

            services.TryAddSingleton<IHealthAggregator>(aggregator);
            services.TryAddScoped<HealthEndpointCore>();
        }

        public static void AddHealthContributors(IServiceCollection services, params Type[] contributors)
        {
            List<ServiceDescriptor> descriptors = new List<ServiceDescriptor>();
            foreach (var c in contributors)
            {
                descriptors.Add(new ServiceDescriptor(typeof(IHealthContributor), c, ServiceLifetime.Scoped));
            }

            services.TryAddEnumerable(descriptors);
        }

        internal static Type[] GetDefaultHealthContributors()
        {
            return new Type[]
            {
                typeof(DiskSpaceContributor)
            };
        }
    }
}
