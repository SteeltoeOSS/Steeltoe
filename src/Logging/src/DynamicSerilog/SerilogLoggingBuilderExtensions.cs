// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Logging.DynamicSerilog;

public static class SerilogLoggingBuilderExtensions
{
    public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder, LoggerConfiguration serilogConfiguration, bool preserveDefaultConsole = false)
    {
        ArgumentGuard.NotNull(builder);

        if (builder.Services.Any(sd => sd.ServiceType == typeof(IDynamicLoggerProvider)))
        {
            throw new InvalidOperationException(
                "An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier in program.cs (before adding Actuators) or remove duplicate IDynamicLoggerProvider entries.");
        }

        builder.AddFilter<SerilogDynamicProvider>(null, LogLevel.Trace);

        // only run if an IDynamicLoggerProvider hasn't already been added
        if (!builder.Services.Any(sd => sd.ServiceType == typeof(IDynamicLoggerProvider)))
        {
            if (!preserveDefaultConsole)
            {
                builder.ClearProviders();
            }

            if (serilogConfiguration != null)
            {
                builder.Services.AddSingleton(serilogConfiguration);

                builder.Services.AddOptions<SerilogOptions>()
                    .Configure<LoggerConfiguration>((options, serilogConfiguration) => options.SetSerilogOptions(serilogConfiguration));
            }
            else
            {
                builder.Services.AddOptions<SerilogOptions>().Configure<IConfiguration>((options, configuration) => options.SetSerilogOptions(configuration));
            }

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SerilogDynamicProvider>());
            builder.Services.AddSingleton(p => p.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().SingleOrDefault());
        }

        return builder;
    }

    /// <summary>
    /// Add Serilog with Console sink, wrapped in a <see cref="IDynamicLoggerProvider" /> that supports dynamically controlling the minimum log level via
    /// management endpoints.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ILoggingBuilder" /> for configuring the LoggerFactory.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When true, do not remove Microsoft's ConsoleLoggerProvider.
    /// </param>
    /// <returns>
    /// The configured <see cref="ILoggingBuilder" />.
    /// </returns>
    public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder, bool preserveDefaultConsole = false)
    {
        return builder.AddDynamicSerilog(null, preserveDefaultConsole);
    }

    /// <summary>
    /// Add Serilog, wrapped in a <see cref="IDynamicLoggerProvider" /> that supports dynamically controlling the minimum log level via management endpoints.
    /// Will add a Console sink if <paramref name="loggerConfiguration" /> is not provided.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ILoggingBuilder" /> for configuring the LoggerFactory.
    /// </param>
    /// <param name="loggerConfiguration">
    /// An initial <see cref="LoggerConfiguration" />.
    /// </param>
    /// <param name="preserveStaticLogger">
    /// Not supported.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When true, do not remove Microsoft's ConsoleLoggerProvider.
    /// </param>
    /// <returns>
    /// The configured <see cref="ILoggingBuilder" />.
    /// </returns>
    [Obsolete("Please use a different overload of AddDynamicSerilog ")]
    public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder, LoggerConfiguration loggerConfiguration, bool preserveStaticLogger,
        bool preserveDefaultConsole = false)
    {
        return builder.AddDynamicSerilog(loggerConfiguration, preserveDefaultConsole);
    }

    /// <summary>
    /// Add Steeltoe logger wrapped in a <see cref="IDynamicLoggerProvider" /> that supports dynamically controlling the minimum log level via management
    /// endpoints.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ILoggingBuilder" /> for configuring the LoggerFactory.
    /// </param>
    /// <returns>
    /// The configured <see cref="ILoggingBuilder" />.
    /// </returns>
    [Obsolete("Please use AddDynamicSerilog instead")]
    public static ILoggingBuilder AddSerilogDynamicConsole(this ILoggingBuilder builder)
    {
        return builder.AddDynamicSerilog();
    }

    /// <summary>
    /// Add Steeltoe logger wrapped in a <see cref="IDynamicLoggerProvider" /> that supports dynamically controlling the minimum log level via management
    /// endpoints.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ILoggingBuilder" /> for configuring the LoggerFactory.
    /// </param>
    /// <param name="loggerConfiguration">
    /// An initial <see cref="LoggerConfiguration" />.
    /// </param>
    /// <param name="preserveStaticLogger">
    /// Indicates whether to preserve the value of <see cref="Log.Logger" />.
    /// </param>
    /// <returns>
    /// The configured <see cref="ILoggingBuilder" />.
    /// </returns>
    [Obsolete("Please use AddDynamicSerilog instead")]
    public static ILoggingBuilder AddSerilogDynamicConsole(this ILoggingBuilder builder, LoggerConfiguration loggerConfiguration,
        bool preserveStaticLogger = false)
    {
        return builder.AddDynamicSerilog(loggerConfiguration, preserveStaticLogger);
    }
}
