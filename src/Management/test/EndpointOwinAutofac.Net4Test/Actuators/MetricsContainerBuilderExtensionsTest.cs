// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Metrics;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class MetricsContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterMetricsMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => MetricsContainerBuilderExtensions.RegisterMetricsActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => MetricsContainerBuilderExtensions.RegisterMetricsActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterMetricsMiddleware_RegistersComponents()
        {
            // Arrange
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            MetricsContainerBuilderExtensions.RegisterMetricsActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<IMetricsOptions>(), "Metrics options are registered");
            Assert.True(container.IsRegistered<MetricsEndpoint>(), "Metrics endpoint is registered");
            Assert.True(container.IsRegistered<MetricsEndpointOwinMiddleware>(), "Metrics endpoint middleware is registered");
        }
    }
}
