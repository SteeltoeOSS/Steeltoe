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

#if NET461

using Apache.Geode.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CloudFoundry.Connector.Test;
using Steeltoe.CloudFoundry.ConnectorCore.Test;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.GemFire.Test
{
    public class GemFireServiceCollectionExtensionsTest
    {
        public GemFireServiceCollectionExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void AddGemFireConnection_ThrowsIfServiceCollectionNull()
        {
           // arrange
            IServiceCollection services = null;
            IConfiguration config = new ConfigurationBuilder().Build();

            // actsert
            var exception = Assert.Throws<ArgumentNullException>(() => services.AddGemFireConnection(config, typeof(BasicAuthInitialize)));
            Assert.Equal(nameof(services), exception.ParamName);
        }

        [Fact]
        public void AddGemFireConnection_ThrowsIfConfigurationNull()
        {
            // arrange
            IServiceCollection services = new ServiceCollection();
            IConfiguration config = null;

            // actsert
            var exception = Assert.Throws<ArgumentNullException>(() => services.AddGemFireConnection(config, typeof(BasicAuthInitialize)));
        }

        [Fact]
        public void AddGemFireConnection_NoVCAPs_Adds()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            services.AddGemFireConnection(config, typeof(BasicAuthInitialize));
            var cacheFactory = services.BuildServiceProvider().GetService<CacheFactory>();
            var cache = services.BuildServiceProvider().GetService<Cache>();
            var poolFactory = services.BuildServiceProvider().GetService<PoolFactory>();

            // Assert
            Assert.NotNull(cacheFactory);
            Assert.NotNull(cache);
            Assert.NotNull(poolFactory);
        }

        [Fact]
        public void AddGemFireConnection_SingleVCAPService_Adds()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", GemFireTestHelpers.SingleBinding_PivotalCloudCache_DevPlan_VCAP);
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            services.AddGemFireConnection(config, typeof(BadConstructorAuthInitializer));
            var cacheFactory = services.BuildServiceProvider().GetService<CacheFactory>();
            var cache = services.BuildServiceProvider().GetService<Cache>();
            var poolFactory = services.BuildServiceProvider().GetService<PoolFactory>();

            // Assert
            Assert.NotNull(cacheFactory);
            Assert.NotNull(cache);
            Assert.NotNull(poolFactory);
        }

        [Fact]
        public void AddGemFireConnection_WithServiceName_NoVCAPs_Throws()
        {
            // arrange
            IServiceCollection services = new ServiceCollection();
            IConfiguration config = new ConfigurationBuilder().Build();

            // Actsert
            Assert.Throws<ConnectorException>(() => services.AddGemFireConnection(config, typeof(BasicAuthInitialize), "serviceName"));
        }

        [Fact]
        public void AddGemFireConnection_MultipleServices_NoServiceName_Throws()
        {
            // Arrange an environment where multiple GemFire services have been provisioned
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", GemFireTestHelpers.MultipleBindings_PivotalCloudCache_VCAP);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => services.AddGemFireConnection(config, typeof(BasicAuthInitialize)));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddGemFireConnection_MultipleServices_WithServiceName_Adds()
        {
            // Arrange an environment where multiple GemFire services have been provisioned
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", GemFireTestHelpers.MultipleBindings_PivotalCloudCache_VCAP);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // act
            services.AddGemFireConnection(config, typeof(BasicAuthInitialize), "pcc-dev");
            var cacheFactory = services.BuildServiceProvider().GetService<CacheFactory>();
            var cache = services.BuildServiceProvider().GetService<Cache>();
            var poolFactory = services.BuildServiceProvider().GetService<PoolFactory>();

            // Assert
            Assert.NotNull(cacheFactory);
            Assert.NotNull(cache);
            Assert.NotNull(poolFactory);
        }
    }
}
#endif
