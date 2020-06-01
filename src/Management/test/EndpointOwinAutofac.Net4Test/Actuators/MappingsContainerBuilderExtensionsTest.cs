// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Mappings;
using System;
using System.Web.Http;
using System.Web.Http.Description;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class MappingsContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterMappingsMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            ApiExplorer apiExplorerNull = null;
            ApiExplorer apiExplorer = new ApiExplorer(new HttpConfiguration());

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => MappingsContainerBuilderExtensions.RegisterMappingsActuator(containerNull, config, apiExplorer));
            var ex2 = Assert.Throws<ArgumentNullException>(() => MappingsContainerBuilderExtensions.RegisterMappingsActuator(containerBuilder, configNull, apiExplorer));
            var ex3 = Assert.Throws<ArgumentNullException>(() => MappingsContainerBuilderExtensions.RegisterMappingsActuator(containerBuilder, config, apiExplorerNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
            Assert.Equal("apiExplorer", ex3.ParamName);
        }

        [Fact]
        public void RegisterMappingsMiddleware_RegistersComponents()
        {
            // Arrange
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            ApiExplorer apiExplorer = new ApiExplorer(new HttpConfiguration());

            // Act
            MappingsContainerBuilderExtensions.RegisterMappingsActuator(containerBuilder, config, apiExplorer);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<IMappingsOptions>(), "Mappings options are registered");
            Assert.True(container.IsRegistered<MappingsEndpoint>(), "Mappings endpoint is registered");
            Assert.True(container.IsRegistered<MappingsEndpointOwinMiddleware>(), "Mappings endpoint middleware is registered");
        }
    }
}
