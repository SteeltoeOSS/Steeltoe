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
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.EndpointOwin;
using System;
using System.Collections.Generic;
using System.Web;
using Xunit;

namespace Steeltoe.Management.EndpointAutofac.Actuators.Test
{
    public class InfoContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterInfoMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => InfoContainerBuilderExtensions.RegisterInfoActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => InfoContainerBuilderExtensions.RegisterInfoActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterInfoMiddleware_RegistersComponents()
        {
            // Arrange
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            InfoContainerBuilderExtensions.RegisterInfoActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<IInfoOptions>(), "Info options are registered");
            Assert.True(container.IsRegistered<IInfoContributor>(), "At least one Info contributor registered");
            Assert.True(container.IsRegistered<IEndpoint<Dictionary<string, object>>>(), "Info endpoint is registered");
            Assert.True(container.IsRegistered<EndpointOwinMiddleware<Dictionary<string, object>>>(), "Env endpoint middleware is registered");
        }

        ////[Fact]
        ////public void RegisterInfoModule_ThrowsOnNulls()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerNull = null;
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot configNull = null;
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    var ex = Assert.Throws<ArgumentNullException>(() => InfoContainerBuilderExtensions.RegisterInfoModule(containerNull, config));
        ////    var ex2 = Assert.Throws<ArgumentNullException>(() => InfoContainerBuilderExtensions.RegisterInfoModule(containerBuilder, configNull));

        ////    // Assert
        ////    Assert.Equal("container", ex.ParamName);
        ////    Assert.Equal("config", ex2.ParamName);
        ////}

        ////[Fact]
        ////public void RegisterInfoModule_RegistersComponents()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    InfoContainerBuilderExtensions.RegisterInfoModule(containerBuilder, config);
        ////    var container = containerBuilder.Build();

        ////    // Assert
        ////    Assert.True(container.IsRegistered<IInfoOptions>(), "Info options are registered");
        ////    Assert.True(container.IsRegistered<InfoEndpoint>(), "Info endpoint is registered");
        ////    Assert.True(container.IsRegistered<IInfoContributor>(), "At least one Info contributor registered");
        ////    Assert.True(container.IsRegistered<IHttpModule>(), "Info HttpModule is registered");
        ////}
    }
}
