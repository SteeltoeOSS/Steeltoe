// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Logging;

/// <summary>
/// Represents a logger category with its configured and effective minimum <see cref="LogLevel" />.
/// </summary>
public sealed class DynamicLoggerState
{
    /// <summary>
    /// Gets the logger category name, which is typically a namespace or fully-qualified type name.
    /// </summary>
    public string CategoryName { get; }

    /// <summary>
    /// Gets the minimum log level before it was dynamically changed. If not <c>null</c>, this entry can be reset. Value can be the same as
    /// <see cref="EffectiveMinLevel" />.
    /// </summary>
    public LogLevel? BackupMinLevel { get; }

    /// <summary>
    /// Gets the currently active minimum log level.
    /// </summary>
    public LogLevel EffectiveMinLevel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicLoggerState" /> class.
    /// </summary>
    /// <param name="categoryName">
    /// The logger category name, which is typically a namespace or fully-qualified type name.
    /// </param>
    /// <param name="backupMinLevel">
    /// The minimum log level before it was dynamically changed. If not <c>null</c>, this entry can be reset.
    /// </param>
    /// <param name="effectiveMinLevel">
    /// The currently active minimum log level.
    /// </param>
    public DynamicLoggerState(string categoryName, LogLevel? backupMinLevel, LogLevel effectiveMinLevel)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

        CategoryName = categoryName;
        BackupMinLevel = backupMinLevel;
        EffectiveMinLevel = effectiveMinLevel;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string categoryName = CategoryName.Length == 0 ? "Default" : CategoryName;
        return BackupMinLevel == null ? $"{categoryName}: {EffectiveMinLevel}" : $"{categoryName}: {BackupMinLevel} -> {EffectiveMinLevel}";
    }
}
