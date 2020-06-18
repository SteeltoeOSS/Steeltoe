// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public static class EndpointServiceCollectionExtensions
    {
        /// <summary>
        /// Adds components of the Thread Dump actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add actuator to</param>
        /// <param name="config">Application configuration (this actuator looks for settings starting with management:endpoints:dump)</param>
        public static void AddThreadDumpActuator(this IServiceCollection services, IConfiguration config)
        {
            services.AddThreadDumpActuator(config, MediaTypeVersion.V1);
        }

        public static void AddThreadDumpActuator(this IServiceCollection services, IConfiguration config, MediaTypeVersion version)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddActuatorManagementOptions(config);
            var options = new ThreadDumpEndpointOptions(config);
            if (version == MediaTypeVersion.V1)
            {
                services.TryAddSingleton<ThreadDumpEndpoint>();
            }
            else
            {
                if (options.Id == "dump")
                {
                    options.Id = "threaddump";
                }

                services.TryAddSingleton<ThreadDumpEndpoint_v2>();
            }

            services.TryAddSingleton<IThreadDumpOptions>(options);
            services.RegisterEndpointOptions(options);
            services.TryAddSingleton<IThreadDumper, ThreadDumper>();
        }
    }
}
