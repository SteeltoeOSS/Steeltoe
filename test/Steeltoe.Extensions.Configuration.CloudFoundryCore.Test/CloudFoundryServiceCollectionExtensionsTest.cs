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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class CloudFoundryServiceCollectionExtensionsTest
    {
        [Fact]
        public void ConfigureCloudFoundryOptions_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.ConfigureCloudFoundryOptions(services, config));
            Assert.Contains(nameof(services), ex.Message);
        }

        [Fact]
        public void ConfigureCloudFoundryOptions_ThrowsIfConfigurtionNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.ConfigureCloudFoundryOptions(services, config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void ConfigureCloudFoundryOptions_ConfiguresCloudFoundryOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act and Assert
            var builder = new ConfigurationBuilder().AddCloudFoundry();
            var config = builder.Build();
            CloudFoundryServiceCollectionExtensions.ConfigureCloudFoundryOptions(services, config);

            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetService<IOptions<CloudFoundryApplicationOptions>>();
            Assert.NotNull(app);
            var service = serviceProvider.GetService<IOptions<CloudFoundryServicesOptions>>();
            Assert.NotNull(service);
        }
    }
}
