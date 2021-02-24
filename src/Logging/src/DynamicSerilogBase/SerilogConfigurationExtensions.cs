// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Extensions.Logging.DynamicSerilog
{
    internal static class SerilogConfigurationExtensions
    {
        public static LoggerConfiguration SerilogConsole(this LoggerSinkConfiguration sinkConfiguration) => sinkConfiguration.Console();

        internal static LoggerConfiguration AddConsoleIfNoSinksFound(this LoggerConfiguration loggerConfiguration)
        {
            var configuredSinksField = loggerConfiguration.GetType().GetField("_logEventSinks", BindingFlags.NonPublic | BindingFlags.Instance);
            if (configuredSinksField.GetValue(loggerConfiguration) is not List<ILogEventSink> configuredSinks || !configuredSinks.Any())
            {
                loggerConfiguration.WriteTo.Console();
            }

            return loggerConfiguration;
        }
    }
}
