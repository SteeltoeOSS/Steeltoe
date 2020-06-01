// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Apache.Geode.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CloudFoundry.Connector.GemFire;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test
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