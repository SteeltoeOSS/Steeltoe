// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.EndpointOwin.Hypermedia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
{
    public static class ActuatorContainerBuilderExtensions
    {
        /// <summary>
        /// Register the <see cref="ActuatorEndpoint"/>, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        public static void RegisterHypermediaActuator(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterInstance(new ActuatorManagementOptions(config))
                .SingleInstance()
                .As<IManagementOptions>();

            container.Register(c =>
            {
                var options = new HypermediaEndpointOptions(config);
                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>().OfType<ActuatorManagementOptions>().Single();

                mgmtOptions.EndpointOptions.Add(options);
                return options;
            }).As<IActuatorHypermediaOptions>().SingleInstance();
            container.RegisterType<ActuatorEndpoint>().SingleInstance();
            container.RegisterType<ActuatorHypermediaEndpointOwinMiddleware>().SingleInstance();
        }
    }
}
