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

namespace Steeltoe.Management.Endpoint.Hypermedia.Test
{
    public class HypermediaEndpointOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new HypermediaEndpointOptions();
            Assert.Equal(string.Empty, opts.Id);
            Assert.Equal(string.Empty, opts.Path);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new HypermediaEndpointOptions(config));
        }

        [Fact]
        public void Constructor_BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",

                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:info:enabled"] = "true",
                ["management:endpoints:info:path"] = "infopath",

                ["management:endpoints:cloudfoundry:validatecertificates"] = "false",
                ["management:endpoints:cloudfoundry:enabled"] = "true"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new InfoEndpointOptions(config);
            var actOpts = new HypermediaEndpointOptions(config);

            Assert.Equal("info", opts.Id);
            Assert.Equal("infopath", opts.Path);
        }
    }
}
