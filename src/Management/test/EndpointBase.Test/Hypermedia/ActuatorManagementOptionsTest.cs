// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var appsettings = new Dictionary<string, string>
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/management",
            };

            var configurationBuilder = new ConfigurationBuilder();
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
            var appsettings = new Dictionary<string, string>
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/management",
            };

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "something");

            var configurationBuilder = new ConfigurationBuilder();
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
            var appsettings = new Dictionary<string, string>
            {
                ["management:endpoints:path"] = "/cloudfoundryapplication",
            };

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "something");
            var configurationBuilder = new ConfigurationBuilder();
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
