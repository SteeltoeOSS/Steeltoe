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
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.EndpointOwin;
using System;

namespace Steeltoe.Management.EndpointAutofac.Actuators
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

            container.RegisterInstance(new EnvOptions(config)).As<IEnvOptions>();
            container.RegisterInstance(hostingEnv ?? new GenericHostingEnvironment()).As<IHostingEnvironment>();
            container.RegisterType<EnvEndpoint>().As<IEndpoint<EnvironmentDescriptor>>().SingleInstance();
            container.RegisterType<EndpointOwinMiddleware<EnvironmentDescriptor>>().SingleInstance();
        }

        /// <summary>
        /// Register the ENV endpoint, Http Module and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="hostingEnv">A class describing the app hosting environment - defaults to <see cref="GenericHostingEnvironment"/></param>
        //public static void RegisterEnvModule(this ContainerBuilder container, IConfiguration config, IHostingEnvironment hostingEnv = null)
        //{
        //    if (container == null)
        //    {
        //        throw new System.ArgumentNullException(nameof(container));
        //    }

        //    if (config == null)
        //    {
        //        throw new System.ArgumentNullException(nameof(config));
        //    }

        //    container.RegisterInstance(new EnvOptions(config)).As<IEnvOptions>();
        //    container.RegisterInstance(hostingEnv ?? new GenericHostingEnvironment()).As<IHostingEnvironment>();
        //    container.RegisterType<EnvEndpoint>().As<IEndpoint<EnvironmentDescriptor>>();
        //    container.RegisterType<EnvModule>().As<IHttpModule>();
        //}
    }
}
