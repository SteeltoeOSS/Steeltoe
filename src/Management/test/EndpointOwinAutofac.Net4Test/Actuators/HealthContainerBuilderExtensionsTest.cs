// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Health;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class HealthContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterHealthMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            var containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            var config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => HealthContainerBuilderExtensions.RegisterHealthActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => HealthContainerBuilderExtensions.RegisterHealthActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterHealthMiddleware_RegistersComponents()
        {
            // Arrange
            var containerBuilder = new ContainerBuilder();
            var config = new ConfigurationBuilder().Build();

            // Act
            HealthContainerBuilderExtensions.RegisterHealthActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<IHealthOptions>(), "Health options are registered");
            Assert.True(container.IsRegistered<IHealthContributor>(), "At least one health contributor registered");
            Assert.True(container.IsRegistered<HealthEndpoint>(), "Health endpoint is registered");
            Assert.True(container.IsRegistered<HealthEndpointOwinMiddleware>(), "Env endpoint middleware is registered");
        }

        ////[Fact]
        ////public void RegisterHealthModule_ThrowsOnNulls()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerNull = null;
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot configNull = null;
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    var ex = Assert.Throws<ArgumentNullException>(() => HealthContainerBuilderExtensions.RegisterHealthModule(containerNull, config));
        ////    var ex2 = Assert.Throws<ArgumentNullException>(() => HealthContainerBuilderExtensions.RegisterHealthModule(containerBuilder, configNull));

        ////    // Assert
        ////    Assert.Equal("container", ex.ParamName);
        ////    Assert.Equal("config", ex2.ParamName);
        ////}

        ////[Fact]
        ////public void RegisterHealthModule_RegistersComponents()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    HealthContainerBuilderExtensions.RegisterHealthModule(containerBuilder, config);
        ////    var container = containerBuilder.Build();

        ////    // Assert
        ////    Assert.True(container.IsRegistered<IHealthOptions>(), "Health options are registered");
        ////    Assert.True(container.IsRegistered<IEndpoint<HealthCheckResult>>(), "Health endpoint is registered"); // REVIEW this should probably be registered as HealthEndpoint
        ////    Assert.True(container.IsRegistered<IHealthContributor>(), "At least one health contributor registered");
        ////    Assert.True(container.IsRegistered<IHttpModule>(), "Health HttpModule is registered");
        ////}
    }
}
