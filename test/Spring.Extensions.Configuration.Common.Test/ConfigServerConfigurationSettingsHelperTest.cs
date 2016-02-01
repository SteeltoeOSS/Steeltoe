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
using System;
using System.Collections.Generic;
using Xunit;

namespace Spring.Extensions.Configuration.Common.Test
{
    public class ConfigServerConfigurationSettingsHelperTest
    {
        [Fact]
        public void Initalize_ThrowsOnNulls()
        {
            //Initialize(string configPrefix, ConfigServerClientSettingsBase settings, IHostingEnvironment environment, ConfigurationRoot root)
            // Arrange
            string configPrefix = null;
            ConfigServerClientSettingsBase settings = null;
            IHostingEnvironment environment = null;
            ConfigurationRoot root = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ConfigServerConfigurationSettingsHelper.Initialize(configPrefix, settings, environment, root));
            Assert.Contains(nameof(configPrefix), ex.Message);
            ex = Assert.Throws<ArgumentNullException>(() => ConfigServerConfigurationSettingsHelper.Initialize("foobar", settings, environment, root));
            Assert.Contains(nameof(settings), ex.Message);
            ex = Assert.Throws<ArgumentNullException>(() => ConfigServerConfigurationSettingsHelper.Initialize("foobar", new ConfigServerClientSettingsBase(), environment, root));
            Assert.Contains(nameof(environment), ex.Message);
        }

        [Fact]
        public void Initialize_WithDefaultSettings()
        {
            // Arrange
            string prefix = "spring:cloud:config";
            ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
            HostingEnvironment env = new HostingEnvironment();
            env.EnvironmentName = null;
            ConfigurationRoot root = new ConfigurationRoot(new List<IConfigurationProvider>());

            // Act and Assert
            ConfigServerConfigurationSettingsHelper.Initialize(prefix, settings, env, root);
            ConfigServerTestHelpers.VerifyDefaults(settings);



        }
    }
}
