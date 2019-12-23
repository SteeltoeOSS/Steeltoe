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
using System;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    /// <summary>
    /// Extension methods for adding services related to CloudFoundry
    /// </summary>
    public static class CloudFoundryServiceCollectionExtensions
    {
        /// <summary>
        /// Bind configuration data into <see cref="CloudFoundryApplicationOptions"/> and <see cref="CloudFoundryServicesOptions"/>
        /// and add both to the provided service container as configured TOptions.  You can then inject both options using the normal
        /// Options pattern.
        /// </summary>
        /// <param name="services">the service container</param>
        /// <param name="config">the applications configuration</param>
        /// <returns>service container</returns>
        public static IServiceCollection ConfigureCloudFoundryOptions(this IServiceCollection services, IConfiguration config)
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

            var appSection = config.GetSection(CloudFoundryApplicationOptions.ApplicationConfigRoot);
            services.Configure<CloudFoundryApplicationOptions>(appSection);

            var serviceSection = config.GetSection(CloudFoundryServicesOptions.CONFIGURATION_PREFIX);
            services.Configure<CloudFoundryServicesOptions>(serviceSection);

            return services;
        }

        /// <summary>
        /// Find the Cloud Foundry service with the <paramref name="serviceName"/> in VCAP_SERVICES and bind the configuration data from
        /// the provided <paramref name="config"/> into the options type and add it to the provided service container as a configured named TOption.
        /// The name of the TOption will be the <paramref name="serviceName"/>. You can then inject the option using the normal Options pattern.
        /// </summary>
        /// <typeparam name="TOption">the options type</typeparam>
        /// <param name="services">the service container</param>
        /// <param name="config">the applications configuration</param>
        /// <param name="serviceName">the Cloud Foundry service name to bind to the options type</param>
        /// <returns>service container</returns>
        public static IServiceCollection ConfigureCloudFoundryService<TOption>(this IServiceCollection services, IConfiguration config, string serviceName)
            where TOption : AbstractServiceOptions
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            services.Configure<TOption>(serviceName, (option) =>
            {
                option.Bind(config, serviceName);
            });

            return services;
        }

        /// <summary>
        /// Find all of the Cloud Foundry services with the <paramref name="serviceLabel"/> in VCAP_SERVICES and bind the configuration data from
        /// the provided <paramref name="config"/> into the options type and add them all to the provided service container as a configured named TOptions.
        /// The name of each TOption will be the the name of the Cloud Foundry service binding. You can then inject all the options using the normal Options pattern.
        /// </summary>
        /// <typeparam name="TOption">the options type</typeparam>
        /// <param name="services">the service container</param>
        /// <param name="config">the applications configuration</param>
        /// <param name="serviceLabel">the Cloud Foundry service label to use to bind to the options type</param>
        /// <returns>serice container</returns>
        public static IServiceCollection ConfigureCloudFoundryServices<TOption>(this IServiceCollection services, IConfiguration config, string serviceLabel)
               where TOption : AbstractServiceOptions
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(serviceLabel))
            {
                throw new ArgumentException(nameof(serviceLabel));
            }

            var servicesOptions = GetServiceOptionsFromConfiguration(config);
            servicesOptions.Services.TryGetValue(serviceLabel, out var cfServices);
            if (cfServices != null)
            {
                foreach (var s in cfServices)
                {
                    services.ConfigureCloudFoundryService<TOption>(config, s.Name);
                }
            }

            return services;
        }

        private static CloudFoundryServicesOptions GetServiceOptionsFromConfiguration(IConfiguration config)
        {
            if (config is IConfigurationRoot asRoot)
            {
                return new CloudFoundryServicesOptions(asRoot);
            }
            else
            {
                return new CloudFoundryServicesOptions(config);
            }
        }
    }
}
