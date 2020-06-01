// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Logging.Autofac;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint;
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
        public static void RegisterLoggersActuator(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterLogging(config);
            container.RegisterType<DynamicConsoleLoggerProvider>().As<IDynamicLoggerProvider>();
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
