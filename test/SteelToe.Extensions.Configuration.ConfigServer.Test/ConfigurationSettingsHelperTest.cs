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

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using Xunit;

namespace SteelToe.Extensions.Configuration.ConfigServer.Test
{
    public class ConfigurationSettingsHelperTest
    {
        [Fact]
        public void Initalize_ThrowsOnNulls()
        {
            // Arrange
            string configPrefix = null;
            ConfigServerClientSettings settings = null;
            IHostingEnvironment environment = null;
            ConfigurationRoot root = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ConfigurationSettingsHelper.Initialize(configPrefix, settings, environment, root));
            Assert.Contains(nameof(configPrefix), ex.Message);
            ex = Assert.Throws<ArgumentNullException>(() => ConfigurationSettingsHelper.Initialize("foobar", settings, environment, root));
            Assert.Contains(nameof(settings), ex.Message);
            ex = Assert.Throws<ArgumentNullException>(() => ConfigurationSettingsHelper.Initialize("foobar", new ConfigServerClientSettings(), environment, root));
            Assert.Contains(nameof(environment), ex.Message);
        }

        [Fact]
        public void Initialize_WithDefaultSettings()
        {
            // Arrange
            string prefix = "spring:cloud:config";
            ConfigServerClientSettings settings = new ConfigServerClientSettings();
            HostingEnvironment env = new HostingEnvironment();
            ConfigurationRoot root = new ConfigurationRoot(new List<IConfigurationProvider>());

            // Act and Assert
            ConfigurationSettingsHelper.Initialize(prefix, settings, env, root);
            TestHelpers.VerifyDefaults(settings);



        }
    }
}
