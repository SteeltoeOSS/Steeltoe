// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Serilog;

namespace Steeltoe.Extensions.Logging.DynamicSerilog;

public static class SerilogWebHostBuilderExtensions
{
    /// <summary>
    /// Configure Serilog as the <see cref="IDynamicLoggerProvider" /> to enable dynamically controlling log levels via management endpoints.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="configureLogger">
    /// The delegate for configuring the <see cref="DynamicLoggerConfiguration" /> that will be used to construct a <see cref="Serilog.Core.Logger" />.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When true, do not remove Microsoft's ConsoleLoggerProvider.
    /// </param>
    /// <returns>
    /// The <see cref="IWebHostBuilder" />.
    /// </returns>
    public static IWebHostBuilder AddDynamicSerilog(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, LoggerConfiguration> configureLogger = null,
        bool preserveDefaultConsole = false)
    {
        return hostBuilder.ConfigureLogging((hostContext, logBuilder) =>
        {
            LoggerConfiguration loggerConfiguration = null;

            if (configureLogger != null)
            {
                loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration);
                configureLogger(hostContext, loggerConfiguration);

                logBuilder.AddDynamicSerilog(loggerConfiguration, preserveDefaultConsole);
            }
            else
            {
                logBuilder.AddDynamicSerilog(null);
            }
        });
    }
}
