// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Logging.DynamicSerilog;

internal static class HostBuilderWrapperExtensions
{
    public static HostBuilderWrapper AddDynamicSerilog(this HostBuilderWrapper wrapper, Action<HostBuilderContextWrapper, LoggerConfiguration>? configureLogger,
        bool preserveDefaultConsole)
    {
        ArgumentGuard.NotNull(wrapper);

        wrapper.ConfigureLogging((hostContext, loggingBuilder) =>
        {
            LoggerConfiguration? loggerConfiguration = null;

            if (configureLogger != null)
            {
                loggerConfiguration = new LoggerConfiguration();
                configureLogger(hostContext, loggerConfiguration);
            }

            loggingBuilder.AddDynamicSerilog(loggerConfiguration, preserveDefaultConsole);
        });

        return wrapper;
    }
}
