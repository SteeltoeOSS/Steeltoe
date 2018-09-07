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
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Mappings;
using System;
using System.Web.Http;
using System.Web.Http.Description;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Autofac.Actuators.Test
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
