// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.RabbitMQ;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using System;

namespace Steeltoe.CloudFoundry.ConnectorAutofac
{
    public static class RabbitMQContainerBuilderExtensions
    {
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

            Type rabbitMQInterfaceType = RabbitMQTypeLocator.IConnectionFactory;
            Type rabbitMQImplementationType = RabbitMQTypeLocator.ConnectionFactory;

            RabbitMQServiceInfo info = serviceName == null
                ? config.GetSingletonServiceInfo<RabbitMQServiceInfo>()
                : config.GetRequiredServiceInfo<RabbitMQServiceInfo>(serviceName);

            RabbitMQProviderConnectorOptions rabbitMQConfig = new RabbitMQProviderConnectorOptions(config);
            RabbitMQProviderConnectorFactory factory = new RabbitMQProviderConnectorFactory(info, rabbitMQConfig, rabbitMQImplementationType);
            container.Register(c => new RabbitMQHealthContributor(factory, c.ResolveOptional<ILogger<RabbitMQHealthContributor>>())).As<IHealthContributor>();

            return container.Register(c => factory.Create(null)).As(rabbitMQInterfaceType, rabbitMQImplementationType);
        }
    }
}
