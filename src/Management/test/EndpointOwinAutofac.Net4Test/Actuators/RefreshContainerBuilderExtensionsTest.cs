// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class RefreshContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterRefreshMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => RefreshContainerBuilderExtensions.RegisterRefreshActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => RefreshContainerBuilderExtensions.RegisterRefreshActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterRefreshMiddleware_RegistersComponents()
        {
            // Arrange
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            RefreshContainerBuilderExtensions.RegisterRefreshActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<IRefreshOptions>(), "Refresh options are registered");
            Assert.True(container.IsRegistered<IEndpoint<IList<string>>>(), "Refresh endpoint is registered");
            Assert.True(container.IsRegistered<EndpointOwinMiddleware<IList<string>>>(), "Refresh endpoint middleware is registered");
        }
    }
}
