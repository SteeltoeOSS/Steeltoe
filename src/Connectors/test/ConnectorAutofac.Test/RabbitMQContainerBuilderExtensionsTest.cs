// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Steeltoe.CloudFoundry.Connector.RabbitMQ;
using Steeltoe.Common.HealthChecks;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorAutofac.Test
{
    public class RabbitMQContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterRabbitMQConnection_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => RabbitMQContainerBuilderExtensions.RegisterRabbitMQConnection(null, config));
        }

        [Fact]
        public void RegisterRabbitMQConnection_Requires_Config()
        {
            // arrange
            var cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => RabbitMQContainerBuilderExtensions.RegisterRabbitMQConnection(cb, null));
        }

        [Fact]
        public void RegisterRabbitMQConnection_AddsToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = RabbitMQContainerBuilderExtensions.RegisterRabbitMQConnection(container, config);
            var services = container.Build();
            var rabbitMQIFactory = services.Resolve<IConnectionFactory>();
            var rabbitMQFactory = services.Resolve<ConnectionFactory>();

            // assert
            Assert.NotNull(rabbitMQIFactory);
            Assert.NotNull(rabbitMQFactory);
            Assert.IsType<ConnectionFactory>(rabbitMQIFactory);
            Assert.IsType<ConnectionFactory>(rabbitMQFactory);
        }

        [Fact]
        public void RegisterRabbitMQConnection_AddsHealthContributorToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = RabbitMQContainerBuilderExtensions.RegisterRabbitMQConnection(container, config);
            var services = container.Build();
            var healthContributor = services.Resolve<IHealthContributor>();

            // assert
            Assert.NotNull(healthContributor);
            Assert.IsType<RabbitMQHealthContributor>(healthContributor);
        }
    }
}
