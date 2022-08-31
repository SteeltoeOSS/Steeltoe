// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Steeltoe.Extensions.Logging.DynamicSerilog;

/// <summary>
/// Implements a subset of the Serilog Options needed for SerilogDynamicProvider.
/// </summary>
public class SerilogOptions : ISerilogOptions
{
    private LoggerConfiguration _serilogConfiguration;

    public string ConfigurationPath => "Serilog";

    /// <summary>
    /// Gets or sets the minimum level for the root logger (and the "Default"). Limits the verbosity of all other overrides to this setting.
    /// </summary>
    public MinimumLevel MinimumLevel { get; set; }

    public void SetSerilogOptions(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(ConfigurationPath);
        section.Bind(this);

        if (MinimumLevel == null || MinimumLevel.Default == (LogEventLevel)(-1))
        {
            var defaultLevel = LogEventLevel.Information;

            string strMinLevel = section.GetValue<string>("MinimumLevel");

            if (!string.IsNullOrEmpty(strMinLevel))
            {
                Enum.TryParse(strMinLevel, out defaultLevel);
            }

            MinimumLevel = new MinimumLevel
            {
                Default = defaultLevel
            };
        }

        MinimumLevel.Override ??= new Dictionary<string, LogEventLevel>();
        _serilogConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configuration).ClearLevels(MinimumLevel);
    }

    // Capture Serilog configuration provided programmatically using reflection
    public void SetSerilogOptions(LoggerConfiguration loggerConfiguration)
    {
        FieldInfo minLevelProperty = loggerConfiguration.GetType().GetField("_minimumLevel", BindingFlags.NonPublic | BindingFlags.Instance);
        var minimumLevel = (LogEventLevel)minLevelProperty.GetValue(loggerConfiguration);

        FieldInfo overridesProperty = loggerConfiguration.GetType().GetField("_overrides", BindingFlags.NonPublic | BindingFlags.Instance);
        var overrideSwitches = (Dictionary<string, LoggingLevelSwitch>)overridesProperty.GetValue(loggerConfiguration);

        Dictionary<string, LogEventLevel> overrideLevels = new();

        foreach (KeyValuePair<string, LoggingLevelSwitch> overrideSwitch in overrideSwitches)
        {
            overrideLevels.Add(overrideSwitch.Key, overrideSwitch.Value.MinimumLevel);
        }

        MinimumLevel = new MinimumLevel
        {
            Default = minimumLevel,
            Override = overrideLevels ?? new Dictionary<string, LogEventLevel>()
        };

        _serilogConfiguration = loggerConfiguration.ClearLevels(MinimumLevel);
    }

    public LoggerConfiguration GetSerilogConfiguration()
    {
        // Method, so it won't `Bind` to anything
        return _serilogConfiguration;
    }
}
