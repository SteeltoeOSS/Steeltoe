// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Serilog;

namespace Steeltoe.Extensions.Logging.DynamicSerilog
{
    internal static class SerilogConfigurationExtensions
    {
        /// <summary>
        /// Clear all the levels from serilog configuration. This extension is used to clear the levels in serilog, after capturing them into steeltoe config
        /// and using steeltoe config to control the verbosity.
        /// </summary>
        /// <param name="loggerConfiguration">The <see cref="LoggerConfiguration"/></param>
        /// <param name="minimumLevel">The Steeltoe <see cref="MinimumLevel"/></param>
        /// <returns>The <see cref="LoggerConfiguration"/> that is cleared</returns>
        internal static LoggerConfiguration ClearLevels(this LoggerConfiguration loggerConfiguration, MinimumLevel minimumLevel)
        {
            foreach (var overrideLevel in minimumLevel.Override)
            {
                loggerConfiguration.MinimumLevel.Override(overrideLevel.Key, Serilog.Events.LogEventLevel.Verbose);
            }

            return loggerConfiguration.MinimumLevel.Verbose();
        }
    }
}
