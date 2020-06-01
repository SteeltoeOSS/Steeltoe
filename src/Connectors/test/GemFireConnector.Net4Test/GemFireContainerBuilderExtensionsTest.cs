// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Apache.Geode.Client;
using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.ConnectorAutofac;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test
{
    public class GemFireContainerBuilderExtensionsTest
    {
        public GemFireContainerBuilderExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void RegisterGemFireConnection_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => GemFireContainerBuilderExtensions.RegisterGemFireConnection(null, config, typeof(BasicAuthInitialize)));
        }

        [Fact]
        public void RegisterGemFireConnection_Requires_Config()
        {
            // arrange
            ContainerBuilder cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => GemFireContainerBuilderExtensions.RegisterGemFireConnection(cb, null, typeof(BasicAuthInitialize)));
        }

        [Fact]
        public void RegisterGemFireConnection_NoVCAPs_RegistersGemFire()
        {
            // arrange
            ContainerBuilder containerBuilder = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            GemFireContainerBuilderExtensions.RegisterGemFireConnection(containerBuilder, config, typeof(BasicAuthInitialize));
            var container = containerBuilder.Build();
            var cacheFactory = container.Resolve<CacheFactory>();
            var poolFactory = container.Resolve<PoolFactory>();

            // assert
            Assert.NotNull(cacheFactory);
            Assert.IsType<CacheFactory>(cacheFactory);
            Assert.NotNull(poolFactory);
            Assert.IsType<PoolFactory>(poolFactory);
        }

        [Fact]
        public void RegisterGemFireConnection_WithVCAPS_RegistersGemFire()
        {
            // arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", GemFireTestHelpers.SingleBinding_PivotalCloudCache_DevPlan_VCAP);
            ContainerBuilder containerBuilder = new ContainerBuilder();
            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddCloudFoundry();
            var config = configBuilder.Build();

            // act
            GemFireContainerBuilderExtensions.RegisterGemFireConnection(containerBuilder, config, typeof(BasicAuthInitialize));
            var container = containerBuilder.Build();
            var cacheFactory = container.Resolve<CacheFactory>();
            var poolFactory = container.Resolve<PoolFactory>();

            // assert
            Assert.NotNull(cacheFactory);
            Assert.IsType<CacheFactory>(cacheFactory);
            Assert.NotNull(poolFactory);
            Assert.IsType<PoolFactory>(poolFactory);
        }

        [Fact]
        public void RegisterGemFireConnection_WithMultipleBindingsAndNoServiceName_Throws()
        {
            // arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", GemFireTestHelpers.MultipleBindings_PivotalCloudCache_VCAP);
            ContainerBuilder containerBuilder = new ContainerBuilder();
            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddCloudFoundry();
            var config = configBuilder.Build();

            // act
            var exception = Assert.Throws<ConnectorException>(() => GemFireContainerBuilderExtensions.RegisterGemFireConnection(containerBuilder, config, typeof(BasicAuthInitialize)));

            // assert
            Assert.Contains("Multiple", exception.Message);
        }

        [Fact]
        public void RegisterGemFireConnection_WithMultipleBindingsAndServiceName_RegistersGemFire()
        {
            // arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", GemFireTestHelpers.MultipleBindings_PivotalCloudCache_VCAP);
            ContainerBuilder containerBuilder = new ContainerBuilder();
            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddCloudFoundry();
            var config = configBuilder.Build();

            // act
            GemFireContainerBuilderExtensions.RegisterGemFireConnection(containerBuilder, config, typeof(BasicAuthInitialize), "pcc-dev");
            var container = containerBuilder.Build();
            var cacheFactory = container.Resolve<CacheFactory>();
            var poolFactory = container.Resolve<PoolFactory>();

            // assert
            Assert.NotNull(cacheFactory);
            Assert.IsType<CacheFactory>(cacheFactory);
            Assert.NotNull(poolFactory);
            Assert.IsType<PoolFactory>(poolFactory);
        }
    }
}
