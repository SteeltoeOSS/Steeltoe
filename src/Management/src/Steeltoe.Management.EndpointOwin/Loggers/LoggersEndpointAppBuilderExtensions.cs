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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.Loggers;
using System;

namespace Steeltoe.Management.EndpointOwin.Loggers
{
    public static class LoggersEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add Loggers actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring loggers endpoint</param>
        /// <param name="loggerProvider">Provider of loggers to report on and configure</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Loggers Endpoint added</returns>
        public static IAppBuilder UseLoggersActuator(this IAppBuilder builder, IConfiguration config, ILoggerProvider loggerProvider, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (loggerProvider == null)
            {
                throw new ArgumentNullException(nameof(loggerProvider));
            }

            var options = new LoggersEndpointOptions(config);
            var mgmtOptions = ManagementOptions.Get(config);
            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            var endpoint = new LoggersEndpoint(options, loggerProvider as IDynamicLoggerProvider, loggerFactory?.CreateLogger<LoggersEndpoint>());
            var logger = loggerFactory?.CreateLogger<LoggersEndpointOwinMiddleware>();
            return builder.Use<LoggersEndpointOwinMiddleware>(endpoint, mgmtOptions, logger);
        }
    }
}
