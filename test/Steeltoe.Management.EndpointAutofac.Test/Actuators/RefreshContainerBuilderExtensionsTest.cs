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
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.EndpointAutofac.Actuators.Test
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
