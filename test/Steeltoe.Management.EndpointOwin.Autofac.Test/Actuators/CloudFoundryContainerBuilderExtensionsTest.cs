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
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Autofac.Actuators.Test
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
