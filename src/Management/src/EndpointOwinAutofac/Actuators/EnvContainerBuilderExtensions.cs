// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.EndpointOwin;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
{
    public static class EnvContainerBuilderExtensions
    {
        /// <summary>
        /// Register the ENV endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="hostingEnv">A class describing the app hosting environment - defaults to <see cref="GenericHostingEnvironment"/></param>
        public static void RegisterEnvActuator(this ContainerBuilder container, IConfiguration config, IHostingEnvironment hostingEnv = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.Register(c =>
            {
                var envOptions = new EnvEndpointOptions(config);
                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>();
                foreach (var mgmt in mgmtOptions)
                {
                    mgmt.EndpointOptions.Add(envOptions);
                }

                return envOptions;
            }).As<IEnvOptions>().IfNotRegistered(typeof(IEnvOptions));

            container.RegisterInstance(hostingEnv ?? new GenericHostingEnvironment() { EnvironmentName = "Production" }).As<IHostingEnvironment>();
            container.RegisterType<EnvEndpoint>().As<IEndpoint<EnvironmentDescriptor>>().SingleInstance();
            container.RegisterType<EndpointOwinMiddleware<EnvironmentDescriptor>>().SingleInstance();
        }
    }
}
