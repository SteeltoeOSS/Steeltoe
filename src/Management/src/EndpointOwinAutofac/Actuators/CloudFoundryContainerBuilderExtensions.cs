// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.EndpointOwin.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
{
    public static class CloudFoundryContainerBuilderExtensions
    {
        /// <summary>
        /// Register the Cloud Foundry endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        public static void RegisterCloudFoundryActuator(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterInstance(new CloudFoundryManagementOptions())
                .SingleInstance()
                .As<IManagementOptions>();

            container.Register(c =>
            {
                var options = new CloudFoundryEndpointOptions(config);
                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>().OfType<CloudFoundryManagementOptions>().Single();
                mgmtOptions.EndpointOptions.Add(options);
                return options;
            }).As<ICloudFoundryOptions>().SingleInstance();
            container.RegisterType<CloudFoundryEndpoint>().SingleInstance();
            container.RegisterType<CloudFoundryEndpointOwinMiddleware>().SingleInstance();
        }

        /// <summary>
        /// Add security checks on requests to OWIN middlewares
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        public static void RegisterCloudFoundrySecurityMiddleware(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterType<CloudFoundrySecurityOwinMiddleware>().SingleInstance();
        }
    }
}
