// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.HeapDump;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class HeapDumpContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterHeapDumpMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            var containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            var config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => HeapDumpContainerBuilderExtensions.RegisterHeapDumpActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => HeapDumpContainerBuilderExtensions.RegisterHeapDumpActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterHeapDumpMiddleware_RegistersComponents()
        {
            // Arrange
            var containerBuilder = new ContainerBuilder();
            var config = new ConfigurationBuilder().Build();

            // Act
            HeapDumpContainerBuilderExtensions.RegisterHeapDumpActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<IHeapDumpOptions>(), "HeapDump options are registered");
            Assert.True(container.IsRegistered<IHeapDumper>(), "HeapDumper is registered");
            Assert.True(container.IsRegistered<HeapDumpEndpoint>(), "HeapDump endpoint is registered");
            Assert.True(container.IsRegistered<HeapDumpEndpointOwinMiddleware>(), "Env endpoint middleware is registered");
        }

        ////[Fact]
        ////public void RegisterHeapDumpModule_ThrowsOnNulls()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerNull = null;
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot configNull = null;
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    var ex = Assert.Throws<ArgumentNullException>(() => HeapDumpContainerBuilderExtensions.RegisterHeapDumpModule(containerNull, config));
        ////    var ex2 = Assert.Throws<ArgumentNullException>(() => HeapDumpContainerBuilderExtensions.RegisterHeapDumpModule(containerBuilder, configNull));

        ////    // Assert
        ////    Assert.Equal("container", ex.ParamName);
        ////    Assert.Equal("config", ex2.ParamName);
        ////}

        ////[Fact]
        ////public void RegisterHeapDumpModule_RegistersComponents()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    HeapDumpContainerBuilderExtensions.RegisterHeapDumpModule(containerBuilder, config);
        ////    var container = containerBuilder.Build();

        ////    // Assert
        ////    Assert.True(container.IsRegistered<IHeapDumpOptions>(), "HeapDump options are registered");
        ////    Assert.True(container.IsRegistered<IHeapDumper>(), "HeapDumper is registered");
        ////    Assert.True(container.IsRegistered<HeapDumpEndpoint>(), "HeapDump endpoint is registered");
        ////    Assert.True(container.IsRegistered<IHttpModule>(), "HeapDump HttpModule is registered");
        ////}
    }
}
