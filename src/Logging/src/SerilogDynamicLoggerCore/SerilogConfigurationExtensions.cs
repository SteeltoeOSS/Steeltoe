// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog;
using Serilog.Configuration;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger
{
    internal static class SerilogConfigurationExtensions
    {
        public static LoggerConfiguration SerilogConsole(this LoggerSinkConfiguration sinkConfiguration) => sinkConfiguration.Console();
    }
}
