// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Serilog;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Logging.DynamicSerilog;

public static class SerilogWebHostBuilderExtensions
{
    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddDynamicSerilog(this IWebHostBuilder hostBuilder)
    {
        return AddDynamicSerilog(hostBuilder, null, false);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="configureLogger">
    /// Enables to configure Serilog from code instead of configuration.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddDynamicSerilog(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, LoggerConfiguration>? configureLogger)
    {
        return AddDynamicSerilog(hostBuilder, configureLogger, false);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When set to <c>true</c>, does not remove existing logger providers.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddDynamicSerilog(this IWebHostBuilder hostBuilder, bool preserveDefaultConsole)
    {
        return AddDynamicSerilog(hostBuilder, null, preserveDefaultConsole);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IWebHostBuilder" /> to configure.
    /// </param>
    /// <param name="configureLogger">
    /// Enables to configure Serilog from code instead of configuration.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When set to <c>true</c>, does not remove existing logger providers.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IWebHostBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static IWebHostBuilder AddDynamicSerilog(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, LoggerConfiguration>? configureLogger,
        bool preserveDefaultConsole)
    {
        ArgumentGuard.NotNull(hostBuilder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(hostBuilder);
        wrapper.AddDynamicSerilog(HostBuilderContextWrapper.WrapAction(configureLogger), preserveDefaultConsole);

        return hostBuilder;
    }
}
