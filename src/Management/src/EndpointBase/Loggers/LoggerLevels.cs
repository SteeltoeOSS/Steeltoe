﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Loggers
{
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
            switch (level)
            {
                case LogLevel.None:
                    return "OFF";
                case LogLevel.Critical:
                    return "FATAL";
                case LogLevel.Error:
                    return "ERROR";
                case LogLevel.Warning:
                    return "WARN";
                case LogLevel.Information:
                    return "INFO";
                case LogLevel.Debug:
                    return "DEBUG";
                case LogLevel.Trace:
                    return "TRACE";
                default:
                    return "OFF";
            }
        }

        public static LogLevel? MapLogLevel(string level)
        {
            switch (level)
            {
                case "OFF":
                    return LogLevel.None;
                case "FATAL":
                    return LogLevel.Critical;
                case "ERROR":
                    return LogLevel.Error;
                case "WARN":
                    return LogLevel.Warning;
                case "INFO":
                    return LogLevel.Information;
                case "DEBUG":
                    return LogLevel.Debug;
                case "TRACE":
                    return LogLevel.Trace;
                default:
                    return null;
            }
        }
    }
}
