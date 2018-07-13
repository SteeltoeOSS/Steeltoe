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
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.EndpointOwin;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointAutofac.Actuators
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
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterType<ThreadDumper>().As<IThreadDumper>().SingleInstance();
            container.RegisterInstance(new ThreadDumpOptions(config)).As<IThreadDumpOptions>();
            container.RegisterType<ThreadDumpEndpoint>().As<IEndpoint<List<ThreadInfo>>>().SingleInstance();
            container.RegisterType<EndpointOwinMiddleware<List<ThreadInfo>>>().SingleInstance();
        }

        /// <summary>
        /// Register the ThreadDump endpoint, HttpModule and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        //public static void RegisterThreadDumpModule(this ContainerBuilder container, IConfiguration config)
        //{
        //    if (container == null)
        //    {
        //        throw new ArgumentNullException(nameof(container));
        //    }

        //    if (config == null)
        //    {
        //        throw new ArgumentNullException(nameof(config));
        //    }

        //    container.RegisterType<ThreadDumper>().As<IThreadDumper>().SingleInstance();
        //    container.RegisterInstance(new ThreadDumpOptions(config)).As<IThreadDumpOptions>();
        //    container.RegisterType<ThreadDumpEndpoint>();
        //    container.RegisterType<ThreadDumpModule>().As<IHttpModule>();
        //}
    }
}
