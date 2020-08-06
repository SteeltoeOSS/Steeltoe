// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Steeltoe.Extensions.Logging.DynamicSerilog;
using System;

namespace Steeltoe.Extensions.Logging.DynamicSerilogCore
{
    public static class SerilogBuilderExtensions
    {
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
