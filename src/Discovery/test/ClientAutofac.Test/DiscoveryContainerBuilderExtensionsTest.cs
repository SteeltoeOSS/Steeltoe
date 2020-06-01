// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Options.Autofac;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Eureka;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace Steeltoe.Discovery.Client.Test
{
    public class DiscoveryContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterDiscoveryClient_ThrowsIfContainerNull()
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
        public void RegisterDiscoveryClient_ThrowsIfConfigurtionNull()
        {
            // Arrange
            ContainerBuilder services = new ContainerBuilder();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(services, config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void RegisterDiscoveryClient_ThrowsIfDiscoveryOptionsNull()
        {
            // Arrange
            ContainerBuilder services = new ContainerBuilder();
            DiscoveryOptions discoveryOptions = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(services, discoveryOptions));
            Assert.Contains(nameof(discoveryOptions), ex.Message);
        }

        [Fact]
        public void RegisterDiscoveryClient_ThrowsIfDiscoveryOptionsClientType_Unknown()
        {
            // Arrange
            ContainerBuilder services = new ContainerBuilder();
            DiscoveryOptions discoveryOptions = new DiscoveryOptions();

            // Act and Assert
            var ex = Assert.Throws<ArgumentException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(services, discoveryOptions));
            Assert.Contains("UNKNOWN", ex.Message);
        }

        [Fact]
        public void RegisterDiscoveryClient_ThrowsIfSetupOptionsNull()
        {
            // Arrange
            ContainerBuilder services = new ContainerBuilder();
            Action<DiscoveryOptions> setupOptions = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => DiscoveryContainerBuilderExtensions.RegisterDiscoveryClient(services, setupOptions));
            Assert.Contains(nameof(setupOptions), ex.Message);
        }

        [Fact]
        public void RegisterDiscoveryClient_WithEurekaConfig_AddsDiscoveryClient()
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
        public void RegisterDiscoveryClient_WithNoEurekaConfig_AddsDiscoveryClient_UnknownClientType()
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
        public void RegisterDiscoveryClient_WithDiscoveryOptions_AddsDiscoveryClient()
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
        public void RegisterDiscoveryClient_WithDiscoveryOptions_MissingOptions_Throws()
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
        public void RegisterDiscoveryClient_WithSetupAction_AddsDiscoveryClient()
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

        [Fact]
        public void RegisterDiscoveryClient_WithEurekaInet_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "spring:cloud:inet:defaulthostname", "fromtest" },
                { "spring:cloud:inet:skipReverseDnsLookup", "true" },
                { "eureka:client:shouldFetchRegistry", "false" },
                { "eureka:client:shouldRegisterWithEureka", "false" },
                { "eureka:instance:useNetUtils", "true" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ContainerBuilder();

            // act
            services.RegisterOptions();
            services.RegisterDiscoveryClient(config);
            var service = services.Build().Resolve<IDiscoveryClient>();

            // assert
            Assert.NotNull(service);
            Assert.IsAssignableFrom<EurekaDiscoveryClient>(service);
            Assert.Equal("fromtest", service.GetLocalServiceInstance().Host);
        }

        [Fact]
        public void RegisterDiscoveryClient_WithConsulInet_AddsDiscoveryClient()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>
            {
                { "spring:application:name", "myName" },
                { "spring:cloud:inet:defaulthostname", "fromtest" },
                { "spring:cloud:inet:skipReverseDnsLookup", "true" },
                { "consul:discovery:useNetUtils", "true" },
                { "consul:discovery:register", "false" },
                { "consul:discovery:deregister", "false" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ContainerBuilder();

            // act
            services.RegisterOptions();
            services.RegisterDiscoveryClient(config);
            var service = services.Build().Resolve<IDiscoveryClient>();

            // assert
            Assert.NotNull(service);
            Assert.IsAssignableFrom<ConsulDiscoveryClient>(service);
            Assert.Equal("fromtest", service.GetLocalServiceInstance().Host);
        }
    }
}
