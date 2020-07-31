// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Loggers;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class LoggersContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterLoggersMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            var containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            var config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => LoggersContainerBuilderExtensions.RegisterLoggersActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => LoggersContainerBuilderExtensions.RegisterLoggersActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterLoggersMiddleware_RegistersComponents()
        {
            // Arrange
            var containerBuilder = new ContainerBuilder();
            var config = new ConfigurationBuilder().Build();

            // Act
            LoggersContainerBuilderExtensions.RegisterLoggersActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<ILoggersOptions>(), "Loggers options are registered");
            Assert.True(container.IsRegistered<LoggersEndpoint>(), "Loggers endpoint is registered");
            Assert.True(container.IsRegistered<LoggersEndpointOwinMiddleware>(), "Loggers endpoint middleware is registered");
        }
    }
}
