// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Metrics;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class MetricsContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterMetricsMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            var containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            var config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => MetricsContainerBuilderExtensions.RegisterMetricsActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => MetricsContainerBuilderExtensions.RegisterMetricsActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterMetricsMiddleware_RegistersComponents()
        {
            // Arrange
            var containerBuilder = new ContainerBuilder();
            var config = new ConfigurationBuilder().Build();

            // Act
            MetricsContainerBuilderExtensions.RegisterMetricsActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<IMetricsOptions>(), "Metrics options are registered");
            Assert.True(container.IsRegistered<MetricsEndpoint>(), "Metrics endpoint is registered");
            Assert.True(container.IsRegistered<MetricsEndpointOwinMiddleware>(), "Metrics endpoint middleware is registered");
        }
    }
}
