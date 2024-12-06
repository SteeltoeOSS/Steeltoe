// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Actuators.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

public sealed class LoggerLevelsTest : BaseTest
{
    [Fact]
    public void MapLogLevel_ToString_ReturnsExpected()
    {
        LoggerLevels.LogLevelToString(LogLevel.None).Should().Be("OFF");
        LoggerLevels.LogLevelToString(LogLevel.Critical).Should().Be("FATAL");
        LoggerLevels.LogLevelToString(LogLevel.Error).Should().Be("ERROR");
        LoggerLevels.LogLevelToString(LogLevel.Warning).Should().Be("WARN");
        LoggerLevels.LogLevelToString(LogLevel.Information).Should().Be("INFO");
        LoggerLevels.LogLevelToString(LogLevel.Debug).Should().Be("DEBUG");
        LoggerLevels.LogLevelToString(LogLevel.Trace).Should().Be("TRACE");
    }

    [Fact]
    public void MapLogLevel_FromString_ReturnsExpected()
    {
        LoggerLevels.StringToLogLevel("OFF").Should().Be(LogLevel.None);
        LoggerLevels.StringToLogLevel("FATAL").Should().Be(LogLevel.Critical);
        LoggerLevels.StringToLogLevel("ERROR").Should().Be(LogLevel.Error);
        LoggerLevels.StringToLogLevel("WARN").Should().Be(LogLevel.Warning);
        LoggerLevels.StringToLogLevel("INFO").Should().Be(LogLevel.Information);
        LoggerLevels.StringToLogLevel("DEBUG").Should().Be(LogLevel.Debug);
        LoggerLevels.StringToLogLevel("TRACE").Should().Be(LogLevel.Trace);
        LoggerLevels.StringToLogLevel("Invalid").Should().BeNull();
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var effectiveOnly = new LoggerLevels(null, LogLevel.Warning);
        effectiveOnly.ConfiguredLevel.Should().BeNull();
        effectiveOnly.EffectiveLevel.Should().Be("WARN");

        var bothLevels = new LoggerLevels(LogLevel.Information, LogLevel.Warning);
        bothLevels.ConfiguredLevel.Should().Be("INFO");
        bothLevels.EffectiveLevel.Should().Be("WARN");
    }
}
