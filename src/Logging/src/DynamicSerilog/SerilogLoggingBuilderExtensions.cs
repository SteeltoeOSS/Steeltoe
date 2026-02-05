// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
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
    /// Enables configuring Serilog from code instead of configuration.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
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
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
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
    /// Enables configuring Serilog from code instead of configuration.
    /// </param>
    /// <param name="preserveDefaultConsole">
    /// When set to <c>true</c>, does not remove existing logger providers.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder, LoggerConfiguration? serilogConfiguration, bool preserveDefaultConsole)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!IsSerilogDynamicLoggerProviderAlreadyRegistered(builder))
        {
            AssertNoDynamicLoggerProviderRegistered(builder);

            builder.AddFilter<DynamicSerilogLoggerProvider>(null, LogLevel.Trace);

            if (!preserveDefaultConsole)
            {
                builder.ClearProviders();
            }

            ConfigureSerilogOptions(builder, serilogConfiguration);

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DynamicSerilogLoggerProvider>());
            builder.Services.AddSingleton(provider => provider.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().Single());
        }

        return builder;
    }

    private static bool IsSerilogDynamicLoggerProviderAlreadyRegistered(ILoggingBuilder builder)
    {
        return builder.Services.Any(descriptor =>
            descriptor.SafeGetImplementationType() == typeof(DynamicSerilogLoggerProvider) && descriptor.ServiceType == typeof(ILoggerProvider));
    }

    private static void AssertNoDynamicLoggerProviderRegistered(ILoggingBuilder builder)
    {
        if (builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IDynamicLoggerProvider)))
        {
            throw new InvalidOperationException(
                $"A different {nameof(IDynamicLoggerProvider)} has already been registered. Call '{nameof(AddDynamicSerilog)}' earlier during startup (before adding actuators).");
        }
    }

    private static void ConfigureSerilogOptions(ILoggingBuilder builder, LoggerConfiguration? serilogConfiguration)
    {
        builder.Services.AddOptions<SerilogOptions>().Configure<IConfiguration>((options, configuration) => options.SetSerilogOptions(configuration));

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IOptionsChangeTokenSource<SerilogOptions>, ConfigurationChangeTokenSource<SerilogOptions>>());

        if (serilogConfiguration != null)
        {
            builder.Services.AddOptions<SerilogOptions>().Configure(options => options.SetSerilogOptions(serilogConfiguration));
        }
    }
}
