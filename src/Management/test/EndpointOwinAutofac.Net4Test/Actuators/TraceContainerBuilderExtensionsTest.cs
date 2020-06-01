// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.EndpointOwin;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators.Test
{
    public class TraceContainerBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void RegisterTraceMiddleware_ThrowsOnNulls()
        {
            // Arrange
            ContainerBuilder containerNull = null;
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot configNull = null;
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => TraceContainerBuilderExtensions.RegisterTraceActuator(containerNull, config));
            var ex2 = Assert.Throws<ArgumentNullException>(() => TraceContainerBuilderExtensions.RegisterTraceActuator(containerBuilder, configNull));

            // Assert
            Assert.Equal("container", ex.ParamName);
            Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void RegisterTraceMiddleware_RegistersComponents()
        {
            // Arrange
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            TraceContainerBuilderExtensions.RegisterTraceActuator(containerBuilder, config);
            var container = containerBuilder.Build();

            // Assert
            Assert.True(container.IsRegistered<ITraceOptions>(), "Trace options are registered");
            Assert.True(container.IsRegistered<ITraceRepository>(), "ITraceRepository is registered");
            Assert.True(container.IsRegistered<IEndpoint<List<TraceResult>>>(), "Trace endpoint is registered");
            Assert.True(container.IsRegistered<EndpointOwinMiddleware<List<TraceResult>>>(), "Trace endpoint middleware is registered");
        }
    }
}
