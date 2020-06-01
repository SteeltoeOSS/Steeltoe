// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.EndpointOwin.HeapDump;
using System;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
{
    public static class HeapDumpContainerBuilderExtensions
    {
        /// <summary>
        /// Register the HeapDump endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        public static void RegisterHeapDumpActuator(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // container.RegisterInstance(new HeapDumpOptions(config)).As<IHeapDumpOptions>();
            container.Register(c =>
            {
                var options = new HeapDumpEndpointOptions(config);
                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>();
                foreach (var mgmt in mgmtOptions)
                {
                    mgmt.EndpointOptions.Add(options);
                }

                return options;
            }).As<IHeapDumpOptions>().IfNotRegistered(typeof(IHeapDumpOptions));

            // REVIEW: is this path override necessary? Running under IIS Express, the path comes up wrong
            container.RegisterType<HeapDumper>().As<IHeapDumper>().WithParameter("basePathOverride", GetContentRoot()).SingleInstance();
            container.RegisterType<HeapDumpEndpoint>().SingleInstance();
            container.RegisterType<HeapDumpEndpointOwinMiddleware>().SingleInstance();
        }

        private static string GetContentRoot()
        {
            var basePath = (string)AppDomain.CurrentDomain.GetData("APP_CONTEXT_BASE_DIRECTORY") ?? AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(basePath);
        }
    }
}
