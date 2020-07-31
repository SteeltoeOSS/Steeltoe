// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class InfoContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterInfoMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            var containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            var config = new ConfigurationBuilder().Build();

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
            var containerBuilder = new ContainerBuilder();
            var config = new ConfigurationBuilder().Build();

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
