// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Steeltoe.Common;

namespace Steeltoe.Logging.DynamicSerilog;

public static class SerilogLoggingBuilderExtensions
{
    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ILoggingBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <see cref="ILoggingBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder)
    {
        return AddDynamicSerilog(builder, null, false);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ILoggingBuilder" /> to configure.
    /// </param>
    /// <param name="serilogConfiguration">
    /// Enables to configure Serilog from code instead of configuration.
    /// </param>
    /// <returns>
    /// The incoming <see cref="ILoggingBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder, LoggerConfiguration? serilogConfiguration)
    {
        return AddDynamicSerilog(builder, serilogConfiguration, false);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ILoggingBuilder" /> to configure.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When set to <c>true</c>, does not remove existing logger providers.
    /// </param>
    /// <returns>
    /// The incoming <see cref="ILoggingBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder, bool preserveDefaultConsole)
    {
        return AddDynamicSerilog(builder, null, preserveDefaultConsole);
    }

    /// <summary>
    /// Adds Serilog with Console sink, wrapped in a <see cref="DynamicSerilogLoggerProvider" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ILoggingBuilder" /> to configure.
    /// </param>
    /// <param name="serilogConfiguration">
    /// Enables to configure Serilog from code instead of configuration.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When set to <c>true</c>, does not remove existing logger providers.
    /// </param>
    /// <returns>
    /// The incoming <see cref="ILoggingBuilder" />, so that additional calls can be chained.
    /// </returns>
    public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder, LoggerConfiguration? serilogConfiguration, bool preserveDefaultConsole)
    {
        ArgumentGuard.NotNull(builder);

        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IDynamicLoggerProvider)))
        {
            throw new InvalidOperationException(
                "An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier in program startup (before adding Actuators) or remove duplicate IDynamicLoggerProvider entries.");
        }

        builder.AddFilter<DynamicSerilogLoggerProvider>(null, LogLevel.Trace);

        if (!preserveDefaultConsole)
        {
            builder.ClearProviders();
        }

        if (serilogConfiguration != null)
        {
            builder.Services.AddSingleton(serilogConfiguration);
            builder.Services.AddOptions<SerilogOptions>().Configure<LoggerConfiguration>((options, configuration) => options.SetSerilogOptions(configuration));
        }
        else
        {
            builder.Services.AddOptions<SerilogOptions>().Configure<IConfiguration>((options, configuration) => options.SetSerilogOptions(configuration));
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DynamicSerilogLoggerProvider>());
        builder.Services.AddSingleton(provider => provider.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().Single());

        return builder;
    }
}
