// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.TestResources;

public static class LoggerExtensions
{
    private static readonly List<LogLevel> LogLevelsInOrderOfVerbosity =
    [
        LogLevel.Trace,
        LogLevel.Debug,
        LogLevel.Information,
        LogLevel.Warning,
        LogLevel.Error,
        LogLevel.Critical
    ];

    public static LogLevel ProbeMinLevel(this ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        foreach (LogLevel logLevel in LogLevelsInOrderOfVerbosity)
        {
            if (logger.IsEnabled(logLevel))
            {
                return logLevel;
            }
        }

        return LogLevel.None;
    }
}
