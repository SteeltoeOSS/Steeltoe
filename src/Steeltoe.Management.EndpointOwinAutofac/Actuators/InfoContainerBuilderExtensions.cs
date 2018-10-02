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

            container.RegisterInstance(new InfoOptions(config)).As<IInfoOptions>().SingleInstance();
            container.RegisterType<InfoEndpoint>().As<IEndpoint<Dictionary<string, object>>>().SingleInstance();
            container.RegisterType<EndpointOwinMiddleware<Dictionary<string, object>>>().SingleInstance();
        }
    }
}
