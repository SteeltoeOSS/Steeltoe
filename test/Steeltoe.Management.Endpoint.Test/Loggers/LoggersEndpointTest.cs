//
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


using Steeltoe.Extensions.Logging.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.Logging;
using System;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    public class LoggersEndpointTest : BaseTest
    {
        [Fact]
        public void AddLevels_AddsExpected()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions());
            ep.AddLevels(dict);

            Assert.Equal(1, dict.Count);
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
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions());
            ep.SetLogLevel(null, null, null);
        }

        [Fact]
        public void SetLogLevel_ThrowsIfNulls()
        {
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions());
            Assert.Throws<ArgumentException>(() => ep.SetLogLevel(new TestLogProvider(), null, null));
            Assert.Throws<ArgumentException>(() => ep.SetLogLevel(new TestLogProvider(), "foobar", null));
        }

        [Fact]
        public void SetLogLevel_CallsProvider()
        {
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions());
            var provider = new TestLogProvider();
            ep.SetLogLevel(provider, "foobar", "WARN");

            Assert.Equal("foobar", provider.Category);
            Assert.Equal(LogLevel.Warning, provider.Level);
        }

        [Fact]
        public void GetLoggerConfigurations_NullProvider()
        {
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions());
            var result = ep.GetLoggerConfigurations(null);
            Assert.NotNull(result);
        }

        [Fact]
        public void GetLoggerConfiguration_CallsProvider()
        {
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions());
            var provider = new TestLogProvider();
            var result = ep.GetLoggerConfigurations(provider);
            Assert.NotNull(result);
            Assert.True(provider.GetLoggerConfigurationsCalled);

        }
        [Fact]
        public void DoInvoke_NoChangeRequest_ReturnsExpected()
        {
            LoggersEndpoint ep = new LoggersEndpoint(new LoggersOptions());
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
            Assert.Equal(0, loggers.Count);
        }
    }

    class TestLogProvider : ICloudFoundryLoggerProvider
    {
        public string Category { get; set; }
        public LogLevel Level { get; set; }
        public bool GetLoggerConfigurationsCalled { get; set;
        }
        public ILogger CreateLogger(string categoryName)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ICollection<ILoggerConfiguration> GetLoggerConfigurations()
        {
            GetLoggerConfigurationsCalled = true;
            return new List<ILoggerConfiguration>();
        }

        public void SetLogLevel(string category, LogLevel level)
        {
            Category = category;
            Level = level;
        }
    }
}
