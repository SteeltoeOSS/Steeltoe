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

using Microsoft.Extensions.Logging;
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
            Dictionary<string, object> dict = new Dictionary<string, object>();

            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions(), null, null);
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
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions(), null, null);
            ep.SetLogLevel(null, null, null);
        }

        [Fact]
        public void SetLogLevel_ThrowsIfNullName()
        {
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions(), null, null);
            Assert.Throws<ArgumentException>(() => ep.SetLogLevel(new TestLogProvider(), null, null));
        }

        [Fact]
        public void SetLogLevel_CallsProvider()
        {
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions(), null, null);
            var provider = new TestLogProvider();
            ep.SetLogLevel(provider, "foobar", "WARN");

            Assert.Equal("foobar", provider.Category);
            Assert.Equal(LogLevel.Warning, provider.Level);
        }

        [Fact]
        public void GetLoggerConfigurations_NullProvider()
        {
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions(), null, null);
            var result = ep.GetLoggerConfigurations(null);
            Assert.NotNull(result);
        }

        [Fact]
        public void GetLoggerConfiguration_CallsProvider()
        {
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions(), null, null);
            var provider = new TestLogProvider();
            var result = ep.GetLoggerConfigurations(provider);
            Assert.NotNull(result);
            Assert.True(provider.GetLoggerConfigurationsCalled);
        }

        [Fact]
        public void DoInvoke_NoChangeRequest_ReturnsExpected()
        {
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions(), null, null);
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
