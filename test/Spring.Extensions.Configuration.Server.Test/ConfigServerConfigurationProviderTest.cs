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

using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Spring.Extensions.Configuration.Server.Test
{
    public class ConfigServerConfigurationProviderTest
    {

        [Fact]
        public void SettingsConstructor__ThrowsIfSettingsNull()
        {
            // Arrange
            ConfigServerClientSettings settings = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProvider(settings));
            Assert.Contains(nameof(settings), ex.Message);
        }

        [Fact]
        public void SettingsConstructor__ThrowsIfHttpClientNull()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            HttpClient httpClient = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ConfigServerConfigurationProvider(settings, httpClient));
            Assert.Contains(nameof(httpClient), ex.Message);
        }

        [Fact]
        public void SettingsConstructor__WithLoggerFactorySucceeds()
        {
            // Arrange
            LoggerFactory logFactory = new LoggerFactory();
            ConfigServerClientSettings settings = new ConfigServerClientSettings();

            // Act and Assert
            var provider = new ConfigServerConfigurationProvider(settings, logFactory);
            Assert.NotNull(provider.Logger);
        }

        [Fact]
        public void DefaultConstructor_InitializedWithDefaultSettings()
        {
            // Arrange
            ConfigServerConfigurationProvider provider = new ConfigServerConfigurationProvider();

            // Act and Assert
            ConfigServerTestHelpers.VerifyDefaults(provider.Settings);

        }
    }
}


