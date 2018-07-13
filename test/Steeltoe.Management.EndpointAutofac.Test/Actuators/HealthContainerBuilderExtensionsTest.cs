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
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.EndpointOwin.Health;
using System;
using System.Web;
using Xunit;

namespace Steeltoe.Management.EndpointAutofac.Actuators.Test
{
    public class HealthContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterHealthMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => HealthContainerBuilderExtensions.RegisterHealthActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => HealthContainerBuilderExtensions.RegisterHealthActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterHealthMiddleware_RegistersComponents()
        {
            // Arrange
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            HealthContainerBuilderExtensions.RegisterHealthActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<IHealthOptions>(), "Health options are registered");
            Assert.True(container.IsRegistered<IHealthContributor>(), "At least one health contributor registered");
            Assert.True(container.IsRegistered<HealthEndpoint>(), "Health endpoint is registered");
            Assert.True(container.IsRegistered<HealthEndpointOwinMiddleware>(), "Env endpoint middleware is registered");
        }

        ////[Fact]
        ////public void RegisterHealthModule_ThrowsOnNulls()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerNull = null;
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot configNull = null;
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    var ex = Assert.Throws<ArgumentNullException>(() => HealthContainerBuilderExtensions.RegisterHealthModule(containerNull, config));
        ////    var ex2 = Assert.Throws<ArgumentNullException>(() => HealthContainerBuilderExtensions.RegisterHealthModule(containerBuilder, configNull));

        ////    // Assert
        ////    Assert.Equal("container", ex.ParamName);
        ////    Assert.Equal("config", ex2.ParamName);
        ////}

        ////[Fact]
        ////public void RegisterHealthModule_RegistersComponents()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    HealthContainerBuilderExtensions.RegisterHealthModule(containerBuilder, config);
        ////    var container = containerBuilder.Build();

        ////    // Assert
        ////    Assert.True(container.IsRegistered<IHealthOptions>(), "Health options are registered");
        ////    Assert.True(container.IsRegistered<IEndpoint<HealthCheckResult>>(), "Health endpoint is registered"); // REVIEW this should probably be registered as HealthEndpoint
        ////    Assert.True(container.IsRegistered<IHealthContributor>(), "At least one health contributor registered");
        ////    Assert.True(container.IsRegistered<IHttpModule>(), "Health HttpModule is registered");
        ////}
    }
}
