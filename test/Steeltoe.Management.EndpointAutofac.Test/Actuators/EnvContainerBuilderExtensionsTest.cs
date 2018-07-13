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
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.EndpointOwin;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointAutofac.Actuators.Test
{
    public class EnvContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterEnvMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            IConfigurationRoot config = new ConfigurationBuilder().Build();

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
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

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
