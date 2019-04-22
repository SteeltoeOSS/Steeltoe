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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using Xunit;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test
{
    public class EndpointServiceCollectionTest : BaseTest
    {
        [Fact]
        public void AddCloudFoundryActuator_ThrowsOnNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddCloudFoundryActuator(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddCloudFoundryActuator(services2, config));
            Assert.Contains(nameof(config), ex2.Message);

        }

        [Fact]
        public void AddCloudFoundryActuator_AddsCorrectServices()
        {
            ServiceCollection services = new ServiceCollection();
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': false,
            'path': '/cloudfoundryapplication',
            'info' : {
                'enabled': true,
                'sensitive': false
            }
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

            services.AddCloudFoundryActuator(config);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<ICloudFoundryOptions>();
            Assert.NotNull(options);
            var ep = serviceProvider.GetService<CloudFoundryEndpoint>();
            Assert.NotNull(ep);

        }
    }
}
