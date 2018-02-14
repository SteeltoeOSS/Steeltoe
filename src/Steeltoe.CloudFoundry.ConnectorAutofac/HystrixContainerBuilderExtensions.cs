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

using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Hystrix;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.ConnectorAutofac
{
    public static class HystrixContainerBuilderExtensions
    {
        private static string[] rabbitAssemblies = new string[] { "RabbitMQ.Client" };
        private static string[] rabbitTypeNames = new string[] { "RabbitMQ.Client.ConnectionFactory" };

        /// <summary>
        /// Add a HystrixConnectionFactory to your Autofac Container
        /// </summary>
        /// <param name="container">Your Autofac Container Builder</param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">Cloud Foundry service name binding</param>
        /// <returns>the RegistrationBuilder for (optional) additional configuration</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterHystrixConnection(this ContainerBuilder container, IConfiguration config, string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Type rabbitFactory = ConnectorHelpers.FindType(rabbitAssemblies, rabbitTypeNames);
            if (rabbitFactory == null)
            {
                throw new ConnectorException("Unable to find RabbitMQ ConnectionFactory type. RabbitMQ.Client assembly may be missing");
            }

            HystrixRabbitMQServiceInfo info;

            if (serviceName == null)
            {
                info = config.GetSingletonServiceInfo<HystrixRabbitMQServiceInfo>();
            }
            else
            {
                info = config.GetRequiredServiceInfo<HystrixRabbitMQServiceInfo>(serviceName);
            }

            HystrixProviderConnectorOptions hystrixConfig = new HystrixProviderConnectorOptions(config);
            HystrixProviderConnectorFactory factory = new HystrixProviderConnectorFactory(info, hystrixConfig, rabbitFactory);

            return container.Register(c => factory.Create(null)).As<HystrixConnectionFactory>();
        }
    }
}
