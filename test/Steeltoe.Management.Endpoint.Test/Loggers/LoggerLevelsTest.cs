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
using Xunit;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    public class LoggerLevelsTest
    {
        [Fact]
        public void MapLogLevel_ToString_ReturnsExpected()
        {
            Assert.Equal("OFF", LoggerLevels.MapLogLevel(LogLevel.None));
            Assert.Equal("FATAL", LoggerLevels.MapLogLevel(LogLevel.Critical));
            Assert.Equal("ERROR", LoggerLevels.MapLogLevel(LogLevel.Error));
            Assert.Equal("WARN", LoggerLevels.MapLogLevel(LogLevel.Warning));
            Assert.Equal("INFO", LoggerLevels.MapLogLevel(LogLevel.Information));
            Assert.Equal("DEBUG", LoggerLevels.MapLogLevel(LogLevel.Debug));
            Assert.Equal("TRACE", LoggerLevels.MapLogLevel(LogLevel.Trace));
        }

        [Fact]
        public void MapLogLevel_FromString_ReturnsExpected()
        {
            Assert.Equal(LogLevel.None, LoggerLevels.MapLogLevel("OFF"));
            Assert.Equal(LogLevel.Critical, LoggerLevels.MapLogLevel("FATAL"));
            Assert.Equal(LogLevel.Error, LoggerLevels.MapLogLevel("ERROR"));
            Assert.Equal(LogLevel.Warning, LoggerLevels.MapLogLevel("WARN"));
            Assert.Equal(LogLevel.Information, LoggerLevels.MapLogLevel("INFO"));
            Assert.Equal(LogLevel.Debug, LoggerLevels.MapLogLevel("DEBUG"));
            Assert.Equal(LogLevel.Trace, LoggerLevels.MapLogLevel("TRACE"));
            Assert.Null(LoggerLevels.MapLogLevel("FooBar"));
        }

        [Fact]
        public void Constructor_SetsProperties()
        {
            LoggerLevels lv1 = new LoggerLevels(null, LogLevel.Warning);
            Assert.Null(lv1.ConfiguredLevel);
            Assert.Equal("WARN", lv1.EffectiveLevel);
            LoggerLevels lv2 = new LoggerLevels(LogLevel.Information, LogLevel.Warning);
            Assert.Equal("INFO", lv2.ConfiguredLevel);
            Assert.Equal("WARN", lv2.EffectiveLevel);
        }
    }
}
