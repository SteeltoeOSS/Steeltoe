// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Linq;

namespace Steeltoe.Extensions.Logging.DynamicSerilog
{
    public static class SerilogLoggingBuilderExtensions
    {
        /// <summary>
        /// Add Steeltoe logger wrapped in a <see cref="IDynamicLoggerProvider"/> that supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for configuring the LoggerFactory  </param>
        /// <returns>The configured <see cref="ILoggingBuilder"/></returns>
        public static ILoggingBuilder AddSerilogDynamicConsole(this ILoggingBuilder builder)
        {
            return builder.AddSerilogDynamicConsole(null, false);
        }

        /// <summary>
        /// Add Steeltoe logger wrapped in a <see cref="IDynamicLoggerProvider"/> that supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for configuring the LoggerFactory  </param>
        /// <param name="loggerConfiguration">An initial <see cref="LoggerConfiguration"/></param>
        /// <param name="preserveStaticLogger">Indicates whether to preserve the value of <see cref="Log.Logger"/>.</param>
        /// <returns>The configured <see cref="ILoggingBuilder"/></returns>
        public static ILoggingBuilder AddSerilogDynamicConsole(this ILoggingBuilder builder, LoggerConfiguration loggerConfiguration, bool preserveStaticLogger = false)
        {
            if (builder.Services.Any(sd => sd.ServiceType == typeof(IDynamicLoggerProvider)))
            {
                throw new InvalidOperationException("An IDynamicLoggerProvider has already been configured! Call 'AddSerilogDynamicConsole' earlier in program.cs (before adding Actuators) or remove duplicate IDynamicLoggerProvider entries.");
            }

            var serilogOptions = new SerilogOptions(builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>());
            loggerConfiguration ??= new LoggerConfiguration();

            // Add a level switch that controls the "Default" level at the root
            var levelSwitch = new LoggingLevelSwitch(serilogOptions.MinimumLevel.Default);
            loggerConfiguration.MinimumLevel.ControlledBy(levelSwitch);
            var logger = loggerConfiguration.CreateLogger();
            if (!preserveStaticLogger)
            {
                Log.Logger = logger;
            }

            builder.Services.AddSingleton<ISerilogOptions>(serilogOptions);
            builder.Services.AddSingleton(levelSwitch);
            builder.Services.AddSingleton(logger);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SerilogDynamicProvider>());
            builder.Services.AddSingleton((p) => p.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().SingleOrDefault());
            return builder;
        }
    }
}
