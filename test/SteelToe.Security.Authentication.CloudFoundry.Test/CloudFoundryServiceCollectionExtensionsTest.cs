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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.CloudFoundry.Connector.OAuth;
using System;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddCloudFoundryAuthentication_ThowsForNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.AddCloudFoundryAuthentication(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.AddCloudFoundryAuthentication(new ServiceCollection(), config));
            Assert.Contains(nameof(config), ex2.Message);

        }
        [Fact]
        public void AddCloudFoundryAuthentication_AddsRequiredServices()
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            var services = new ServiceCollection();
            CloudFoundryServiceCollectionExtensions.AddCloudFoundryAuthentication(services, config);
            var provider = services.BuildServiceProvider();
            var iopts = provider.GetService(typeof(IOptions<OAuthServiceOptions>)) as IOptions<OAuthServiceOptions>;
            Assert.NotNull(iopts);

        }
        [Fact]
        public void AddCloudFoundryJwtAuthentication_ThowsForNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.AddCloudFoundryJwtAuthentication(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.AddCloudFoundryJwtAuthentication(new ServiceCollection(), config));
            Assert.Contains(nameof(config), ex2.Message);

        }
        [Fact]
        public void AddCloudFoundryJwtAuthentication_AddsRequiredServices()
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();

            var services = new ServiceCollection();
            CloudFoundryServiceCollectionExtensions.AddCloudFoundryJwtAuthentication(services, config);
            var provider = services.BuildServiceProvider();
            var iopts = provider.GetService(typeof(IOptions<OAuthServiceOptions>)) as IOptions<OAuthServiceOptions>;
            Assert.NotNull(iopts);

        }
    }
}
