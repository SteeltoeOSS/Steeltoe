// Copyright 2015 the original author or authors.
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
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.Rabbit
{
    public static class RabbitProviderServiceCollectionExtensions
    {
        private static string[] rabbitMQAssemblies = new string[] { "RabbitMQ.Client" };
        private static string[] rabbitMQInterfaceTypeNames = new string[] { "RabbitMQ.Client.IConnectionFactory" };
        private static string[] rabbitMQImplementationTypeNames = new string[] { "RabbitMQ.Client.ConnectionFactory" };

        /// <summary>
        /// Add RabbitMQ to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="registerInterface">Optionally disable registering the interface type with DI</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory"></param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddRabbitConnection(this IServiceCollection services, IConfiguration config, bool registerInterface = true, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RabbitServiceInfo info = config.GetSingletonServiceInfo<RabbitServiceInfo>();

            DoAdd(services, info, config, registerInterface, contextLifetime);
            return services;
        }

        /// <summary>
        /// Add RabbitMQ to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="serviceName"></param>
        /// <param name="registerInterface">Optionally disable registering the interface type with DI</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory"></param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddRabbitConnection(this IServiceCollection services, IConfiguration config, string serviceName, bool registerInterface = true, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
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

            RabbitServiceInfo info = config.GetRequiredServiceInfo<RabbitServiceInfo>(serviceName);

            DoAdd(services, info, config, registerInterface, contextLifetime);
            return services;
        }

        private static void DoAdd(IServiceCollection services, RabbitServiceInfo info, IConfiguration config, bool registerInterface, ServiceLifetime contextLifetime)
        {
            Type rabbitMQInterfaceType = ConnectorHelpers.FindType(rabbitMQAssemblies, rabbitMQInterfaceTypeNames);
            Type rabbitMQImplementationType = ConnectorHelpers.FindType(rabbitMQAssemblies, rabbitMQImplementationTypeNames);
            if (rabbitMQInterfaceType == null || rabbitMQImplementationType == null)
            {
                throw new ConnectorException("Unable to find ConnectionFactory, are you missing RabbitMQ assembly");
            }

            RabbitProviderConnectorOptions rabbitMQConfig = new RabbitProviderConnectorOptions(config);
            RabbitProviderConnectorFactory factory = new RabbitProviderConnectorFactory(info, rabbitMQConfig, rabbitMQImplementationType);
            if (registerInterface)
            {
                services.Add(new ServiceDescriptor(rabbitMQInterfaceType, factory.Create, contextLifetime));
            }

            services.Add(new ServiceDescriptor(rabbitMQImplementationType, factory.Create, contextLifetime));
        }
    }
}
