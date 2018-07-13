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
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.EndpointOwin;
using Steeltoe.Management.EndpointOwin.Health;
using System;

namespace Steeltoe.Management.EndpointAutofac.Actuators
{
    public static class HealthContainerBuilderExtensions
    {
        /// <summary>
        /// Register the Health endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        public static void RegisterHealthActuator(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterHealthActuator(config, new DefaultHealthAggregator(), new Type[] { typeof(DiskSpaceContributor) });
        }

        /// <summary>
        /// Register the Health endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="contributors">Types that implement <see cref="IHealthContributor"/></param>
        public static void RegisterHealthActuator(this ContainerBuilder container, IConfiguration config, params Type[] contributors)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterHealthActuator(config, new DefaultHealthAggregator(), contributors);
        }

        /// <summary>
        /// Register the Health endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="aggregator">Your <see cref="IHealthAggregator"/></param>
        /// <param name="contributors">Types that implement <see cref="IHealthContributor"/></param>
        public static void RegisterHealthActuator(this ContainerBuilder container, IConfiguration config, IHealthAggregator aggregator, params Type[] contributors)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (aggregator == null)
            {
                aggregator = new DefaultHealthAggregator();
            }

            container.RegisterInstance(new HealthOptions(config)).As<IHealthOptions>().SingleInstance();
            container.RegisterInstance(aggregator).As<IHealthAggregator>().SingleInstance();
            foreach (var c in contributors)
            {
                container.RegisterType(c).As<IHealthContributor>();
            }

            container.RegisterType<HealthEndpoint>();
            container.RegisterType<HealthEndpointOwinMiddleware>();
        }

        /// <summary>
        /// Register the Health endpoint, IHttpModule and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        //public static void RegisterHealthModule(this ContainerBuilder container, IConfiguration config)
        //{
        //    if (container == null)
        //    {
        //        throw new ArgumentNullException(nameof(container));
        //    }

        //    if (config == null)
        //    {
        //        throw new ArgumentNullException(nameof(config));
        //    }

        //    container.RegisterHealthModule(config, new DefaultHealthAggregator(), new Type[] { typeof(DiskSpaceContributor) });
        //}

        /// <summary>
        /// Register the Health endpoint, IHttpModule and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="contributors">Types that implement <see cref="IHealthContributor"/></param>
        //public static void RegisterHealthModule(this ContainerBuilder container, IConfiguration config, params Type[] contributors)
        //{
        //    if (container == null)
        //    {
        //        throw new ArgumentNullException(nameof(container));
        //    }

        //    if (config == null)
        //    {
        //        throw new ArgumentNullException(nameof(config));
        //    }

        //    container.RegisterHealthModule(config, new DefaultHealthAggregator(), contributors);
        //}

        /// <summary>
        /// Register the Health endpoint, IHttpModule and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="aggregator">Your <see cref="IHealthAggregator"/></param>
        /// <param name="contributors">Types that implement <see cref="IHealthContributor"/></param>
        //public static void RegisterHealthModule(this ContainerBuilder container, IConfiguration config, IHealthAggregator aggregator, params Type[] contributors)
        //{
        //    if (container == null)
        //    {
        //        throw new ArgumentNullException(nameof(container));
        //    }

        //    if (config == null)
        //    {
        //        throw new ArgumentNullException(nameof(config));
        //    }

        //    if (aggregator == null)
        //    {
        //        aggregator = new DefaultHealthAggregator();
        //    }

        //    container.RegisterInstance(new HealthOptions(config)).As<IHealthOptions>();
        //    container.RegisterInstance(aggregator).As<IHealthAggregator>();
        //    foreach (var c in contributors)
        //    {
        //        container.RegisterType(c).As<IHealthContributor>();
        //    }

        //    container.RegisterType<HealthEndpoint>().As<IEndpoint<HealthCheckResult>>();
        //    container.RegisterType<HealthModule>().As<IHttpModule>();
        //}
    }
}
