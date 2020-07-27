﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.EndpointOwinAutofac.Actuators;
using Steeltoe.Management.Hypermedia;
using System;
using System.Web.Http.Description;

namespace Steeltoe.Management.EndpointOwinAutofac
{
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Add all cloudfoundry actuator OWIN Middlewares, configure CORS and Cloud Foundry request security
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="apiExplorer">An <see cref="ApiExplorer" /> that has access to your HttpConfiguration.<para />If not provided, mappings actuator will not be registered</param>
        public static void RegisterCloudFoundryActuators(this ContainerBuilder container, IConfiguration config, IApiExplorer apiExplorer = null)
        {
            container.RegisterCloudFoundryActuators(config, apiExplorer, MediaTypeVersion.V1, ActuatorContext.CloudFoundry);
        }

        /// <summary>
        /// Add all cloudfoundry actuator OWIN Middlewares, configure CORS and Cloud Foundry request security
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="apiExplorer">An <see cref="ApiExplorer" /> that has access to your HttpConfiguration.<para />If not provided, mappings actuator will not be registered</param>
        /// <param name="version">MediaType version for response</param>
        /// <param name="context">Actuator Context for endpoints</param>
        public static void RegisterCloudFoundryActuators(this ContainerBuilder container, IConfiguration config, IApiExplorer apiExplorer, MediaTypeVersion version,  ActuatorContext context)
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
            if (context != ActuatorContext.Actuator)
            {
                container.RegisterCloudFoundrySecurityMiddleware(config);
                container.RegisterCloudFoundryActuator(config);
            }

            if (context != ActuatorContext.CloudFoundry)
            {
                container.RegisterDiagnosticSourceMiddleware();
                container.RegisterHypermediaActuator(config);
            }

            container.RegisterHealthActuator(config);
            container.RegisterHeapDumpActuator(config);
            container.RegisterInfoActuator(config);
            container.RegisterLoggersActuator(config);
            if (apiExplorer != null)
            {
                container.RegisterMappingsActuator(config, apiExplorer);
            }

            container.RegisterThreadDumpActuator(config, version);
            container.RegisterTraceActuator(config, version);
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

            if (container.TryResolve<IDiagnosticsManager>(out var diagManager))
            {
                diagManager.Start();
            }
        }
    }
}
