// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Hystrix;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorAutofac.Test
{
    public class HystrixContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterHystrixConnection_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixConnection(null, config));
        }

        [Fact]
        public void RegisterHystrixConnection_Requires_Config()
        {
            // arrange
            ContainerBuilder cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixConnection(cb, null));
        }

        [Fact]
        public void RegisterHystrixConnection_AddsToContainer()
        {
            // arrange
            ContainerBuilder container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = HystrixContainerBuilderExtensions.RegisterHystrixConnection(container, config);
            var services = container.Build();
            var hystrixFactory = services.Resolve<HystrixConnectionFactory>();

            // assert
            Assert.NotNull(hystrixFactory);
            Assert.IsType<HystrixConnectionFactory>(hystrixFactory);
        }
    }
}
