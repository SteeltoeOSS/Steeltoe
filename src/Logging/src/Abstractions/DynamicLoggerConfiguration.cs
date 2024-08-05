// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Logging;

/// <summary>
/// Represents an <see cref="ILogger" /> category with its minimum log level.
/// </summary>
public sealed class DynamicLoggerConfiguration
{
    /// <summary>
    /// Gets the logger category name, which is typically a namespace or fully-qualified type name.
    /// </summary>
    public string CategoryName { get; }

    /// <summary>
    /// Gets the minimum log level that was originally configured at application startup (if present).
    /// </summary>
    public LogLevel? ConfigurationMinLevel { get; }

    /// <summary>
    /// Gets the currently active minimum log level.
    /// </summary>
    public LogLevel EffectiveMinLevel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicLoggerConfiguration" /> class.
    /// </summary>
    /// <param name="categoryName">
    /// The logger category name, which is typically a namespace or fully-qualified type name.
    /// </param>
    /// <param name="configurationMinLevel">
    /// The minimum log level that was originally configured at application startup (if present).
    /// </param>
    /// <param name="effectiveMinLevel">
    /// The currently active minimum log level.
    /// </param>
    public DynamicLoggerConfiguration(string categoryName, LogLevel? configurationMinLevel, LogLevel effectiveMinLevel)
    {
        ArgumentException.ThrowIfNullOrEmpty(categoryName);

        CategoryName = categoryName;
        ConfigurationMinLevel = configurationMinLevel;
        EffectiveMinLevel = effectiveMinLevel;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ConfigurationMinLevel == null ? $"{CategoryName}: {EffectiveMinLevel}" : $"{CategoryName}: {ConfigurationMinLevel} -> {EffectiveMinLevel}";
    }
}
