// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class CloudFoundryContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterCloudFoundryMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryContainerBuilderExtensions.RegisterCloudFoundryActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => CloudFoundryContainerBuilderExtensions.RegisterCloudFoundryActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterCloudFoundryMiddleware_RegistersComponents()
        {
            // Arrange
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            CloudFoundryContainerBuilderExtensions.RegisterCloudFoundryActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<ICloudFoundryOptions>(), "Cloud Foundry options are registered");
            Assert.True(container.IsRegistered<CloudFoundryEndpoint>(), "Cloud Foundry endpoint is registered");
            Assert.True(container.IsRegistered<CloudFoundryEndpointOwinMiddleware>(), "Cloud Foundry endpoint middleware is registered");
        }

        [Fact]
        public void RegisterCloudFoundrySecurityMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryContainerBuilderExtensions.RegisterCloudFoundrySecurityMiddleware(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => CloudFoundryContainerBuilderExtensions.RegisterCloudFoundrySecurityMiddleware(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterCloudFoundrySecurityMiddleware_RegistersComponents()
        {
            // Arrange
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            CloudFoundryContainerBuilderExtensions.RegisterCloudFoundrySecurityMiddleware(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<CloudFoundrySecurityOwinMiddleware>(), "Cloud Foundry endpoint middleware is registered");
        }

        ////[Fact]
        ////public void RegisterCloudFoundryModule_ThrowsOnNulls()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerNull = null;
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot configNull = null;
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryContainerBuilderExtensions.RegisterCloudFoundryModule(containerNull, config));
        ////    var ex2 = Assert.Throws<ArgumentNullException>(() => CloudFoundryContainerBuilderExtensions.RegisterCloudFoundryModule(containerBuilder, configNull));

        ////    // Assert
        ////    Assert.Equal("container", ex.ParamName);
        ////    Assert.Equal("config", ex2.ParamName);
        ////}

        ////[Fact]
        ////public void RegisterCloudFoundryModule_RegistersComponents()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    CloudFoundryContainerBuilderExtensions.RegisterCloudFoundryModule(containerBuilder, config);
        ////    var container = containerBuilder.Build();

        ////    // Assert
        ////    Assert.True(container.IsRegistered<ICloudFoundryOptions>(), "Cloud Foundry options are registered");
        ////    Assert.True(container.IsRegistered<CloudFoundryEndpoint>(), "Cloud Foundry endpoint is registered");
        ////    Assert.True(container.IsRegistered<IHttpModule>(), "Cloud Foundry HttpModule is registered");
        ////}
    }
}
