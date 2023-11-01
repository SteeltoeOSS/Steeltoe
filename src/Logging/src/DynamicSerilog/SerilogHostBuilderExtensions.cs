// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Serilog;
using Steeltoe.Common;

namespace Steeltoe.Logging.DynamicSerilog;

public static class SerilogHostBuilderExtensions
{
    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddDynamicSerilog(this IHostBuilder hostBuilder)
    {
        return AddDynamicSerilog(hostBuilder, null, false);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <param name="configureLogger">
    /// Enables to configure Serilog from code instead of configuration.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddDynamicSerilog(this IHostBuilder hostBuilder, Action<HostBuilderContext, LoggerConfiguration>? configureLogger)
    {
        return AddDynamicSerilog(hostBuilder, configureLogger, false);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When set to <c>true</c>, does not remove existing logger providers.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddDynamicSerilog(this IHostBuilder hostBuilder, bool preserveDefaultConsole)
    {
        return AddDynamicSerilog(hostBuilder, null, preserveDefaultConsole);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IHostBuilder" /> to configure.
    /// </param>
    /// <param name="configureLogger">
    /// Enables to configure Serilog from code instead of configuration.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When set to <c>true</c>, does not remove existing logger providers.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IHostBuilder AddDynamicSerilog(this IHostBuilder hostBuilder, Action<HostBuilderContext, LoggerConfiguration>? configureLogger,
        bool preserveDefaultConsole)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.ConfigureLogging((hostContext, loggingBuilder) =>
        {
            LoggerConfiguration? loggerConfiguration = null;

            if (configureLogger != null)
            {
                loggerConfiguration = new LoggerConfiguration();
                configureLogger(hostContext, loggerConfiguration);
            }

            loggingBuilder.AddDynamicSerilog(loggerConfiguration, preserveDefaultConsole);
        });
    }
}
