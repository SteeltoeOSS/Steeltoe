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
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test
{
    public class CloudFoundryEndpointOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new CloudFoundryEndpointOptions();
            Assert.Null(opts.Enabled);
            Assert.True(opts.ValidateCertificates);
            Assert.Equal(string.Empty, opts.Id);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new CloudFoundryEndpointOptions(config));
        }

        [Fact]
        public void Constructor_BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:info:enabled"] = "true",
                ["management:endpoints:cloudfoundry:validatecertificates"] = "false",
                ["management:endpoints:cloudfoundry:enabled"] = "true"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new InfoEndpointOptions(config);
            var cloudOpts = new CloudFoundryEndpointOptions(config);

            Assert.True(cloudOpts.Enabled);
            Assert.Equal(string.Empty, cloudOpts.Id);
            Assert.Equal(string.Empty, cloudOpts.Path);
            Assert.False(cloudOpts.ValidateCertificates);

            Assert.True(opts.Enabled);
            Assert.Equal("info", opts.Id);
            Assert.Equal("info", opts.Path);
        }
    }
}
