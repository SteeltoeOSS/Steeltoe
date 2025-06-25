// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Logging;

/// <summary>
/// Provides access to <see cref="ILogger" /> categories and their minimum log levels.
/// </summary>
public interface IDynamicLoggerProvider : ILoggerProvider
{
    /// <summary>
    /// Gets the list of logger categories in use, with their minimum log levels.
    /// </summary>
    ICollection<DynamicLoggerState> GetLogLevels();

    /// <summary>
    /// Changes the minimum log level for the specified logger category and its descendants.
    /// </summary>
    /// <param name="categoryName">
    /// The logger category name, which is typically a namespace or fully-qualified type name. An empty string represents the default minimum log level.
    /// </param>
    /// <param name="minLevel">
    /// The minimum log level to activate, or <c>null</c> to reset the level.
    /// </param>
    void SetLogLevel(string categoryName, LogLevel? minLevel);

    /// <summary>
    /// Refreshes the internally-tracked minimum log levels per logger category, originating from configuration.
    /// </summary>
    /// <param name="configuration">
    /// The updated minimum log levels per logger category.
    /// </param>
    void RefreshConfiguration(LogLevelsConfiguration configuration);
}
