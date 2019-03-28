// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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

namespace Steeltoe.Management.Exporter.Tracing.Zipkin.Test
{
    public class TraceExporterOptionsTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            TraceExporterOptions opts = new TraceExporterOptions(null, builder.Build());

            Assert.Equal("Unknown", opts.ServiceName);
            Assert.True(opts.ValidateCertificates);
            Assert.Equal(TraceExporterOptions.DEFAULT_TIMEOUT, opts.TimeoutSeconds);
            Assert.True(opts.UseShortTraceIds);
            Assert.Equal(TraceExporterOptions.DEFAULT_ENDPOINT, opts.Endpoint);
        }

        [Fact]
        public void ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new TraceExporterOptions(null, config));
        }

        [Fact]
        public void BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:tracing:exporter:zipkin:serviceName"] = "foobar",
                ["management:tracing:exporter:zipkin:validateCertificates"] = "false",
                ["management:tracing:exporter:zipkin:timeoutSeconds"] = "100",
                ["management:tracing:exporter:zipkin:useShortTraceIds"] = "true",
                ["management:tracing:exporter:zipkin:endpoint"] = "https://foo.com/api/v2/spans"
            };

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            TraceExporterOptions opts = new TraceExporterOptions(null, builder.Build());

            Assert.Equal("foobar", opts.ServiceName);
            Assert.False(opts.ValidateCertificates);
            Assert.Equal(100, opts.TimeoutSeconds);
            Assert.True(opts.UseShortTraceIds);
            Assert.Equal("https://foo.com/api/v2/spans", opts.Endpoint);
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
            TraceExporterOptions opts = new TraceExporterOptions("default", config);
            Assert.Equal("foobar", opts.ServiceName);

            // Managment name overrides spring name
            appsettings = new Dictionary<string, string>()
            {
                ["spring:application:name"] = "foobar",
                ["management:tracing:exporter:zipkin:serviceName"] = "foobar2"
            };
            builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            config = builder.Build();
            opts = new TraceExporterOptions(null, config);
            Assert.Equal("foobar2", opts.ServiceName);

            // Default name returned
            appsettings = new Dictionary<string, string>();
            builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            config = builder.Build();
            opts = new TraceExporterOptions("default", config);
            Assert.Equal("default", opts.ServiceName);

            // No default name, returns unknown
            opts = new TraceExporterOptions(null, config);
            Assert.Equal("Unknown", opts.ServiceName);

            // vcap app name overrides spring name
            appsettings = new Dictionary<string, string>()
            {
                ["vcap:application:name"] = "foobar",
                ["spring:application:name"] = "foobar",
            };
            builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            config = builder.Build();
            opts = new TraceExporterOptions(null, config);
            Assert.Equal("foobar", opts.ServiceName);

            // Management name overrides everything
            appsettings = new Dictionary<string, string>()
            {
                ["management:tracing:exporter:zipkin:serviceName"] = "foobar",
                ["vcap:application:name"] = "foobar1",
                ["spring:application:name"] = "foobar2",
            };
            builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(appsettings);
            config = builder.Build();
            opts = new TraceExporterOptions(null, config);
            Assert.Equal("foobar", opts.ServiceName);
        }
    }
}
