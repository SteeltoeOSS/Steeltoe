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
