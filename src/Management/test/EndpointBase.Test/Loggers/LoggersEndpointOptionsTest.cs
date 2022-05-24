// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    public class LoggersEndpointOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new LoggersEndpointOptions();
            Assert.Null(opts.Enabled);
            Assert.Equal("loggers", opts.Id);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            const IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new LoggersEndpointOptions(config));
        }

        [Fact]
        public void Constructor_BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:loggers:enabled"] = "false",
                ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
                ["management:endpoints:cloudfoundry:enabled"] = "true"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new LoggersEndpointOptions(config);
            var cloudOpts = new CloudFoundryEndpointOptions(config);

            Assert.True(cloudOpts.Enabled);
            Assert.Equal(string.Empty, cloudOpts.Id);
            Assert.Equal(string.Empty, cloudOpts.Path);
            Assert.True(cloudOpts.ValidateCertificates);

            Assert.False(opts.Enabled);
            Assert.Equal("loggers", opts.Id);
            Assert.Equal("loggers", opts.Path);
        }
    }
}
