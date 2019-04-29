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
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Management.Endpoint;
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

            return builder.UseThreadDumpActuator(config, loggerFactory, MediaTypeVersion.V1);
        }

        /// <summary>
        /// Add Thread Dump actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring thread dump endpoint</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <param name="version">MediaTypeVersion for endpoint response</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Thread Dump Endpoint added</returns>
        public static IAppBuilder UseThreadDumpActuator(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory, MediaTypeVersion version)
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

            if (version == MediaTypeVersion.V2 && options.Id == "dump")
            {
                options.Id = "threaddump";
            }

            var mgmtOptions = ManagementOptions.Get(config);
            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            var threadDumper = new ThreadDumper(options, loggerFactory?.CreateLogger<ThreadDumper>());
            return builder.UseThreadDumpActuator(options, threadDumper, loggerFactory, version);
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

            return builder.UseThreadDumpActuator(options, threadDumper, loggerFactory, MediaTypeVersion.V1);
        }

        /// <summary>
        /// Add HealthCheck actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="options">Options for configuring the thread dump endpoint</param>
        /// <param name="threadDumper">Class responsible for dumping threads</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <param name="version">MediaType version of the response</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Health Endpoint added</returns>
        public static IAppBuilder UseThreadDumpActuator(this IAppBuilder builder, IThreadDumpOptions options, IThreadDumper threadDumper, ILoggerFactory loggerFactory, MediaTypeVersion version)
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

            switch (version)
            {
                case MediaTypeVersion.V1:
                    return UseThreadDumpComponents(builder, options, threadDumper, loggerFactory);
                default:
                    return UseThreadDumpV2Components(builder, options, threadDumper, loggerFactory);
            }
        }

        private static IAppBuilder UseThreadDumpComponents(IAppBuilder builder, IThreadDumpOptions options, IThreadDumper threadDumper, ILoggerFactory loggerFactory)
        {
            var endpoint = new ThreadDumpEndpoint(options, threadDumper, loggerFactory?.CreateLogger<ThreadDumpEndpoint>());
            var logger = loggerFactory?.CreateLogger<EndpointOwinMiddleware<List<ThreadInfo>>>();
            var mgmtOptions = ManagementOptions.Get();
            return builder.Use<EndpointOwinMiddleware<List<ThreadInfo>>>(endpoint, mgmtOptions, new List<HttpMethod> { HttpMethod.Get }, true, logger);
        }

        private static IAppBuilder UseThreadDumpV2Components(IAppBuilder builder, IThreadDumpOptions options, IThreadDumper threadDumper, ILoggerFactory loggerFactory)
        {
            var endpoint = new ThreadDumpEndpoint_v2(options, threadDumper, loggerFactory?.CreateLogger<ThreadDumpEndpoint_v2>());
            var logger = loggerFactory?.CreateLogger<EndpointOwinMiddleware<ThreadDumpResult>>();
            var mgmtOptions = ManagementOptions.Get();
            return builder.Use<EndpointOwinMiddleware<ThreadDumpResult>>(endpoint, mgmtOptions, new List<HttpMethod> { HttpMethod.Get }, true, logger);
        }
    }
}
