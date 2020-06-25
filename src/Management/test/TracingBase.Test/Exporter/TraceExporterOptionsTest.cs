﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Trace.Exporter.Zipkin;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.TracingBase.Test.Exporter
{
    public class TraceExporterOptionsTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            var config = new ConfigurationBuilder().Build();
            var opts = new TraceExporterOptions(new ApplicationInstanceInfo(config), config);

            Assert.Equal(TestHelpers.EntryAssemblyName, opts.ServiceName);
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

            var config = TestHelpers.GetConfigurationFromDictionary(appsettings);
            var opts = new TraceExporterOptions(new ApplicationInstanceInfo(config), config);

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
            var config = TestHelpers.GetConfigurationFromDictionary(appsettings);
            var opts = new TraceExporterOptions(new ApplicationInstanceInfo(config), config);
            Assert.Equal("foobar", opts.ServiceName);

            // Management name overrides spring name
            appsettings = new Dictionary<string, string>()
            {
                ["spring:application:name"] = "foobar",
                ["management:tracing:exporter:zipkin:serviceName"] = "foobar2"
            };
            config = TestHelpers.GetConfigurationFromDictionary(appsettings);
            opts = new TraceExporterOptions(new ApplicationInstanceInfo(config), config);
            Assert.Equal("foobar2", opts.ServiceName);

            // Default name returned
            appsettings = new Dictionary<string, string>();
            config = TestHelpers.GetConfigurationFromDictionary(appsettings);
            opts = new TraceExporterOptions(new ApplicationInstanceInfo(config), config);
            Assert.Equal(TestHelpers.EntryAssemblyName, opts.ServiceName);

            // vcap app name overrides spring name
            appsettings = new Dictionary<string, string>()
            {
                ["vcap:application:name"] = "foobar",
                ["spring:application:name"] = "foobar",
            };
            config = TestHelpers.GetConfigurationFromDictionary(appsettings);
            opts = new TraceExporterOptions(new ApplicationInstanceInfo(config), config);
            Assert.Equal("foobar", opts.ServiceName);

            // Management name overrides everything
            appsettings = new Dictionary<string, string>()
            {
                ["management:tracing:exporter:zipkin:serviceName"] = "foobar",
                ["vcap:application:name"] = "foobar1",
                ["spring:application:name"] = "foobar2",
            };
            config = TestHelpers.GetConfigurationFromDictionary(appsettings);
            opts = new TraceExporterOptions(new ApplicationInstanceInfo(config), config);
            Assert.Equal("foobar", opts.ServiceName);
        }
    }
}
