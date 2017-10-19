//
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
//

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;
using System.IO;
using Steeltoe.Common.Discovery;
using System.Threading;
using Steeltoe.Discovery.Eureka;
using Autofac;
using Steeltoe.Common.Options.Autofac;

namespace Steeltoe.Discovery.Client.Test
{
    public class DiscoveryContainerBuilderExtensionsTest 
    {

        [Fact]
        public void RegisteriscoveryClient_ThrowsIfContainerNull()
        {
            // Arrange
            ContainerBuilder container = null;
            IConfigurationRoot config = null;
            DiscoveryOptions options = null;
            Action<DiscoveryOptions> action = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(container, config));
            Assert.Contains(nameof(container), ex.Message);
            ex = Assert.Throws<ArgumentNullException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(container, options));
            Assert.Contains(nameof(container), ex.Message);
            ex = Assert.Throws<ArgumentNullException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(container, action));
            Assert.Contains(nameof(container), ex.Message);
        }

        [Fact]
        public void RegisteriscoveryClient_ThrowsIfConfigurtionNull()
        {
            // Arrange
            ContainerBuilder services = new ContainerBuilder();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(services, config));
            Assert.Contains(nameof(config), ex.Message);

        }

        [Fact]
        public void RegisteriscoveryClient_ThrowsIfDiscoveryOptionsNull()
        {
            // Arrange
            ContainerBuilder services = new ContainerBuilder();
            DiscoveryOptions discoveryOptions = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(services, discoveryOptions));
            Assert.Contains(nameof(discoveryOptions), ex.Message);

        }
        [Fact]
        public void RegisteriscoveryClient_ThrowsIfDiscoveryOptionsClientType_Unknown()
        {
            // Arrange
            ContainerBuilder services = new ContainerBuilder();
            DiscoveryOptions discoveryOptions = new DiscoveryOptions();

            // Act and Assert
            var ex = Assert.Throws<ArgumentException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(services, discoveryOptions));
            Assert.Contains("UNKNOWN", ex.Message);

        }

        [Fact]
        public void RegisteriscoveryClient_ThrowsIfSetupOptionsNull()
        {
            // Arrange
            ContainerBuilder services = new ContainerBuilder();
            Action<DiscoveryOptions> setupOptions = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(services, setupOptions));
            Assert.Contains(nameof(setupOptions), ex.Message);

        }

        [Fact]
        public void RegisteriscoveryClient_WithEurekaConfig_AddsDiscoveryClient()
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
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            var services = new ContainerBuilder();
            services.RegisterOptions();
            services.RegisterDiscoveryClient(config);

            var service = services.Build().Resolve<IDiscoveryClient>();
            Assert.NotNull(service);

        }

        [Fact]
        public void RegisteriscoveryClient_WithNoEurekaConfig_AddsDiscoveryClient_UnknownClientType()
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
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            var services = new ContainerBuilder();
            Assert.Throws<ArgumentException>(() => services.RegisterDiscoveryClient(config));


        }

        [Fact]
        public void RegisteriscoveryClient_WithDiscoveryOptions_AddsDiscoveryClient()
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

            var services = new ContainerBuilder();

            services.RegisterDiscoveryClient(options);

            var service = services.Build().Resolve<IDiscoveryClient>();
            Assert.NotNull(service);

        }

        [Fact]
        public void RegisteriscoveryClient_WithDiscoveryOptions_MissingOptions_Throws()
        {
            // Arrange
            DiscoveryOptions options = new DiscoveryOptions()
            {
                ClientType = DiscoveryClientType.EUREKA,
                ClientOptions = null,
                RegistrationOptions = null

            };

            var services = new ContainerBuilder();
  
            Assert.Throws<ArgumentException>(() => services.RegisterDiscoveryClient(options));
        }

        [Fact]
        public void RegisteriscoveryClient_WithSetupAction_AddsDiscoveryClient()
        {
            // Arrange
            var services = new ContainerBuilder();

            services.RegisterDiscoveryClient((options) =>
           {
               options.ClientType = DiscoveryClientType.EUREKA;
               options.ClientOptions = new EurekaClientOptions()
               {
                   ShouldFetchRegistry = false,
                   ShouldRegisterWithEureka = false
               };
               options.RegistrationOptions = new EurekaInstanceOptions();

           });

            var service = services.Build().Resolve<IDiscoveryClient>();
            Assert.NotNull(service);

        }
    }
}
