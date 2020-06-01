// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.EndpointOwin;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
{
    public static class InfoContainerBuilderExtensions
    {
        /// <summary>
        /// Register the Info endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        public static void RegisterInfoActuator(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterInfoActuator(config, new GitInfoContributor(AppDomain.CurrentDomain.BaseDirectory + "git.properties"), new AppSettingsInfoContributor(config));
        }

        /// <summary>
        /// Register the Info endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="contributors">Contributors to application information</param>
        public static void RegisterInfoActuator(this ContainerBuilder container, IConfiguration config, params IInfoContributor[] contributors)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            foreach (var c in contributors)
            {
                container.RegisterInstance(c).As<IInfoContributor>().SingleInstance();
            }

            container.Register(c =>
            {
                var options = new InfoEndpointOptions(config);
                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>();
                foreach (var mgmt in mgmtOptions)
                {
                    mgmt.EndpointOptions.Add(options);
                }

                return options;
            }).As<IInfoOptions>().IfNotRegistered(typeof(IInfoOptions)).SingleInstance();
            container.RegisterType<InfoEndpoint>().IfNotRegistered(typeof(InfoEndpoint)).As<IEndpoint<Dictionary<string, object>>>().SingleInstance();
            container.RegisterType<EndpointOwinMiddleware<Dictionary<string, object>>>().IfNotRegistered(typeof(EndpointOwinMiddleware<Dictionary<string, object>>)).SingleInstance();
        }
    }
}
