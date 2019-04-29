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

namespace Steeltoe.Management.Endpoint.Metrics.Test
{
    public class MetricsOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new MetricsEndpointOptions();
            Assert.Null(opts.Enabled);
            Assert.Equal("metrics", opts.Id);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new MetricsEndpointOptions(config));
        }

        [Fact]
        public void Constructor_BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/management",
                ["management:endpoints:metrics:enabled"] = "false",
                ["management:endpoints:metrics:id"] = "metricsmanagement",
                ["management:endpoints:metrics:ingressIgnorePattern"] = "pattern",
                ["management:endpoints:metrics:egressIgnorePattern"] = "pattern",
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new MetricsEndpointOptions(config);
            Assert.False(opts.Enabled);
            Assert.Equal("metricsmanagement", opts.Id);
            Assert.Equal("pattern", opts.IngressIgnorePattern);
            Assert.Equal("pattern", opts.EgressIgnorePattern);
        }
    }
}
