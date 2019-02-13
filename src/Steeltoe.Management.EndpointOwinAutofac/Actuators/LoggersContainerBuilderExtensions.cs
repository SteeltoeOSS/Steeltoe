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
using Steeltoe.Common.Logging.Autofac;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Discovery;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.EndpointOwin.Loggers;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
{
    public static class LoggersContainerBuilderExtensions
    {
        /// <summary>
        /// Register the Loggers endpoint, middleware and options<para />Steeltoe's <see cref="DynamicConsoleLogger"/> will be configured and included in the DI container
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="loggerProvider">Your pre-existing <see cref="DynamicLoggerProvider"/> will be created if not provided</param>
        /// <param name="loggerFactory">Your pre-existing <see cref="ILoggerFactory"/>. A new <see cref="LoggerFactory"/> will be added if not provided</param>
        public static void RegisterLoggersActuator(this ContainerBuilder container, IConfiguration config, DynamicLoggerProvider loggerProvider = null, ILoggerFactory loggerFactory = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (loggerProvider == null)
            {
                loggerProvider = new DynamicLoggerProvider(new ConsoleLoggerSettings().FromConfiguration(config));
            }

            if (loggerFactory == null)
            {
                loggerFactory = new LoggerFactory(new List<ILoggerProvider> { loggerProvider });
            }

            container.RegisterLogging(config);
            container.RegisterInstance(loggerProvider).As<IDynamicLoggerProvider>();
            container.Register(c =>
            {
                var options = new LoggersEndpointOptions(config);
                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>();
                foreach (var mgmt in mgmtOptions)
                {
                    mgmt.EndpointOptions.Add(options);
                }
                return options;
            }).As<ILoggersOptions>().IfNotRegistered(typeof(ILoggersOptions));
            container.RegisterType<LoggersEndpoint>().SingleInstance();
            container.RegisterType<LoggersEndpointOwinMiddleware>().SingleInstance();
        }
    }
}
