// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Serilog;
using System;

namespace Steeltoe.Extensions.Logging.DynamicSerilog
{
    public static class SerilogHostBuilderExtensions
    {
        /// <summary>
        /// Configure Serilog as the <see cref="IDynamicLoggerProvider"/> to enable dynamically controlling log levels via management endpoints
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure</param>
        /// <param name="configureLogger">The delegate for configuring the <see cref="DynamicLoggerConfiguration" /> that will be used to construct a <see cref="Serilog.Core.Logger" /></param>
        /// <param name="preserveStaticLogger">Indicates whether to preserve the value of <see cref="Log.Logger"/>.</param>
        /// <param name="preserveDefaultConsole">When true, do not remove Microsoft's ConsoleLoggerProvider</param>
        /// <returns>The <see cref="IHostBuilder"/></returns>
        public static IHostBuilder AddDynamicSerilog(
            this IHostBuilder hostBuilder,
            Action<HostBuilderContext, LoggerConfiguration> configureLogger = null,
            bool preserveStaticLogger = false,
            bool preserveDefaultConsole = false)
        {
            return hostBuilder
                .ConfigureLogging((hostContext, logBuilder) =>
                {
                    LoggerConfiguration loggerConfiguration = null;

                    if (configureLogger is object)
                    {
                        loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration);
                        configureLogger(hostContext, loggerConfiguration);
                    }

                    logBuilder.AddDynamicSerilog(loggerConfiguration, preserveDefaultConsole);
                });
        }

        /// <summary>
        /// Sets Steeltoe <see cref="IDynamicLoggerProvider"/> Serilog implementation as a LoggerProvider which supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure</param>
        /// <param name="configureLogger">The delegate for configuring the <see cref="DynamicLoggerConfiguration" /> that will be used to construct a <see cref="Serilog.Core.Logger" /></param>
        /// <param name="preserveStaticLogger">Indicates whether to preserve the value of <see cref="Log.Logger"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/></returns>
        [Obsolete("Please use 'AddDynamicSerilog' instead")]
        public static IHostBuilder UseSerilogDynamicConsole(
            this IHostBuilder hostBuilder,
            Action<HostBuilderContext, LoggerConfiguration> configureLogger = null,
            bool preserveStaticLogger = false) => hostBuilder.AddDynamicSerilog(configureLogger, preserveStaticLogger);
    }
}
