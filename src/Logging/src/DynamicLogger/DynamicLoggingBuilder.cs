// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Steeltoe.Extensions.Logging
{
    public static class DynamicLoggingBuilder
    {
        /// <summary>
        /// Adds Dynamic Console Logger Provider
        /// </summary>
        /// <param name="builder">Your ILoggingBuilder</param>
        /// <param name="ensureCleanSetup">If true removes any <see cref="ConsoleLoggerProvider"/>, ensures logging config classes are available</param>
        public static ILoggingBuilder AddDynamicConsole(this ILoggingBuilder builder, bool ensureCleanSetup = false)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (ensureCleanSetup)
            {
                // remove the original ConsoleLoggerProvider to prevent duplicate logging
                var serviceDescriptor = builder.Services.FirstOrDefault(descriptor => descriptor.ImplementationType == typeof(ConsoleLoggerProvider));
                if (serviceDescriptor != null)
                {
                    builder.Services.Remove(serviceDescriptor);
                }

                // make sure logger provider configurations are available
                if (!builder.Services.Any(descriptor => descriptor.ServiceType == typeof(ILoggerProviderConfiguration<ConsoleLoggerProvider>)))
                {
                    builder.AddConfiguration();
                }
            }

            builder.AddFilter<DynamicConsoleLoggerProvider>(null, LogLevel.Trace);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DynamicConsoleLoggerProvider>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ConsoleLoggerOptions>, ConsoleLoggerOptionsSetup>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<ConsoleLoggerOptions>, LoggerProviderOptionsChangeTokenSource<ConsoleLoggerOptions, ConsoleLoggerProvider>>());
            builder.Services.AddSingleton((p) => p.GetServices<ILoggerProvider>().OfType<IDynamicLoggerProvider>().SingleOrDefault());
            return builder;
        }

        internal class ConsoleLoggerOptionsSetup : ConfigureFromConfigurationOptions<ConsoleLoggerOptions>
        {
            public ConsoleLoggerOptionsSetup(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
                : base(providerConfiguration.Configuration)
            {
            }
        }
    }
}
