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
using Serilog;
using Serilog.Core;
using System;
using System.Linq;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger
{
    public static class SerilogBuilderExtensions
    {
        /// <summary>
        /// Add Serilog with Console sink, wrapped in a <see cref="IDynamicLoggerProvider"/> that supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for configuring the LoggerFactory</param>
        /// <param name="serilogConfiguration">The <see cref="Serilog.LoggerConfiguration"/></param>
        /// <param name="preserveDefaultConsole">When true, do not remove Microsoft's ConsoleLoggerProvider</param>
        /// <returns>The configured <see cref="ILoggingBuilder"/></returns>
        public static ILoggingBuilder AddSerilogDynamicConsole(this ILoggingBuilder builder, LoggerConfiguration serilogConfiguration, bool preserveDefaultConsole = false)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (builder.Services.Any(sd => sd.ServiceType == typeof(IDynamicLoggerProvider)))
            {
                throw new InvalidOperationException("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier in program.cs (before adding Actuators) or remove duplicate IDynamicLoggerProvider entries.");
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
                    builder.Services.AddOptions<SerilogOptions>()
                        .Configure<IConfiguration>((options, iconfiguration) => options.SetSerilogOptions(iconfiguration));
                }

                builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SerilogDynamicProvider>());
                builder.Services.AddSingleton((p) => p.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().SingleOrDefault());
            }

            return builder;
        }

        /// <summary>
        /// Add Serilog with Console sink, wrapped in a <see cref="IDynamicLoggerProvider"/> that supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for configuring the LoggerFactory</param>
        /// <param name="preserveDefaultConsole">When true, do not remove Microsoft's ConsoleLoggerProvider</param>
        /// <returns>The configured <see cref="ILoggingBuilder"/></returns>
        public static ILoggingBuilder AddSerilogDynamicConsole(this ILoggingBuilder builder, bool preserveDefaultConsole = false)
        {
            return builder.AddSerilogDynamicConsole(null, preserveDefaultConsole);
        }

        /// <summary>
        /// Sets Steeltoe <see cref="IDynamicLoggerProvider"/> Serilog implementation as a LoggerProvider which supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="IWebHostBuilder"/> for configuring the WebHostBuilder  </param>
        /// <param name="configureLogger">The delegate for configuring the <see cref="DynamicLoggerConfiguration" /> that will be used to construct a <see cref="Serilog.Core.Logger" /></param>
        /// <param name="preserveStaticLogger">Not Supported!</param>
        /// <returns>The <see cref="IWebHostBuilder"/></returns>
        public static IHostBuilder UseSerilogDynamicConsole(this IHostBuilder builder, Action<HostBuilderContext, Serilog.LoggerConfiguration> configureLogger, bool preserveStaticLogger = false)
        {
            return builder
                 .ConfigureLogging((hostContext, logBuilder) =>
                 {
                     LoggerConfiguration loggerConfiguration = null;

                     if (configureLogger is object)
                     {
                         loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration);
                         configureLogger(hostContext, loggerConfiguration);
                     }

                     logBuilder.AddSerilogDynamicConsole(loggerConfiguration, false);
                 });
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
            return builder.ConfigureServices((WebHostBuilderContext hostContext, IServiceCollection services) =>
            {
                services.AddLogging((logBuilder) =>
                {
                    LoggerConfiguration loggerConfiguration = null;
                    if (configureLogger is object)
                    {
                        loggerConfiguration = new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration);
                        configureLogger(hostContext, loggerConfiguration);

                        logBuilder.AddSerilogDynamicConsole(loggerConfiguration, false);
                    }
                    else
                    {
                        logBuilder.AddSerilogDynamicConsole(false);
                    }
                });
            });
        }
    }
}
