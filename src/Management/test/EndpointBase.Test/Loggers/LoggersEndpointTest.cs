// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    public class LoggersEndpointTest : BaseTest
    {
        [Fact]
        public void AddLevels_AddsExpected()
        {
            var dict = new Dictionary<string, object>();

            var ep = new LoggersEndpoint(new LoggersEndpointOptions(), (IDynamicLoggerProvider)null, null);
            ep.AddLevels(dict);

            Assert.Single(dict);
            Assert.True(dict.ContainsKey("levels"));
            var levs = dict["levels"] as List<string>;
            Assert.NotNull(levs);
            Assert.Equal(7, levs.Count);

            Assert.Contains("OFF", levs);
            Assert.Contains("FATAL", levs);
            Assert.Contains("ERROR", levs);
            Assert.Contains("WARN", levs);
            Assert.Contains("INFO", levs);
            Assert.Contains("DEBUG", levs);
            Assert.Contains("TRACE", levs);
        }

        [Fact]
        public void SetLogLevel_NullProvider()
        {
            var ep = new LoggersEndpoint(new LoggersEndpointOptions(), (IDynamicLoggerProvider)null, null);
            ep.SetLogLevel(null, null, null);
        }

        [Fact]
        public void SetLogLevel_ThrowsIfNullName()
        {
            var ep = new LoggersEndpoint(new LoggersEndpointOptions(), (IDynamicLoggerProvider)null, null);
            Assert.Throws<ArgumentException>(() => ep.SetLogLevel(new TestLogProvider(), null, null));
        }

        [Fact]
        public void SetLogLevel_CallsProvider()
        {
            var ep = new LoggersEndpoint(new LoggersEndpointOptions(), (IDynamicLoggerProvider)null, null);
            var provider = new TestLogProvider();
            ep.SetLogLevel(provider, "foobar", "WARN");

            Assert.Equal("foobar", provider.Category);
            Assert.Equal(LogLevel.Warning, provider.Level);
        }

        [Fact]
        public void GetLoggerConfigurations_NullProvider()
        {
            var ep = new LoggersEndpoint(new LoggersEndpointOptions(), (IDynamicLoggerProvider)null, null);
            var result = ep.GetLoggerConfigurations(null);
            Assert.NotNull(result);
        }

        [Fact]
        public void GetLoggerConfiguration_CallsProvider()
        {
            var ep = new LoggersEndpoint(new LoggersEndpointOptions(), (IDynamicLoggerProvider)null, null);
            var provider = new TestLogProvider();
            var result = ep.GetLoggerConfigurations(provider);
            Assert.NotNull(result);
            Assert.True(provider.GetLoggerConfigurationsCalled);
        }

        [Fact]
        public void DoInvoke_NoChangeRequest_ReturnsExpected()
        {
            var ep = new LoggersEndpoint(new LoggersEndpointOptions(), (IDynamicLoggerProvider)null, null);
            var provider = new TestLogProvider();

            var result = ep.DoInvoke(provider, null);
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("levels"));
            var levs = result["levels"] as List<string>;
            Assert.NotNull(levs);
            Assert.Equal(7, levs.Count);

            Assert.True(result.ContainsKey("loggers"));
            var loggers = result["loggers"] as Dictionary<string, LoggerLevels>;
            Assert.NotNull(loggers);
            Assert.Empty(loggers);
        }
    }
}
