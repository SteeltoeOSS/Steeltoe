// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    public class LoggerLevelsTest : BaseTest
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
