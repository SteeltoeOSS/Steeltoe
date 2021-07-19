// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using System;
using System.Linq;
using static Steeltoe.Extensions.Logging.DynamicLoggingBuilder;

namespace Steeltoe.Extensions.Logging.DynamicSerilog
{
    public static class SerilogLoggingBuilderExtensions
    {
        public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder, IConfiguration configuration, LoggerConfiguration serilogConfiguration = null, bool preserveDefaultConsole = false)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // only run if an IDynamicLoggerProvider hasn't already been added
            if (!builder.Services.Any(sd => sd.ServiceType == typeof(IDynamicLoggerProvider)))
            {
                if (!preserveDefaultConsole)
                {
                    builder.ClearProviders();
                }

                builder.AddFilter<SerilogDynamicProvider>(null, LogLevel.Trace); // TODO : What does this do?
                builder.Services.AddOptions();

                if (serilogConfiguration != null)
                {
                    builder.Services.Configure<SerilogOptions>(options => options.SetSerilogOptions(serilogConfiguration));
                }
                else
                {
                    builder.Services.Configure<SerilogOptions>(options => options.SetSerilogOptions(configuration));
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
        public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder, bool preserveDefaultConsole = false)
        {
            var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            return builder.AddDynamicSerilog(configuration, null, preserveDefaultConsole);
        }

        /// <summary>
        /// Add Serilog, wrapped in a <see cref="IDynamicLoggerProvider"/> that supports
        /// dynamically controlling the minimum log level via management endpoints. Will add a Console sink if <paramref name="loggerConfiguration"/> is not provided.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for configuring the LoggerFactory</param>
        /// <param name="loggerConfiguration">An initial <see cref="LoggerConfiguration"/></param>
        /// <param name="preserveStaticLogger">Not supported!</param>
        /// <param name="preserveDefaultConsole">When true, do not remove Microsoft's ConsoleLoggerProvider</param>
        /// <returns>The configured <see cref="ILoggingBuilder"/></returns>
        /// TODO: Obsolete
        public static ILoggingBuilder AddDynamicSerilog(this ILoggingBuilder builder, LoggerConfiguration loggerConfiguration, bool preserveStaticLogger = false, bool preserveDefaultConsole = false)
        {
            var configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            return builder.AddDynamicSerilog(configuration, loggerConfiguration, preserveDefaultConsole);
        }

        /// <summary>
        /// Add Steeltoe logger wrapped in a <see cref="IDynamicLoggerProvider"/> that supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for configuring the LoggerFactory  </param>
        /// <returns>The configured <see cref="ILoggingBuilder"/></returns>
        [Obsolete("Please use AddDynamicSerilog instead")]
        public static ILoggingBuilder AddSerilogDynamicConsole(this ILoggingBuilder builder) => builder.AddDynamicSerilog();

        /// <summary>
        /// Add Steeltoe logger wrapped in a <see cref="IDynamicLoggerProvider"/> that supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for configuring the LoggerFactory  </param>
        /// <param name="loggerConfiguration">An initial <see cref="LoggerConfiguration"/></param>
        /// <param name="preserveStaticLogger">Indicates whether to preserve the value of <see cref="Log.Logger"/>.</param>
        /// <returns>The configured <see cref="ILoggingBuilder"/></returns>
        [Obsolete("Please use AddDynamicSerilog instead")]
        public static ILoggingBuilder AddSerilogDynamicConsole(this ILoggingBuilder builder, LoggerConfiguration loggerConfiguration, bool preserveStaticLogger = false) => builder.AddDynamicSerilog(loggerConfiguration, preserveStaticLogger);
    }
}
