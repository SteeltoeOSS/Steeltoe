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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Extensions.Logging
{
    public static class DynamicLoggerConfigurationExtensions
    {
        /// <summary>
        /// Configure console logger settings from configuration
        /// </summary>
        /// <param name="settings">Logger settings object to be updated</param>
        /// <param name="configuration">Application configuration. Expects values prefixed with "Logging"</param>
        /// <returns>Configured console logger settings</returns>
        public static ConsoleLoggerSettings FromConfiguration(this ConsoleLoggerSettings settings, IConfiguration configuration)
        {
            if (configuration.GetSection("Logging").GetChildren().Any())
            {
                configuration = configuration.GetSection("Logging");
            }

            settings.IncludeScopes = bool.Parse(configuration.GetSection("IncludeScopes").Value ?? "false");

            AddSwitches(configuration.GetSection("LogLevel").GetChildren(), settings.Switches);
            AddSwitches(configuration.GetSection("Console:LogLevel").GetChildren(), settings.Switches);

            // Make sure a default entry exists
            if (!settings.Switches.Any(k => k.Key == "Default"))
            {
                settings.Switches.Add("Default", LogLevel.None);
            }

            return settings;
        }

        private static void AddSwitches(IEnumerable<IConfigurationSection> settings, IDictionary<string, LogLevel> switches)
        {
            foreach (var setting in settings)
            {
                try
                {
                    switches[setting.Key] = (LogLevel)Enum.Parse(typeof(LogLevel), setting.Value);
                }
                catch
                {
                }
            }
        }
    }
}
