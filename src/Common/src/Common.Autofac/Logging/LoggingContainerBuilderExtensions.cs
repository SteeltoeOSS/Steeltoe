// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;

namespace Steeltoe.Common.Logging.Autofac
{
    public static class LoggingContainerBuilderExtensions
    {
        public static void RegisterLogging(this ContainerBuilder container, IConfiguration configuration)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            container.RegisterType<LoggerFactory>().As<ILoggerFactory>().SingleInstance();
            container.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();
            container.RegisterInstance(new DefaultLoggerLevelConfigureOptions(LogLevel.Information)).As<IConfigureOptions<LoggerFilterOptions>>().SingleInstance();

            var config = configuration.GetSection("Logging");
            container.RegisterInstance(new LoggerFilterConfigureOptions(config)).As<IConfigureOptions<LoggerFilterOptions>>().SingleInstance();
            container.RegisterInstance(new ConfigurationChangeTokenSource<LoggerFilterOptions>(config)).As<IOptionsChangeTokenSource<LoggerFilterOptions>>().SingleInstance();
        }

        public static void RegisterConsoleLogging(this ContainerBuilder container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.RegisterType<ConsoleLoggerProvider>().As<ILoggerProvider>();
        }
    }
}
