// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConfigurationServiceInstanceProviderServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an IConfiguration-based <see cref="IServiceInstanceProvider"/> to the <see cref="IServiceCollection" />
        /// </summary>
        /// <param name="services">Your <see cref="IServiceCollection"/></param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="serviceLifetime">Lifetime of the <see cref="IServiceInstanceProvider"/></param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddConfigurationDiscoveryClient(this IServiceCollection services, IConfiguration configuration, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.Add(new ServiceDescriptor(typeof(IServiceInstanceProvider), typeof(ConfigurationServiceInstanceProvider), serviceLifetime));
            services.AddOptions();
            services.Configure<List<ConfigurationServiceInstance>>(configuration.GetSection("discovery:services"));
            return services;
        }
    }
}
