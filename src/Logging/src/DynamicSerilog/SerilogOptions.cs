// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Steeltoe.Logging.DynamicSerilog.DynamicTypeAccess;

namespace Steeltoe.Logging.DynamicSerilog;

/// <summary>
/// Contains the subset of Serilog options that <see cref="DynamicSerilogLoggerProvider" /> needs.
/// </summary>
public sealed class SerilogOptions
{
    private LoggerConfiguration? _serilogConfiguration;

    /// <summary>
    /// Gets or sets the minimum level for the root logger (and the "Default"). Limits the verbosity of all other overrides to this setting.
    /// </summary>
    public MinimumLevel? MinimumLevel { get; set; }

    /// <summary>
    /// Enables binding from configuration.
    /// </summary>
    /// <param name="configuration">
    /// The configuration to bind from.
    /// </param>
    internal void SetSerilogOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetSection("Serilog");
        section.Bind(this);

        if (MinimumLevel == null || MinimumLevel.Default == (LogEventLevel)(-1))
        {
            var defaultLevel = LogEventLevel.Information;

            string? minLevelText = section.GetValue<string>("MinimumLevel");

            if (Enum.TryParse(minLevelText, out LogEventLevel level))
            {
                defaultLevel = level;
            }

            MinimumLevel ??= new MinimumLevel();
            MinimumLevel.Default = defaultLevel;
        }

#pragma warning disable S4792 // Configuring loggers is security-sensitive
        _serilogConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configuration).ClearLevels(MinimumLevel);
#pragma warning restore S4792 // Configuring loggers is security-sensitive
    }

    /// <summary>
    /// Enables configuring programmatically.
    /// </summary>
    /// <param name="loggerConfiguration">
    /// The instance to obtain settings from.
    /// </param>
    internal void SetSerilogOptions(LoggerConfiguration loggerConfiguration)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);

        var shim = new LoggerConfigurationShim(loggerConfiguration);

        Dictionary<string, LogEventLevel> overrideLevels = [];

        foreach (KeyValuePair<string, LoggingLevelSwitch> overrideSwitch in shim.Overrides)
        {
            overrideLevels.Add(overrideSwitch.Key, overrideSwitch.Value.MinimumLevel);
        }

        MinimumLevel = new MinimumLevel
        {
            Default = shim.MinimumLevel
        };

        foreach ((string name, LogEventLevel level) in overrideLevels)
        {
            MinimumLevel.Override.Add(name, level);
        }

        _serilogConfiguration = loggerConfiguration.ClearLevels(MinimumLevel);
    }

    // Provided as method, so it won't bind to configuration.
    internal LoggerConfiguration GetSerilogConfiguration()
    {
        if (_serilogConfiguration == null)
        {
            throw new InvalidOperationException("Ensure that SetSerilogOptions() is called first.");
        }

        return _serilogConfiguration;
    }
}
