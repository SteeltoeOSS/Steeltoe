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
