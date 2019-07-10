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
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Hypermedia.Test
{
    public class ActuatorManagementOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new ActuatorManagementOptions();
            Assert.Equal("/actuator", opts.Path);
            Assert.Contains("health", opts.Exposure.Include);
            Assert.Contains("info", opts.Exposure.Include);
        }

        [Fact]
        public void Constructor_InitializesWithDefaultsOnCF()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "something");
            var config = new ConfigurationBuilder().Build();

            var opts = new ActuatorManagementOptions(config);
            Assert.Equal("/actuator", opts.Path);
            Assert.Contains("health", opts.Exposure.Include);
            Assert.Contains("info", opts.Exposure.Include);

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new ActuatorManagementOptions(config));
        }

        [Fact]
        public void Constructor_BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/management",
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new ActuatorManagementOptions(config);

            Assert.Equal("/management", opts.Path);
            Assert.False(opts.Enabled);

            Assert.Contains("health", opts.Exposure.Include);
            Assert.Contains("info", opts.Exposure.Include);
        }

        [Fact]
        public void Constructor_BindsConfigurationCorrectly_OnCF()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/management",
            };

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "something");

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new ActuatorManagementOptions(config);

            Assert.Equal("/management", opts.Path);
            Assert.False(opts.Enabled);

            Assert.Contains("health", opts.Exposure.Include);
            Assert.Contains("info", opts.Exposure.Include);

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        }

        [Fact]
        public void Constructor_OverridesInvalidConfiguration_OnCF()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:path"] = "/cloudfoundryapplication",
            };

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "something");
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new ActuatorManagementOptions(config);

            Assert.Equal("/actuator", opts.Path);

            Assert.Contains("health", opts.Exposure.Include);
            Assert.Contains("info", opts.Exposure.Include);

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        }
    }
}
