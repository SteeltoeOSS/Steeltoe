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
    public class MetricsObserverOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new MetricsObserverOptions();
            Assert.Equal(opts.IngressIgnorePattern, MetricsObserverOptions.DEFAULT_INGRESS_IGNORE_PATTERN);
            Assert.Equal(opts.EgressIgnorePattern, MetricsObserverOptions.DEFAULT_EGRESS_IGNORE_PATTERN);
            Assert.True(opts.AspNetCoreHosting);
            Assert.True(opts.GCEvents);
            Assert.False(opts.EventCounterEvents);
            Assert.True(opts.ThreadPoolEvents);
            Assert.False(opts.HttpClientCore);
            Assert.False(opts.HttpClientDesktop);
            Assert.False(opts.HystrixEvents);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new MetricsObserverOptions(config));
        }

        [Fact]
        public void Constructor_BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:metrics:observer:ingressIgnorePattern"] = "pattern",
                ["management:metrics:observer:egressIgnorePattern"] = "pattern",
                ["management:metrics:observer:aspnetcoreHosting"] = "false",
                ["management:metrics:observer:gcEvents"] = "false",
                ["management:metrics:observer:eventCounterEvents"] = "true",
                ["management:metrics:observer:threadPoolEvents"] = "false",
                ["management:metrics:observer:httpClientCore"] = "true",
                ["management:metrics:observer:httpClientDesktop"] = "true",
                ["management:metrics:observer:hystrixEvents"] = "true",
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new MetricsObserverOptions(config);
            Assert.Equal("pattern", opts.IngressIgnorePattern);
            Assert.Equal("pattern", opts.EgressIgnorePattern);
            Assert.False(opts.AspNetCoreHosting);
            Assert.False(opts.GCEvents);
            Assert.True(opts.EventCounterEvents);
            Assert.False(opts.ThreadPoolEvents);
            Assert.True(opts.HttpClientCore);
            Assert.True(opts.HttpClientDesktop);
            Assert.True(opts.HystrixEvents);
        }
    }
}
