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
using Steeltoe.Management.Endpoint.ThreadDump;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Steeltoe.Management.EndpointOwin.ThreadDump
{
    public static class ThreadDumpEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add Thread Dump actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring thread dump endpoint</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Thread Dump Endpoint added</returns>
        public static IAppBuilder UseThreadDumpActuator(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var options = new ThreadDumpEndpointOptions(config);
            var mgmtOptions = ManagementOptions.Get(config);
            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            var threadDumper = new ThreadDumper(options, loggerFactory?.CreateLogger<ThreadDumper>());
            return builder.UseThreadDumpActuator(options, threadDumper, loggerFactory);
        }

        /// <summary>
        /// Add HealthCheck actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="options">Options for configuring the thread dump endpoint</param>
        /// <param name="threadDumper">Class responsible for dumping threads</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Health Endpoint added</returns>
        public static IAppBuilder UseThreadDumpActuator(this IAppBuilder builder, IThreadDumpOptions options, IThreadDumper threadDumper, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (threadDumper == null)
            {
                throw new ArgumentNullException(nameof(threadDumper));
            }

            var endpoint = new ThreadDumpEndpoint(options, threadDumper, loggerFactory?.CreateLogger<ThreadDumpEndpoint>());
            var logger = loggerFactory?.CreateLogger<EndpointOwinMiddleware<List<ThreadInfo>>>();
            var mgmtOptions = ManagementOptions.Get();
            return builder.Use<EndpointOwinMiddleware<List<ThreadInfo>>>(endpoint, mgmtOptions, new List<HttpMethod> { HttpMethod.Get }, true, logger);
        }
    }
}
