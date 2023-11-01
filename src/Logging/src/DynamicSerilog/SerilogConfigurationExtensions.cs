// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog;
using Serilog.Events;
using Steeltoe.Common;

namespace Steeltoe.Logging.DynamicSerilog;

internal static class SerilogConfigurationExtensions
{
    /// <summary>
    /// Clears all the levels from Serilog configuration. This extension is used to clear the levels in Serilog, after capturing them into Steeltoe
    /// configuration and using Steeltoe configuration to control the verbosity.
    /// </summary>
    public static LoggerConfiguration ClearLevels(this LoggerConfiguration loggerConfiguration, MinimumLevel minimumLevel)
    {
        ArgumentGuard.NotNull(loggerConfiguration);
        ArgumentGuard.NotNull(minimumLevel);

        foreach (KeyValuePair<string, LogEventLevel> overrideLevel in minimumLevel.Override)
        {
            loggerConfiguration.MinimumLevel.Override(overrideLevel.Key, LogEventLevel.Verbose);
        }

        return loggerConfiguration.MinimumLevel.Verbose();
    }
}
