// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public class LoggerLevels
    {
        public LoggerLevels(LogLevel? configured, LogLevel effective)
        {
            ConfiguredLevel = configured.HasValue ? MapLogLevel(configured.Value) : null;
            EffectiveLevel = MapLogLevel(effective);
        }

        [JsonProperty("configuredLevel")]
        public string ConfiguredLevel { get; }

        [JsonProperty("effectiveLevel")]
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
}
