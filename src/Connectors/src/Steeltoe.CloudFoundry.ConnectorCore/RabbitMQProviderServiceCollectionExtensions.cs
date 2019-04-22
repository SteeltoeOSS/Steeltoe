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
using Steeltoe.Common.HealthChecks;
using System;

namespace Steeltoe.CloudFoundry.Connector.RabbitMQ
{
    public static class RabbitMQProviderServiceCollectionExtensions
    {
        /// <summary>
        /// Add RabbitMQ and its IHealthContributor to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">logger factory</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>RabbitMQ.Client.ConnectionFactory is retrievable as both ConnectionFactory and IConnectionFactory</remarks>
        public static IServiceCollection AddRabbitMQConnection(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RabbitMQServiceInfo info = config.GetSingletonServiceInfo<RabbitMQServiceInfo>();

            DoAdd(services, info, config, contextLifetime);
            return services;
        }

        /// <summary>
        /// Add RabbitMQ and its IHealthContributor to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="serviceName">cloud foundry service name binding</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">logger factory</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>RabbitMQ.Client.ConnectionFactory is retrievable as both ConnectionFactory and IConnectionFactory</remarks>
        public static IServiceCollection AddRabbitMQConnection(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
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

            RabbitMQServiceInfo info = config.GetRequiredServiceInfo<RabbitMQServiceInfo>(serviceName);

            DoAdd(services, info, config, contextLifetime);
            return services;
        }

        private static void DoAdd(IServiceCollection services, RabbitMQServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
        {
            Type rabbitMQInterfaceType = RabbitMQTypeLocator.IConnectionFactory;
            Type rabbitMQImplementationType = RabbitMQTypeLocator.ConnectionFactory;

            RabbitMQProviderConnectorOptions rabbitMQConfig = new RabbitMQProviderConnectorOptions(config);
            RabbitMQProviderConnectorFactory factory = new RabbitMQProviderConnectorFactory(info, rabbitMQConfig, rabbitMQImplementationType);
            services.Add(new ServiceDescriptor(rabbitMQInterfaceType, factory.Create, contextLifetime));
            services.Add(new ServiceDescriptor(rabbitMQImplementationType, factory.Create, contextLifetime));
            services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RabbitMQHealthContributor(factory, ctx.GetService<ILogger<RabbitMQHealthContributor>>()), ServiceLifetime.Singleton));
        }
    }
}
