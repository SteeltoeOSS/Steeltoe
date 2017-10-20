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
//

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;


namespace Steeltoe.Common.Logging.Autofac
{
    public class LoggerFilterConfigureOptions : IConfigureOptions<LoggerFilterOptions>
    {
        private readonly IConfiguration _configuration;

        public LoggerFilterConfigureOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(LoggerFilterOptions options)
        {
            LoadDefaultConfigValues(options);
        }

        private void LoadDefaultConfigValues(LoggerFilterOptions options)
        {
            if (_configuration == null)
            {
                return;
            }

            foreach (var configurationSection in _configuration.GetChildren())
            {
                if (configurationSection.Key == "LogLevel")
                {
                    // Load global category defaults
                    LoadRules(options, configurationSection, null);
                }
                else
                {
                    var logLevelSection = configurationSection.GetSection("LogLevel");
                    if (logLevelSection != null)
                    {
                        // Load logger specific rules
                        var logger = configurationSection.Key;
                        LoadRules(options, logLevelSection, logger);
                    }
                }
            }
        }

        private void LoadRules(LoggerFilterOptions options, IConfigurationSection configurationSection, string logger)
        {
            foreach (var section in configurationSection.AsEnumerable(true))
            {
                LogLevel level;
                if (TryGetSwitch(section.Value, out level))
                {
                    var category = section.Key;
                    if (category == "Default")
                    {
                        category = null;
                    }
                    var newRule = new LoggerFilterRule(logger, category, level, null);
                    options.Rules.Add(newRule);
                }
            }
        }

        private static bool TryGetSwitch(string value, out LogLevel level)
        {
            if (string.IsNullOrEmpty(value))
            {
                level = LogLevel.None;
                return false;
            }
            else if (Enum.TryParse(value, true, out level))
            {
                return true;
            }
            else
            {
                throw new InvalidOperationException($"Configuration value '{value}' is not supported.");
            }
        }
    }
}
