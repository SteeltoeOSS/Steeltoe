﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
                if (TryGetSwitch(section.Value, out var level))
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
    }
}
