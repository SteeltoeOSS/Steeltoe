// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Loggers;

public class LoggerLevels
{
    public LoggerLevels(LogLevel? configured, LogLevel effective)
    {
        ConfiguredLevel = configured.HasValue ? MapLogLevel(configured.Value) : null;
        EffectiveLevel = MapLogLevel(effective);
    }

    [JsonPropertyName("configuredLevel")]
    public string ConfiguredLevel { get; }

    [JsonPropertyName("effectiveLevel")]
    public string EffectiveLevel { get; }

    public static string MapLogLevel(LogLevel level)
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
            _ => "OFF",
        };
    }

    public static LogLevel? MapLogLevel(string level)
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
            _ => null,
        };
    }
}