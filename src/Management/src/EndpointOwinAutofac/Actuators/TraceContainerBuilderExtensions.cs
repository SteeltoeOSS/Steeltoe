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
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.EndpointOwin;
using Steeltoe.Management.EndpointOwin.Trace;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
{
    public static class TraceContainerBuilderExtensions
    {
        /// <summary>
        /// Register the Trace endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        public static void RegisterTraceActuator(this ContainerBuilder container, IConfiguration config)
        {
            container.RegisterTraceActuator(config, MediaTypeVersion.V1);
        }

        public static void RegisterTraceActuator(this ContainerBuilder container, IConfiguration config, MediaTypeVersion version)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            switch (version)
            {
                case MediaTypeVersion.V1:
                    container.RegisterTraceActuatorComponents(config);
                    break;
                default:
                    container.RegisterHttpTraceActuatorComponents(config);
                    break;
            }
        }

        private static void RegisterTraceActuatorComponents(this ContainerBuilder container, IConfiguration config)
        {
            container.Register(c =>
            {
                var options = new TraceEndpointOptions(config);
                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>();
                foreach (var mgmt in mgmtOptions)
                {
                    mgmt.EndpointOptions.Add(options);
                }

                return options;
            }).As<ITraceOptions>().IfNotRegistered(typeof(ITraceOptions)).SingleInstance();

            container.RegisterType<TraceDiagnosticObserver>().As<IDiagnosticObserver>().As<ITraceRepository>().SingleInstance();
            container.RegisterType<DiagnosticsManager>().As<IDiagnosticsManager>().IfNotRegistered(typeof(IDiagnosticsManager)).SingleInstance();

            container.RegisterType<TraceEndpoint>().As<IEndpoint<List<TraceResult>>>().SingleInstance();
            container.RegisterType<EndpointOwinMiddleware<List<TraceResult>>>().SingleInstance();
        }

        private static void RegisterHttpTraceActuatorComponents(this ContainerBuilder container, IConfiguration config)
        {
            container.Register(c =>
            {
                var options = new HttpTraceEndpointOptions(config);
                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>();
                foreach (var mgmt in mgmtOptions)
                {
                    mgmt.EndpointOptions.Add(options);
                }

                return options;
            }).As<ITraceOptions>().IfNotRegistered(typeof(ITraceOptions)).SingleInstance();

            container.RegisterType<HttpTraceDiagnosticObserver>().As<IDiagnosticObserver>().As<IHttpTraceRepository>().SingleInstance();
            container.RegisterType<DiagnosticsManager>().As<IDiagnosticsManager>().IfNotRegistered(typeof(IDiagnosticsManager)).SingleInstance();

            container.RegisterType<HttpTraceEndpoint>().As<IEndpoint<HttpTraceResult>>().SingleInstance();
            container.RegisterType<EndpointOwinMiddleware<HttpTraceResult>>().SingleInstance();
        }
    }
}
