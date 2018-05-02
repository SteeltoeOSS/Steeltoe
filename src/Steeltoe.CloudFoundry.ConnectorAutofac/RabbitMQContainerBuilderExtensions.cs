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
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.RabbitMQ;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.CloudFoundry.ConnectorBase.Queue;
using Steeltoe.Management.Endpoint.Health;
using System;

namespace Steeltoe.CloudFoundry.ConnectorAutofac
{
    public static class RabbitMQContainerBuilderExtensions
    {
        private static string[] rabbitMQAssemblies = new string[] { "RabbitMQ.Client" };
        private static string[] rabbitMQInterfaceTypeNames = new string[] { "RabbitMQ.Client.IConnectionFactory" };
        private static string[] rabbitMQImplementationTypeNames = new string[] { "RabbitMQ.Client.ConnectionFactory" };

        /// <summary>
        /// Adds RabbitMQ ConnectionFactory (as IConnectionFactory and ConnectionFactory) to your Autofac Container
        /// </summary>
        /// <param name="container">Your Autofac Container Builder</param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">Cloud Foundry service name binding</param>
        /// <returns>the RegistrationBuilder for (optional) additional configuration</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterRabbitMQConnection(this ContainerBuilder container, IConfiguration config, string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Type rabbitMQInterfaceType = ConnectorHelpers.FindType(rabbitMQAssemblies, rabbitMQInterfaceTypeNames);
            Type rabbitMQImplementationType = ConnectorHelpers.FindType(rabbitMQAssemblies, rabbitMQImplementationTypeNames);
            if (rabbitMQInterfaceType == null || rabbitMQImplementationType == null)
            {
                throw new ConnectorException("Unable to find ConnectionFactory, are you missing RabbitMQ assembly");
            }

            RabbitMQServiceInfo info;
            if (serviceName == null)
            {
                info = config.GetSingletonServiceInfo<RabbitMQServiceInfo>();
            }
            else
            {
                info = config.GetRequiredServiceInfo<RabbitMQServiceInfo>(serviceName);
            }

            RabbitMQProviderConnectorOptions rabbitMQConfig = new RabbitMQProviderConnectorOptions(config);
            RabbitMQProviderConnectorFactory factory = new RabbitMQProviderConnectorFactory(info, rabbitMQConfig, rabbitMQImplementationType);
            container.Register(c => new RabbitMQHealthContributor(factory, c.Resolve<ILogger<RabbitMQHealthContributor>>())).As<IHealthContributor>();

            return container.Register(c => factory.Create(null)).As(rabbitMQInterfaceType, rabbitMQImplementationType);
        }
    }
}
