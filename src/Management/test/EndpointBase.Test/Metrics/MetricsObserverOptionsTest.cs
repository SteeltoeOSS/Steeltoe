// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            const IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new MetricsObserverOptions(config));
        }

        [Fact]
        public void Constructor_BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>
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
            var configurationBuilder = new ConfigurationBuilder();
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
