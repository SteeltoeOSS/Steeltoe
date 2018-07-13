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
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.EndpointOwin.CloudFoundry;
using System;

namespace Steeltoe.Management.EndpointAutofac.Actuators
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

            container.RegisterInstance(new CloudFoundryOptions(config)).As<ICloudFoundryOptions>();
            container.RegisterType<CloudFoundryEndpoint>();
            container.RegisterType<CloudFoundryEndpointOwinMiddleware>();
        }

        /// <summary>
        /// Add security checks on requests to OWIN middlewares
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        public static void RegisterCloudFoundrySecurityActuator(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterType<CloudFoundrySecurityOwinMiddleware>();
        }

        /// <summary>
        /// Register the Cloud Foundry endpoint, HttpModule and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        //public static void RegisterCloudFoundryModule(this ContainerBuilder container, IConfiguration config)
        //{
        //    if (container == null)
        //    {
        //        throw new ArgumentNullException(nameof(container));
        //    }

        //    if (config == null)
        //    {
        //        throw new ArgumentNullException(nameof(config));
        //    }

        //    container.RegisterInstance(new CloudFoundryOptions(config)).As<ICloudFoundryOptions>();
        //    container.RegisterType<CloudFoundryEndpoint>();
        //    container.RegisterType<CloudFoundryModule>().As<IHttpModule>();
        //}
    }
}
