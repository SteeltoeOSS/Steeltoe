// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.EndpointOwin;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
{
    public static class ThreadDumpContainerBuilderExtensions
    {
        /// <summary>
        /// Register the ThreadDump endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        public static void RegisterThreadDumpActuator(this ContainerBuilder container, IConfiguration config)
        {
            container.RegisterThreadDumpActuator(config, MediaTypeVersion.V1);
        }

        public static void RegisterThreadDumpActuator(this ContainerBuilder container, IConfiguration config, MediaTypeVersion version)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterType<ThreadDumper>().As<IThreadDumper>().SingleInstance();
            container.Register(c =>
            {
                var options = new ThreadDumpEndpointOptions(config);
                if (options.Id == "dump" && version == MediaTypeVersion.V2)
                {
                    options.Id = "threaddump";
                }

                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>();
                foreach (var mgmt in mgmtOptions)
                {
                    mgmt.EndpointOptions.Add(options);
                }

                return options;
            }).As<IThreadDumpOptions>().IfNotRegistered(typeof(IThreadDumpOptions));

            if (version == MediaTypeVersion.V1)
            {
                container.RegisterType<ThreadDumpEndpoint>().As<IEndpoint<List<ThreadInfo>>>().SingleInstance();

                container.RegisterType<EndpointOwinMiddleware<List<ThreadInfo>>>().SingleInstance();
            }
            else
            {
                container.RegisterType<ThreadDumpEndpoint_v2>().As<IEndpoint<ThreadDumpResult>>().SingleInstance();

                container.RegisterType<EndpointOwinMiddleware<ThreadDumpResult>>().SingleInstance();
            }
        }
    }
}
