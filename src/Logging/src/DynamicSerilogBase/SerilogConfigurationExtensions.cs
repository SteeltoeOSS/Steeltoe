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
        /// Create a new LoggerConfiguration that reads from IConfiguration and adds a Console Sink
        /// </summary>
        /// <param name="configuration"><see cref="IConfiguration"/></param>
        /// <returns>What Steeltoe considers to be a default <see cref="LoggerConfiguration" /></returns>
        internal static LoggerConfiguration GetDefaultSerilogConfiguration(IConfiguration configuration) => new LoggerConfiguration().ReadFrom.Configuration(configuration).WriteToConsole();

        /// <summary>
        /// Calls .WriteTo.Console() for now, could be enhanced (via reflection) to make sure there's only one ConsoleSink
        /// </summary>
        /// <param name="loggerConfiguration"><see cref="LoggerConfiguration"/></param>
        /// <returns><see cref="LoggerConfiguration" /> that writes to console</returns>
        internal static LoggerConfiguration WriteToConsole(this LoggerConfiguration loggerConfiguration) => loggerConfiguration.WriteTo.Console();
    }
}
