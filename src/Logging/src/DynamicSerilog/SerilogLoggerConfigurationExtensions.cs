// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog;
using Serilog.Events;

namespace Steeltoe.Logging.DynamicSerilog;

internal static class SerilogLoggerConfigurationExtensions
{
    /// <summary>
    /// Clears all the levels from Serilog configuration. This extension method clears the levels in Serilog, after capturing them into Steeltoe
    /// configuration and using Steeltoe configuration to control the verbosity.
    /// </summary>
    public static LoggerConfiguration ClearLevels(this LoggerConfiguration loggerConfiguration, MinimumLevel minimumLevel)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);
        ArgumentNullException.ThrowIfNull(minimumLevel);

        foreach ((string categoryName, _) in minimumLevel.Override)
        {
            loggerConfiguration.MinimumLevel.Override(categoryName, LogEventLevel.Verbose);
        }

        return loggerConfiguration.MinimumLevel.Verbose();
    }
}
