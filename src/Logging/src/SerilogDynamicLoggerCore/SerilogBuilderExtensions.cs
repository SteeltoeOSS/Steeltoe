// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Serilog.Core;
using System;
using System.Linq;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger
{
    public static class SerilogBuilderExtensions
    {
        /// <summary>
        /// Add Serilog logger wrapped in a <see cref="IDynamicLoggerProvider"/> that supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for configuring the LoggerFactory  </param>
        /// <returns>The configured <see cref="ILoggingBuilder"/></returns>
        public static ILoggingBuilder AddSerilogDynamicConsole(this ILoggingBuilder builder)
        {
            if (builder.Services.Any(sd => sd.ServiceType == typeof(IDynamicLoggerProvider)))
            {
                throw new InvalidOperationException("An IDynamicLoggerProvider has already been configured! Call 'AddSerilogDynamicConsole' earlier in program.cs (Before AddCloudFoundryActuators()) or remove duplicate IDynamicLoggerProvider entries.");
            }

            var defaultConsoleDescriptor = builder.Services.FirstOrDefault(d => d.ImplementationType == typeof(ConsoleLoggerProvider));
            if (defaultConsoleDescriptor != null)
            {
                builder.Services.Remove(defaultConsoleDescriptor);
            }

            builder.Services.AddSingleton<ISerilogOptions, SerilogOptions>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SerilogDynamicProvider>());

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<ConsoleLoggerOptions>, LoggerProviderOptionsChangeTokenSource<ConsoleLoggerOptions, ConsoleLoggerProvider>>());
            builder.Services.AddSingleton((p) => p.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().SingleOrDefault());
            return builder;
        }

        /// <summary>
        /// Sets Steeltoe <see cref="IDynamicLoggerProvider"/> Serilog implementation as a LoggerProvider which supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="IWebHostBuilder"/> for configuring the WebHostBuilder  </param>
        /// <param name="configureLogger">The delegate for configuring the <see cref="DynamicLoggerConfiguration" /> that will be used to construct a <see cref="Serilog.Core.Logger" /></param>
        /// <param name="preserveStaticLogger">Indicates whether to preserve the value of <see cref="Serilog.Log.Logger"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/></returns>
        public static IHostBuilder UseSerilogDynamicConsole(this IHostBuilder builder, Action<HostBuilderContext, Serilog.LoggerConfiguration> configureLogger, bool preserveStaticLogger = false)
        {
            builder.ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
            {
                services.AddSingleton<ISerilogOptions, SerilogOptions>();
                var loggerConfiguration = new Serilog.LoggerConfiguration();
                configureLogger(context, loggerConfiguration);

                var serilogOptions = new SerilogOptions(context.Configuration);

                // Add a level switch that controls the "Default" level at the root
                var levelSwitch = new LoggingLevelSwitch(serilogOptions.MinimumLevel.Default);
                loggerConfiguration.MinimumLevel.ControlledBy(levelSwitch);

                var logger = loggerConfiguration.CreateLogger();

                if (!preserveStaticLogger)
                {
                    Serilog.Log.Logger = logger;
                }

                services.AddSingleton<IDynamicLoggerProvider>(sp => new SerilogDynamicProvider(sp.GetRequiredService<IConfiguration>(), serilogOptions, logger, levelSwitch));
                services.AddSingleton<ILoggerFactory>(sp => new SerilogDynamicLoggerFactory(sp.GetRequiredService<IDynamicLoggerProvider>()));
            });

            return builder;
        }

        /// <summary>
        /// Sets Steeltoe <see cref="IDynamicLoggerProvider"/> Serilog implementation as a LoggerProvider which supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="IWebHostBuilder"/> for configuring the WebHostBuilder  </param>
        /// <param name="configureLogger">The delegate for configuring the <see cref="DynamicLoggerConfiguration" /> that will be used to construct a <see cref="Serilog.Core.Logger" /></param>
        /// <param name="preserveStaticLogger">Indicates whether to preserve the value of <see cref="Serilog.Log.Logger"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/></returns>
        public static IWebHostBuilder UseSerilogDynamicConsole(this IWebHostBuilder builder, Action<WebHostBuilderContext, Serilog.LoggerConfiguration> configureLogger, bool preserveStaticLogger = false)
        {
            builder.ConfigureServices((WebHostBuilderContext context, IServiceCollection services) =>
            {
                services.AddSingleton<ISerilogOptions, SerilogOptions>();
                var loggerConfiguration = new Serilog.LoggerConfiguration();
                configureLogger(context, loggerConfiguration);

                var serilogOptions = new SerilogOptions(context.Configuration);

                // Add a level switch that controls the "Default" level at the root
                var levelSwitch = new LoggingLevelSwitch(serilogOptions.MinimumLevel.Default);
                loggerConfiguration.MinimumLevel.ControlledBy(levelSwitch);

                var logger = loggerConfiguration.CreateLogger();

                if (!preserveStaticLogger)
                {
                    Serilog.Log.Logger = logger;
                }

                services.AddSingleton<IDynamicLoggerProvider>(sp => new SerilogDynamicProvider(sp.GetRequiredService<IConfiguration>(), serilogOptions, logger, levelSwitch));
                services.AddSingleton<ILoggerFactory>(sp => new SerilogDynamicLoggerFactory(sp.GetRequiredService<IDynamicLoggerProvider>()));
            });

            return builder;
        }
    }
}
