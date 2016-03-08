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

using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.OptionsModel;
using System;

using Xunit;

namespace SteelToe.Extensions.Configuration.ConfigServer.Test
{
    public class ConfigServerConfigServerServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddConfigServer_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ConfigServerServiceCollectionExtensions.AddConfigServer(services, config));
            Assert.Contains(nameof(services), ex.Message);

        }
        [Fact]
        public void AddConfigServer_ThrowsIfConfigurtionNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ConfigServerServiceCollectionExtensions.AddConfigServer(services, config));
            Assert.Contains(nameof(config), ex.Message);

        }
        [Fact]
        public void AddConfigServer_ConfiguresConfigServerClientSettingsOptions_WithDefaults()
        {
            // Arrange
            var services = new ServiceCollection();
            var environment = new HostingEnvironment();

            // Act and Assert
            var builder = new ConfigurationBuilder().AddConfigServer(environment);
            var config = builder.Build();
            ConfigServerServiceCollectionExtensions.AddConfigServer(services, config);

            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetService<IOptions<ConfigServerClientSettingsOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options);
            TestHelpers.VerifyDefaults(options.Settings);

            Assert.Equal(ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED, options.Enabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_FAILFAST, options.FailFast);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_URI, options.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, options.Environment);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CERTIFICATE_VALIDATION, options.ValidateCertificates);
            Assert.Null(options.Name);
            Assert.Null(options.Label);
            Assert.Null(options.Username);
            Assert.Null(options.Password);

        }
        [Fact]
        public void AddConfigServer_AddsConfigurationAsService()
        {
            // Arrange
            var services = new ServiceCollection();
            var environment = new HostingEnvironment();

            // Act and Assert
            var builder = new ConfigurationBuilder().AddConfigServer(environment);
            var config = builder.Build();
            ConfigServerServiceCollectionExtensions.AddConfigServer(services, config);

            var service = services.BuildServiceProvider().GetService<IConfigurationRoot>();
            Assert.NotNull(service);

        }
    }
}
