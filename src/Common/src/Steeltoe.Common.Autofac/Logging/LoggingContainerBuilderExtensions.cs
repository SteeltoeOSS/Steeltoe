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
