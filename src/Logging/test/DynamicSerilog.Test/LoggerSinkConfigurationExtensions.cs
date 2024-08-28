// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Steeltoe.Logging.DynamicSerilog.Test;

/// <summary>
/// Serilog automatically adds the sink from IConfiguration when provided; Needed for "Serilog:Using" = [ "Steeltoe.Logging.DynamicSerilog.Test" ].
/// </summary>
// ReSharper disable once UnusedType.Global
public static class LoggerSinkConfigurationExtensions
{
    public static LoggerConfiguration TestSink(this LoggerSinkConfiguration loggerConfiguration)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);

        return loggerConfiguration.Sink(Test.TestSink.GetCurrentSink(), LogEventLevel.Verbose);
    }
}
