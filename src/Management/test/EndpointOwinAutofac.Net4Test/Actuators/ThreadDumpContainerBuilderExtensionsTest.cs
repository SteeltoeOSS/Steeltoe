// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.EndpointOwin;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
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
