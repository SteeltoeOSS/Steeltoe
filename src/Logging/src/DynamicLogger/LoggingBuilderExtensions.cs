// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Logging.DynamicLogger;

public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Replaces the built-in <see cref="ConsoleLoggerProvider" /> with <see cref="DynamicConsoleLoggerProvider" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ILoggingBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static ILoggingBuilder AddDynamicConsole(this ILoggingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!IsDynamicLoggerProviderAlreadyRegistered(builder))
        {
            EnsureConsoleLoggingIsRegistered(builder);
            UpdateConsoleLoggerProviderRegistration(builder.Services);

            builder.AddFilter<DynamicConsoleLoggerProvider>(null, LogLevel.Trace);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DynamicConsoleLoggerProvider>());
            builder.Services.AddSingleton(provider => provider.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().Single());

            DisableConsoleColorsOnCloudPlatform(builder);
        }

        return builder;
    }

    private static bool IsDynamicLoggerProviderAlreadyRegistered(ILoggingBuilder builder)
    {
        return builder.Services.Any(descriptor => descriptor.ServiceType == typeof(IDynamicLoggerProvider));
    }

    private static void EnsureConsoleLoggingIsRegistered(ILoggingBuilder builder)
    {
        if (builder.Services.All(descriptor => descriptor.ImplementationType != typeof(ConsoleLoggerProvider)))
        {
            builder.AddConsole();
        }
    }

    private static void UpdateConsoleLoggerProviderRegistration(IServiceCollection services)
    {
        // Remove the original ConsoleLoggerProvider registration as ILoggerProvider to prevent duplicate logging.
        ServiceDescriptor? descriptor = services.FirstOrDefault(descriptor => descriptor.ImplementationType == typeof(ConsoleLoggerProvider));

        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        // Yet we need an instance from the container to construct DynamicConsoleLoggerProvider, so register without interface.
        services.AddSingleton<ConsoleLoggerProvider>();
    }

    private static void DisableConsoleColorsOnCloudPlatform(ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IConfigureOptions<SimpleConsoleFormatterOptions>, SimpleConsoleLoggerFormatterConfigureOptions>());
    }

    private sealed class SimpleConsoleLoggerFormatterConfigureOptions : ConfigureFromConfigurationOptions<SimpleConsoleFormatterOptions>
    {
        public SimpleConsoleLoggerFormatterConfigureOptions(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration.GetSection("FormatterOptions"))
        {
        }

        public override void Configure(SimpleConsoleFormatterOptions options)
        {
            base.Configure(options);

            if (Platform.IsCloudHosted)
            {
                options.ColorBehavior = LoggerColorBehavior.Disabled;
            }
        }
    }
}
