// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging.DynamicSerilog
{
    /// <summary>
    /// Implements a subset of the Serilog Options needed for SerilogDynamicProvider
    /// </summary>
    public class SerilogOptions : ISerilogOptions
    {
        public string ConfigPath => "Serilog";

        private Serilog.LoggerConfiguration _serilogConfiguration;

        /// <summary>
        /// Gets or sets the minimum level for the root logger (and the "Default").
        /// Limits the verbosity of all other overrides to this setting
        /// </summary>
        public MinimumLevel MinimumLevel { get; set; }

        public void SetSerilogOptions(IConfiguration configuration)
        {
            var section = configuration.GetSection(ConfigPath);
            section.Bind(this);
            if (MinimumLevel == null)
            {
                var defaultLevel = LogEventLevel.Information;

                var strMinLevel = section.GetValue<string>("MinimumLevel");
                if (!string.IsNullOrEmpty(strMinLevel))
                {
                    Enum.TryParse(strMinLevel, out defaultLevel);
                }

                MinimumLevel = new MinimumLevel()
                {
                    Default = defaultLevel,
                };
            }

            MinimumLevel.Override ??= new Dictionary<string, LogEventLevel>();
            _serilogConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configuration).ClearLevels(MinimumLevel);
        }

        // Capture Serilog configuration provided programmatically using reflection
        public void SetSerilogOptions(Serilog.LoggerConfiguration loggerConfiguration)
        {
            var minLevelProperty = loggerConfiguration.GetType().GetField("_minimumLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var minimumLevel = (LogEventLevel)minLevelProperty.GetValue(loggerConfiguration);

            var overridesProperty = loggerConfiguration.GetType().GetField("_overrides", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var overrideSwitches = (Dictionary<string, Serilog.Core.LoggingLevelSwitch>)overridesProperty.GetValue(loggerConfiguration);

            Dictionary<string, LogEventLevel> overrideLevels = new ();

            foreach (var overrideSwitch in overrideSwitches)
            {
                overrideLevels.Add(overrideSwitch.Key, overrideSwitch.Value.MinimumLevel);
            }

            MinimumLevel = new MinimumLevel()
            {
                Default = minimumLevel,
                Override = overrideLevels ?? new Dictionary<string, LogEventLevel>()
            };

            _serilogConfiguration = loggerConfiguration.ClearLevels(MinimumLevel);
        }

        public Serilog.LoggerConfiguration GetSerilogConfiguration() => _serilogConfiguration; // Method, so it won't `Bind` to anything

        [Obsolete("No longer needed with current implementation. Will be removed in next major release")]
        public IEnumerable<string> SubloggerConfigKeyExclusions { get; set; }

        [Obsolete("No longer needed with current implementation. Will be removed in next major release")]
        public IEnumerable<string> FullnameExclusions => new List<string>();
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class MinimumLevel
#pragma warning restore SA1402 // File may only contain a single class
    {
        public LogEventLevel Default { get; set; }

        public Dictionary<string, LogEventLevel> Override { get; set; }
    }
}
