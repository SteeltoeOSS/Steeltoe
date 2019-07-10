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
using Steeltoe.Common;
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
