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
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.ThreadDump;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Autofac.Actuators.Test
{
    public class ThreadDumpContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterThreadDumpMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => ThreadDumpContainerBuilderExtensions.RegisterThreadDumpActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => ThreadDumpContainerBuilderExtensions.RegisterThreadDumpActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterThreadDumpMiddleware_RegistersComponents()
        {
            // Arrange
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            ThreadDumpContainerBuilderExtensions.RegisterThreadDumpActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<IThreadDumpOptions>(), "ThreadDump options are registered");
            Assert.True(container.IsRegistered<IThreadDumper>(), "ThreadDumper is registered");
            Assert.True(container.IsRegistered<IEndpoint<List<ThreadInfo>>>(), "ThreadDump endpoint is registered");
            Assert.True(container.IsRegistered<EndpointOwinMiddleware<List<ThreadInfo>>>(), "ThreadDump endpoint middleware is registered");
        }
    }
}
