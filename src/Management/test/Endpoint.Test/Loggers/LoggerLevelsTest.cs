// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Loggers;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Loggers;

public sealed class LoggerLevelsTest : BaseTest
{
    [Fact]
    public void MapLogLevel_ToString_ReturnsExpected()
    {
        Assert.Equal("OFF", LoggerLevels.LogLevelToString(LogLevel.None));
        Assert.Equal("FATAL", LoggerLevels.LogLevelToString(LogLevel.Critical));
        Assert.Equal("ERROR", LoggerLevels.LogLevelToString(LogLevel.Error));
        Assert.Equal("WARN", LoggerLevels.LogLevelToString(LogLevel.Warning));
        Assert.Equal("INFO", LoggerLevels.LogLevelToString(LogLevel.Information));
        Assert.Equal("DEBUG", LoggerLevels.LogLevelToString(LogLevel.Debug));
        Assert.Equal("TRACE", LoggerLevels.LogLevelToString(LogLevel.Trace));
    }

    [Fact]
    public void MapLogLevel_FromString_ReturnsExpected()
    {
        Assert.Equal(LogLevel.None, LoggerLevels.StringToLogLevel("OFF"));
        Assert.Equal(LogLevel.Critical, LoggerLevels.StringToLogLevel("FATAL"));
        Assert.Equal(LogLevel.Error, LoggerLevels.StringToLogLevel("ERROR"));
        Assert.Equal(LogLevel.Warning, LoggerLevels.StringToLogLevel("WARN"));
        Assert.Equal(LogLevel.Information, LoggerLevels.StringToLogLevel("INFO"));
        Assert.Equal(LogLevel.Debug, LoggerLevels.StringToLogLevel("DEBUG"));
        Assert.Equal(LogLevel.Trace, LoggerLevels.StringToLogLevel("TRACE"));
        Assert.Null(LoggerLevels.StringToLogLevel("FooBar"));
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var levels1 = new LoggerLevels(null, LogLevel.Warning);
        Assert.Null(levels1.ConfiguredLevel);
        Assert.Equal("WARN", levels1.EffectiveLevel);

        var levels2 = new LoggerLevels(LogLevel.Information, LogLevel.Warning);
        Assert.Equal("INFO", levels2.ConfiguredLevel);
        Assert.Equal("WARN", levels2.EffectiveLevel);
    }
}
