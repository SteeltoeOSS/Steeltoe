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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.RabbitMQ;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.Hystrix
{
    public static class HystrixProviderServiceCollectionExtensions
    {
        /// <summary>
        /// Adds HystrixConnectionFactory to your ServiceCollection
        /// </summary>
        /// <param name="services">Your Service Collection</param>
        /// <param name="config">Application Configuration</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">Not implemented</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddHystrixConnection(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Singleton, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            HystrixRabbitMQServiceInfo info = config.GetSingletonServiceInfo<HystrixRabbitMQServiceInfo>();

            DoAdd(services, info, config, contextLifetime);
            return services;
        }

        /// <summary>
        /// Adds HystrixConnectionFactory to your ServiceCollection
        /// </summary>
        /// <param name="services">Your Service Collection</param>
        /// <param name="config">Application Configuration</param>
        /// <param name="serviceName">Cloud Foundry service name binding</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">Not implemented</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddHystrixConnection(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Singleton, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            HystrixRabbitMQServiceInfo info = config.GetRequiredServiceInfo<HystrixRabbitMQServiceInfo>(serviceName);

            DoAdd(services, info, config, contextLifetime);
            return services;
        }

        private static void DoAdd(IServiceCollection services, HystrixRabbitMQServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
        {
            Type rabbitFactory = RabbitMQTypeLocator.ConnectionFactory;
            HystrixProviderConnectorOptions hystrixConfig = new HystrixProviderConnectorOptions(config);
            HystrixProviderConnectorFactory factory = new HystrixProviderConnectorFactory(info, hystrixConfig, rabbitFactory);
            services.Add(new ServiceDescriptor(typeof(HystrixConnectionFactory), factory.Create, contextLifetime));
        }
    }
}
