// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Steeltoe.Management.Tracing.Test
{
    public class TracingOptionsTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            var config = TestHelpers.GetConfigurationFromDictionary(new Dictionary<string, string>());
            var opts = new TracingOptions(new ApplicationInstanceInfo(config), config);

            Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, opts.Name);
            Assert.Equal(TracingOptions.DEFAULT_INGRESS_IGNORE_PATTERN, opts.IngressIgnorePattern);
            Assert.False(opts.AlwaysSample);
            Assert.False(opts.NeverSample);
            Assert.False(opts.UseShortTraceIds);
            Assert.Equal(TracingOptions.DEFAULT_EGRESS_IGNORE_PATTERN, opts.EgressIgnorePattern);
        }

        [Fact]
        public void ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new TracingOptions(null, config));
        }

        [Fact]
        public void BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>
            {
                ["management:tracing:name"] = "foobar",
                ["management:tracing:ingressIgnorePattern"] = "pattern",
                ["management:tracing:egressIgnorePattern"] = "pattern",
                ["management:tracing:alwaysSample"] = "true",
                ["management:tracing:neverSample"] = "true",
                ["management:tracing:useShortTraceIds"] = "true",
            };

            var config = TestHelpers.GetConfigurationFromDictionary(appsettings);
            var opts = new TracingOptions(new ApplicationInstanceInfo(config), config);

            Assert.Equal("foobar", opts.Name);
            Assert.Equal("pattern", opts.IngressIgnorePattern);
            Assert.True(opts.AlwaysSample);
            Assert.True(opts.NeverSample);
            Assert.True(opts.UseShortTraceIds);
            Assert.Equal("pattern", opts.EgressIgnorePattern);
        }

        [Fact]
        public void ApplicationName_ReturnsExpected()
        {
            var appsettings = new Dictionary<string, string>();
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            var config = builder.Build();
            var appInstanceInfo = new ApplicationInstanceInfo(config);

            // Uses Assembly name as default
            var opts = new TracingOptions(appInstanceInfo, config);
            Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, opts.Name);

            // Finds Spring app name
            appsettings.Add("spring:application:name", "SpringApplicationName");
            config = builder.Build();
            appInstanceInfo = new ApplicationInstanceInfo(config);
            opts = new TracingOptions(appInstanceInfo, config);
            Assert.Equal("SpringApplicationName", opts.Name);

            // Platform app name overrides spring name
            appsettings.Add("application:name", "PlatformName");
            config = builder.Build();
            appInstanceInfo = new ApplicationInstanceInfo(config);
            opts = new TracingOptions(appInstanceInfo, config);
            Assert.Equal("PlatformName", opts.Name);

            // Finds and uses management name
            appsettings.Add("management:name", "ManagementName");
            config = builder.Build();
            appInstanceInfo = new ApplicationInstanceInfo(config);
            opts = new TracingOptions(appInstanceInfo, config);
            Assert.Equal("ManagementName", opts.Name);

            // management:tracing name beats all else
            appsettings.Add("management:tracing:name", "ManagementTracingName");
            config = builder.Build();
            appInstanceInfo = new ApplicationInstanceInfo(config);
            opts = new TracingOptions(appInstanceInfo, config);
            Assert.Equal("ManagementTracingName", opts.Name);
        }
    }
}
