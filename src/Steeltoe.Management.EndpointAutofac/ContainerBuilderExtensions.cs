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
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.EndpointAutofac.Actuators;
using System;
using System.Web.Http.Description;

namespace Steeltoe.Management.EndpointAutofac
{
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Add all actuator OWIN Middlewares, configure CORS and Cloud Foundry request security
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="apiExplorer">An <see cref="ApiExplorer" /> that has access to your HttpConfiguration.<para />If not provided, mappings actuator will not be registered</param>
        public static void RegisterCloudFoundryActuators(this ContainerBuilder container, IConfiguration config, IApiExplorer apiExplorer = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterDiagnosticSourceMiddleware();
            container.RegisterCloudFoundrySecurityActuator(config);
            container.RegisterCloudFoundryActuator(config);
            container.RegisterHealthActuator(config);
            container.RegisterHeapDumpActuator(config);
            container.RegisterInfoActuator(config);
            container.RegisterLoggersActuator(config);
            if (apiExplorer != null)
            {
                container.RegisterMappingsActuator(config, apiExplorer);
            }

            container.RegisterMetricsActuator(config);
            container.RegisterThreadDumpActuator(config);
            container.RegisterTraceActuator(config);
        }

        /// <summary>
        /// Obtains the DiagnosticsManager and if it exists, it starts it.  The manager is added to the container
        /// if metrics or trace actuators are used.
        /// </summary>
        /// <param name="container">the autofac container</param>
        public static void StartActuators(this IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (container.TryResolve<IDiagnosticsManager>(out IDiagnosticsManager diagManager))
            {
                diagManager.Start();
            }
        }
    }
}
