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
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Tracing.Test
{
    public class TracingOptionsTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            TracingOptions opts = new TracingOptions(null, builder.Build());

            Assert.Equal("Unknown", opts.Name);
            Assert.Equal(TracingOptions.DEFAULT_INGRESS_IGNORE_PATTERN, opts.IngressIgnorePattern);
            Assert.False(opts.AlwaysSample);
            Assert.False(opts.NeverSample);
            Assert.True(opts.UseShortTraceIds);
            Assert.Equal(TracingOptions.DEFAULT_EGRESS_IGNORE_PATTERN, opts.EgressIgnorePattern);
            Assert.Equal(0, opts.MaxNumberOfAnnotations);
            Assert.Equal(0, opts.MaxNumberOfAttributes);
            Assert.Equal(0, opts.MaxNumberOfLinks);
            Assert.Equal(0, opts.MaxNumberOfMessageEvents);
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
            var appsettings = new Dictionary<string, string>()
            {
                ["management:tracing:name"] = "foobar",
                ["management:tracing:ingressIgnorePattern"] = "pattern",
                ["management:tracing:egressIgnorePattern"] = "pattern",
                ["management:tracing:maxNumberOfAttributes"] = "100",
                ["management:tracing:maxNumberOfAnnotations"] = "100",
                ["management:tracing:maxNumberOfMessageEvents"] = "100",
                ["management:tracing:maxNumberOfLinks"] = "100",
                ["management:tracing:alwaysSample"] = "true",
                ["management:tracing:neverSample"] = "true",
                ["management:tracing:useShortTraceIds"] = "true",
            };

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            TracingOptions opts = new TracingOptions(null, builder.Build());

            Assert.Equal("foobar", opts.Name);
            Assert.Equal("pattern", opts.IngressIgnorePattern);
            Assert.True(opts.AlwaysSample);
            Assert.True(opts.NeverSample);
            Assert.True(opts.UseShortTraceIds);
            Assert.Equal("pattern", opts.EgressIgnorePattern);
            Assert.Equal(100, opts.MaxNumberOfAnnotations);
            Assert.Equal(100, opts.MaxNumberOfAttributes);
            Assert.Equal(100, opts.MaxNumberOfLinks);
            Assert.Equal(100, opts.MaxNumberOfMessageEvents);
        }

        [Fact]
        public void ApplicationName_ReturnsExpected()
        {
            // Finds spring app name
            var appsettings = new Dictionary<string, string>()
            {
                ["spring:application:name"] = "foobar"
            };
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            var config = builder.Build();
            TracingOptions opts = new TracingOptions("default", config);
            Assert.Equal("foobar", opts.Name);

            // Management name overrides spring name
            appsettings = new Dictionary<string, string>()
            {
                ["spring:application:name"] = "foobar",
                ["management:tracing:name"] = "foobar2"
            };
            builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            config = builder.Build();
            opts = new TracingOptions(null, config);
            Assert.Equal("foobar2", opts.Name);

            // Default name returned
            appsettings = new Dictionary<string, string>();
            builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            config = builder.Build();
            opts = new TracingOptions("default", config);
            Assert.Equal("default", opts.Name);

            // No default name, returns unknown
            opts = new TracingOptions(null, config);
            Assert.Equal("Unknown", opts.Name);

            // vcap app name overrides spring name
            appsettings = new Dictionary<string, string>()
            {
                ["vcap:application:name"] = "foobar",
                ["spring:application:name"] = "foobar",
            };
            builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            config = builder.Build();
            opts = new TracingOptions(null, config);
            Assert.Equal("foobar", opts.Name);

            // Management name overrides everything
            appsettings = new Dictionary<string, string>()
            {
                ["management:tracing:name"] = "foobar",
                ["vcap:application:name"] = "foobar1",
                ["spring:application:name"] = "foobar2",
            };
            builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            config = builder.Build();
            opts = new TracingOptions(null, config);
            Assert.Equal("foobar", opts.Name);
        }
    }
}
