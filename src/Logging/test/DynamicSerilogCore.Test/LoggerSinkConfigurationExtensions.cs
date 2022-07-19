// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using System;

namespace Steeltoe.Extensions.Logging.DynamicSerilog.Test;

/// <summary>
/// Serilog automatically adds the sink from IConfiguration when provided; Needed for "Serilog:Using" = [ "Steeltoe.Extensions.Logging.DynamicSerilog.Test" ].
/// </summary>
public static class LoggerSinkConfigurationExtensions
{
    public static LoggerConfiguration TestSink(this LoggerSinkConfiguration loggerConfiguration)
    {
        if (loggerConfiguration == null)
        {
            throw new ArgumentNullException(nameof(loggerConfiguration));
        }

        return loggerConfiguration.Sink(Test.TestSink.GetCurrentSink(), LogEventLevel.Verbose);
    }
}
