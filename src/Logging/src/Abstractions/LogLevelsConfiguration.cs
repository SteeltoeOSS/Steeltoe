// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Logging;

/// <summary>
/// Provides minimum log levels per logger category, originating from configuration. Used to initialize or update an
/// <see cref="IDynamicLoggerProvider" />.
/// </summary>
public sealed class LogLevelsConfiguration
{
    /// <summary>
    /// Gets the minimum log levels per logger category, which is typically a namespace or fully-qualified type name. An empty string represents the default
    /// minimum log level.
    /// </summary>
    public IReadOnlyDictionary<string, LogLevel> MinLevelsPerCategory { get; }

    public LogLevelsConfiguration(IReadOnlyDictionary<string, LogLevel> minLevelsPerCategory)
    {
        ArgumentNullException.ThrowIfNull(minLevelsPerCategory);

        MinLevelsPerCategory = minLevelsPerCategory;
    }
}
