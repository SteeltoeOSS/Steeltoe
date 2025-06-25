// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Actuators.Loggers;

public sealed class LoggerLevels(LogLevel? configured, LogLevel effective)
{
    [JsonPropertyName("configuredLevel")]
    public string? ConfiguredLevel { get; } = configured.HasValue ? LogLevelToString(configured.Value) : null;

    [JsonPropertyName("effectiveLevel")]
    public string EffectiveLevel { get; } = LogLevelToString(effective);

    internal static string LogLevelToString(LogLevel level)
    {
        return level switch
        {
            LogLevel.None => "OFF",
            LogLevel.Critical => "FATAL",
            LogLevel.Error => "ERROR",
            LogLevel.Warning => "WARN",
            LogLevel.Information => "INFO",
            LogLevel.Debug => "DEBUG",
            LogLevel.Trace => "TRACE",
            _ => "OFF"
        };
    }

    internal static LogLevel? StringToLogLevel(string? level)
    {
        return level switch
        {
            "OFF" => LogLevel.None,
            "FATAL" => LogLevel.Critical,
            "ERROR" => LogLevel.Error,
            "WARN" => LogLevel.Warning,
            "INFO" => LogLevel.Information,
            "DEBUG" => LogLevel.Debug,
            "TRACE" => LogLevel.Trace,
            _ => null
        };
    }
}
