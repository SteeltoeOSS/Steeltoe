// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Logging;

/// <summary>
/// Provides minimum log levels and filters per category, to initialize an <see cref="IDynamicLoggerProvider" />.
/// </summary>
public sealed class LoggerFilterConfiguration
{
    /// <summary>
    /// Gets the minimum log levels originally configured at application startup, per logger category.
    /// </summary>
    public IReadOnlyDictionary<string, LogLevel> ConfigurationMinLevels { get; }

    /// <summary>
    /// Gets the active filters per logger category.
    /// </summary>
    public IReadOnlyDictionary<string, LoggerFilter> EffectiveFilters { get; }

    /// <summary>
    /// Gets the filter used for the default logger category.
    /// </summary>
    public LoggerFilter DefaultFilter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerFilterConfiguration" /> class.
    /// </summary>
    /// <param name="configurationMinLevels">
    /// The minimum log levels originally configured at application startup, per logger category.
    /// </param>
    /// <param name="effectiveFilters">
    /// The active filters per logger category.
    /// </param>
    /// <param name="defaultFilter">
    /// The filter used for the default logger category.
    /// </param>
    public LoggerFilterConfiguration(IReadOnlyDictionary<string, LogLevel> configurationMinLevels, IReadOnlyDictionary<string, LoggerFilter> effectiveFilters,
        LoggerFilter defaultFilter)
    {
        ArgumentNullException.ThrowIfNull(configurationMinLevels);
        ArgumentNullException.ThrowIfNull(effectiveFilters);

        ConfigurationMinLevels = configurationMinLevels;
        EffectiveFilters = effectiveFilters;
        DefaultFilter = defaultFilter;
    }
}
