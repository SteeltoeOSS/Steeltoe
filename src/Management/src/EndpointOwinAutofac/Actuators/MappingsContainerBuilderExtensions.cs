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
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.EndpointOwin.Mappings;
using System;
using System.Collections.Generic;
using System.Web.Http.Description;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
{
    public static class MappingsContainerBuilderExtensions
    {
        /// <summary>
        /// Register the Mappings endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="apiExplorer"><see cref="ApiExplorer"/> for iterating registered routes</param>
        public static void RegisterMappingsActuator(this ContainerBuilder container, IConfiguration config, IApiExplorer apiExplorer)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (apiExplorer == null)
            {
                throw new ArgumentNullException(nameof(apiExplorer));
            }

            container.Register(c =>
            {
                var options = new MappingsEndpointOptions(config);
                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>();
                foreach (var mgmt in mgmtOptions)
                {
                    mgmt.EndpointOptions.Add(options);
                }

                return options;
            }).As<IMappingsOptions>().IfNotRegistered(typeof(IMappingsOptions));

            container.RegisterInstance(apiExplorer);
            container.RegisterType<MappingsEndpoint>().SingleInstance();
            container.RegisterType<MappingsEndpointOwinMiddleware>().SingleInstance();
        }
    }
}
