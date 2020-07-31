// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class EnvContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterEnvMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            var containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            var config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => EnvContainerBuilderExtensions.RegisterEnvActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => EnvContainerBuilderExtensions.RegisterEnvActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterEnvMiddleware_RegistersComponents()
        {
            // Arrange
            var containerBuilder = new ContainerBuilder();
            var config = new ConfigurationBuilder().Build();

            // Act
            EnvContainerBuilderExtensions.RegisterEnvActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<IEnvOptions>(), "Env options are registered");
            Assert.True(container.IsRegistered<IHostingEnvironment>(), "IHostingEnvironment is registered");
            Assert.True(container.IsRegistered<IEndpoint<EnvironmentDescriptor>>(), "Env endpoint is registered");
            Assert.True(container.IsRegistered<EndpointOwinMiddleware<EnvironmentDescriptor>>(), "Env endpoint middleware is registered");
        }

        ////[Fact]
        ////public void RegisterEnvModule_ThrowsOnNulls()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerNull = null;
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot configNull = null;
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    var ex = Assert.Throws<ArgumentNullException>(() => EnvContainerBuilderExtensions.RegisterEnvModule(containerNull, config));
        ////    var ex2 = Assert.Throws<ArgumentNullException>(() => EnvContainerBuilderExtensions.RegisterEnvModule(containerBuilder, configNull));

        ////    // Assert
        ////    Assert.Equal("container", ex.ParamName);
        ////    Assert.Equal("config", ex2.ParamName);
        ////}

        ////[Fact]
        ////public void RegisterEnvModule_RegistersComponents()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    EnvContainerBuilderExtensions.RegisterEnvModule(containerBuilder, config);
        ////    var container = containerBuilder.Build();

        ////    // Assert
        ////    Assert.True(container.IsRegistered<IEnvOptions>(), "Env options are registered");
        ////    Assert.True(container.IsRegistered<IHostingEnvironment>(), "IHostingEnvironment is registered");
        ////    Assert.True(container.IsRegistered<IEndpoint<EnvironmentDescriptor>>(), "Env endpoint is registered");
        ////    Assert.True(container.IsRegistered<IHttpModule>(), "Env HttpModule is registered");
        ////}
    }
}
