// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Serilog;
using Steeltoe.Common;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Logging.DynamicSerilog;

public static class SerilogHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddDynamicSerilog(this IHostApplicationBuilder builder)
    {
        return AddDynamicSerilog(builder, null, false);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="configureLogger">
    /// Enables to configure Serilog from code instead of configuration.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddDynamicSerilog(this IHostApplicationBuilder builder,
        Action<IHostApplicationBuilder, LoggerConfiguration>? configureLogger)
    {
        return AddDynamicSerilog(builder, configureLogger, false);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When set to <c>true</c>, does not remove existing logger providers.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddDynamicSerilog(this IHostApplicationBuilder builder, bool preserveDefaultConsole)
    {
        return AddDynamicSerilog(builder, null, preserveDefaultConsole);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="configureLogger">
    /// Enables to configure Serilog from code instead of configuration.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When set to <c>true</c>, does not remove existing logger providers.
    /// </param>
    /// <returns>
    /// The incoming <see cref="IHostApplicationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IHostApplicationBuilder AddDynamicSerilog(this IHostApplicationBuilder builder,
        Action<IHostApplicationBuilder, LoggerConfiguration>? configureLogger, bool preserveDefaultConsole)
    {
        ArgumentGuard.NotNull(builder);

        HostBuilderWrapper wrapper = HostBuilderWrapper.Wrap(builder);
        wrapper.AddDynamicSerilog(HostBuilderContextWrapper.WrapAction(configureLogger), preserveDefaultConsole);

        return builder;
    }
}
