//
// Copyright 2015 the original author or authors.
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
//

using SteelToe.Discovery.Eureka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace SteelToe.Discovery.Client.Test
{
    public class DiscoveryServiceCollectionExtensionsTest : AbstractBaseTest
    {
        public DiscoveryServiceCollectionExtensionsTest()
        {
            ApplicationInfoManager.Instance.InstanceInfo = null;
            ApplicationInfoManager.Instance.InstanceConfig = null;
            DiscoveryManager.Instance.ClientConfig = null;
            DiscoveryManager.Instance.Client = null;
            DiscoveryManager.Instance.InstanceConfig = null;
        }

        [Fact]
        public void AddDiscoveryClient_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;
            DiscoveryOptions options = null;
            Action<DiscoveryOptions> action = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryServiceCollectionExtensions.AddDiscoveryClient(services, config));
            Assert.Contains(nameof(services), ex.Message);
            ex = Assert.Throws<ArgumentNullException>(() => DiscoveryServiceCollectionExtensions.AddDiscoveryClient(services, options));
            Assert.Contains(nameof(services), ex.Message);
            ex = Assert.Throws<ArgumentNullException>(() => DiscoveryServiceCollectionExtensions.AddDiscoveryClient(services, action));
            Assert.Contains(nameof(services), ex.Message);
        }

        [Fact]
        public void AddDiscoveryClient_ThrowsIfConfigurtionNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryServiceCollectionExtensions.AddDiscoveryClient(services, config));
            Assert.Contains(nameof(config), ex.Message);

        }

        [Fact]
        public void AddDiscoverClient_ThrowsIfDiscoveryOptionsNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            DiscoveryOptions discoveryOptions = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryServiceCollectionExtensions.AddDiscoveryClient(services, discoveryOptions));
            Assert.Contains(nameof(discoveryOptions), ex.Message);

        }
        [Fact]
        public void AddDiscoverClient_ThrowsIfDiscoveryOptionsClientType_Unknown()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            DiscoveryOptions discoveryOptions = new DiscoveryOptions();

            // Act and Assert
            var ex = Assert.Throws<ArgumentException>(() => DiscoveryServiceCollectionExtensions.AddDiscoveryClient(services, discoveryOptions));
            Assert.Contains("UNKNOWN", ex.Message);

        }

        [Fact]
        public void AddDiscoveryClient_ThrowsIfSetupOptionsNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            Action<DiscoveryOptions> setupOptions = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryServiceCollectionExtensions.AddDiscoveryClient(services, setupOptions));
            Assert.Contains(nameof(setupOptions), ex.Message);

        }

        [Fact]
        public void AddDiscoveryClient_WithEurekaConfig_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = @"
{
    'spring': {
        'application': {
            'name': 'myName'
        },
    },
    'eureka': {
        'client': {
            'shouldFetchRegistry': false,
            'shouldRegisterWithEureka': false,
            'serviceUrl': 'http://localhost:8761/eureka/'
        }
    }
}";


            var path = TestHelpers.CreateTempFile(appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(path);
            var config = configurationBuilder.Build();

            var services = new ServiceCollection();
            services.AddDiscoveryClient(config);

            var service = services.BuildServiceProvider().GetService<IDiscoveryClient>();
            Assert.NotNull(service);

        }

        [Fact]
        public void AddDiscoveryClient_WithNoEurekaConfig_AddsDiscoveryClient_UnknownClientType()
        {
            // Arrange
            var appsettings = @"
{
    'spring': {
        'application': {
            'name': 'myName'
        },
    }
}";

            var path = TestHelpers.CreateTempFile(appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(path);
            var config = configurationBuilder.Build();

            var services = new ServiceCollection();
            services.AddDiscoveryClient(config);

            var service = services.BuildServiceProvider().GetService<IDiscoveryClient>();
            Assert.NotNull(service);
            Assert.Equal("Unknown", service.Description);

        }

        [Fact]
        public void AddDiscoveryClient_WithDiscoveryOptions_AddsDiscoveryClient()
        {
            // Arrange
            DiscoveryOptions options = new DiscoveryOptions()
            {
                ClientType = DiscoveryClientType.EUREKA,
                ClientOptions = new EurekaClientOptions()
                {
                    ShouldFetchRegistry = false,
                    ShouldRegisterWithEureka = false
                }

            };

            var services = new ServiceCollection();
            services.AddDiscoveryClient(options);

            var service = services.BuildServiceProvider().GetService<IDiscoveryClient>();
            Assert.NotNull(service);

        }

        [Fact]
        public void AddDiscoveryClient_WithDiscoveryOptions_MissingOptions_AddsDiscoveryClient()
        {
            // Arrange
            DiscoveryOptions options = new DiscoveryOptions()
            {
                ClientType = DiscoveryClientType.EUREKA,
                ClientOptions = null,
                RegistrationOptions = null

            };

            var services = new ServiceCollection();
            services.AddDiscoveryClient(options);

            var service = services.BuildServiceProvider().GetService<IDiscoveryClient>();
            Assert.NotNull(service);

        }
        [Fact]
        public void AddDiscoveryClient_WithSetupAction_AddsDiscoveryClient()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDiscoveryClient((options) =>
           {
               options.ClientType = DiscoveryClientType.EUREKA;
               options.ClientOptions = new EurekaClientOptions()
               {
                   ShouldFetchRegistry = false,
                   ShouldRegisterWithEureka = false
               };
               options.RegistrationOptions = new EurekaInstanceOptions();

           });

            var service = services.BuildServiceProvider().GetService<IDiscoveryClient>();
            Assert.NotNull(service);

        }
    }
}
