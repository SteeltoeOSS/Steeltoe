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
            switch(level)
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
        public static LogLevel MapLogLevel(string level)
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
                    return LogLevel.None;
            }
        }
    }
}
