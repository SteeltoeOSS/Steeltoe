// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog.Core;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger
{
    public static class SerilogBuilderExtensions
    {
        /// <summary>
        /// Add Steeltoe logger wrapped in a <see cref="IDynamicLoggerProvider"/> that supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> for configuring the LoggerFactory  </param>
        /// <returns>The configured <see cref="ILoggingBuilder"/></returns>
        public static ILoggingBuilder AddSerilogDynamicConsole(this ILoggingBuilder builder)
        {
            builder.AddFilter<DynamicLoggerProvider>(null, LogLevel.Trace);
            builder.Services.AddSingleton<ISerilogOptions, SerilogOptions>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SerilogDynamicProvider>());

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<ConsoleLoggerOptions>, LoggerProviderOptionsChangeTokenSource<ConsoleLoggerOptions, ConsoleLoggerProvider>>());
            builder.Services.AddSingleton<IDynamicLoggerProvider>((p) => p.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().SingleOrDefault());
            return builder;
        }

        /// <summary>
        /// Sets Steeltoe <see cref="IDynamicLoggerProvider"/> Serilog implementation as a LoggerProvider which supports
        /// dynamically controlling the minimum log level via management endpoints
        /// </summary>
        /// <param name="builder">The <see cref="IWebHostBuilder"/> for configuring the WebHostBuilder  </param>
        /// <param name="configureLogger">The delegate for configuring the <see cref="LoggerConfiguration" /> that will be used to construct a <see cref="Logger" /></param>
        /// <returns>The <see cref="IWebHostBuilder"/></returns>
        public static IWebHostBuilder UseSerilogDynamicConsole(this IWebHostBuilder builder, Action<WebHostBuilderContext, Serilog.LoggerConfiguration> configureLogger)
        {
            void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
            {
                services.AddSingleton<ISerilogOptions, SerilogOptions>();
                var loggerConfiguration = new Serilog.LoggerConfiguration();
                configureLogger(context, loggerConfiguration);

                var serilogOptions = new SerilogOptions(context.Configuration);

                // Add a level switch that controls the "Default" level at the root
                var levelSwitch = new LoggingLevelSwitch(serilogOptions.MinimumLevel.Default);
                loggerConfiguration.MinimumLevel.ControlledBy(levelSwitch);

                var logger = loggerConfiguration.CreateLogger();

                services.AddSingleton<IDynamicLoggerProvider>(sp => new SerilogDynamicProvider(sp.GetRequiredService<IConfiguration>(), logger, levelSwitch));
                services.AddSingleton<ILoggerFactory>(sp => new SerilogDynamicLoggerFactory(sp.GetRequiredService<IDynamicLoggerProvider>()));
            }

            builder.ConfigureServices(ConfigureServices);

            return builder;
        }
    }
}
