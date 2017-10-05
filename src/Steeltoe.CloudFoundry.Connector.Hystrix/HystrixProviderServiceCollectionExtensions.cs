//
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
//

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.Hystrix
{
    public static class HystrixProviderServiceCollectionExtensions
    {
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

            HystrixRabbitServiceInfo info = config.GetSingletonServiceInfo<HystrixRabbitServiceInfo>();

            DoAdd(services, info, config, contextLifetime);
            return services;
        }

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

            HystrixRabbitServiceInfo info = config.GetRequiredServiceInfo<HystrixRabbitServiceInfo>(serviceName);

            DoAdd(services, info, config, contextLifetime);
            return services;
        }

        private static string[] rabbitAssemblies = new string[] { "RabbitMQ.Client" };
        private static string[] rabbitTypeNames = new string[] { "RabbitMQ.Client.ConnectionFactory" };

        private static void DoAdd(IServiceCollection services, HystrixRabbitServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
        {
            Type rabbitFactory = ConnectorHelpers.FindType(rabbitAssemblies, rabbitTypeNames);
            if (rabbitFactory == null)
            {
                throw new ConnectorException("Unable to find ConnectionFactory, are you missing RabbitMQ assembly");
            }

            HystrixProviderConnectorOptions hystrixConfig = new HystrixProviderConnectorOptions(config);
            HystrixProviderConnectorFactory factory = new HystrixProviderConnectorFactory(info, hystrixConfig, rabbitFactory);
            services.Add(new ServiceDescriptor(typeof(HystrixConnectionFactory), factory.Create, contextLifetime));
        }
    }
}
