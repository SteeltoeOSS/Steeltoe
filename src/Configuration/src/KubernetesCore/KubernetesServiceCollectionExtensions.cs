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
using Steeltoe.Common.Kubernetes;
using System;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    /// <summary>
    /// Extension methods for adding services related to Kubernetes
    /// </summary>
    public static class KubernetesServiceCollectionExtensions
    {
        /// <summary>
        /// Bind configuration data into <see cref="KubernetesApplicationOptions"/> and <see cref="KubernetesServicesOptions"/>
        /// and add both to the provided service container as configured TOptions.  You can then inject both options using the normal
        /// Options pattern.
        /// </summary>
        /// <param name="services">the service container</param>
        /// <param name="config">the applications configuration</param>
        /// <returns>service container</returns>
        public static IServiceCollection ConfigureKubernetesOptions(this IServiceCollection services, IConfiguration config)
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

            var appSection = config.GetSection(KubernetesApplicationOptions.PlatformConfigRoot);
            services.Configure<KubernetesApplicationOptions>(appSection);

            var serviceSection = config.GetSection(KubernetesServicesOptions.ServicesConfigRoot);
            services.Configure<KubernetesServicesOptions>(serviceSection);

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
        public static IServiceCollection ConfigureKubernetesService<TOption>(this IServiceCollection services, IConfiguration config, string serviceName)
            where TOption : KubernetesServicesOptions
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
        public static IServiceCollection ConfigureKubernetesServices<TOption>(this IServiceCollection services, IConfiguration config, string serviceLabel)
               where TOption : KubernetesServicesOptions
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
                    services.ConfigureKubernetesService<TOption>(config, s.Name);
                }
            }

            return services;
        }

        private static KubernetesServicesOptions GetServiceOptionsFromConfiguration(IConfiguration config)
        {
            if (config is IConfigurationRoot asRoot)
            {
                return new KubernetesServicesOptions(asRoot);
            }
            else
            {
                return new KubernetesServicesOptions(config);
            }
        }
    }
}
