// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Metrics.Observer;
using Steeltoe.Management.EndpointOwin.Metrics;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwinAutofac.Actuators
{
    public static class MetricsContainerBuilderExtensions
    {
        /// <summary>
        /// Register the Metrics endpoint, OWIN middleware and options
        /// </summary>
        /// <param name="container">Autofac DI <see cref="ContainerBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        public static void RegisterMetricsActuator(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            container.RegisterType<DiagnosticsManager>().As<IDiagnosticsManager>().IfNotRegistered(typeof(IDiagnosticsManager)).SingleInstance();
            container.RegisterType<CLRRuntimeSource>().As<IPolledDiagnosticSource>().SingleInstance();

            container.Register(c =>
            {
                var options = new MetricsEndpointOptions(config);
                var mgmtOptions = c.Resolve<IEnumerable<IManagementOptions>>();
                foreach (var mgmt in mgmtOptions)
                {
                    mgmt.EndpointOptions.Add(options);
                }

                return options;
            }).As<IMetricsOptions>().IfNotRegistered(typeof(IMetricsOptions)).SingleInstance();

            container.RegisterType<OwinHostingObserver>().As<IDiagnosticObserver>().SingleInstance();
            container.RegisterType<CLRRuntimeObserver>().As<IDiagnosticObserver>().SingleInstance();

            container.RegisterType<OpenCensusStats>().As<IStats>().IfNotRegistered(typeof(IStats)).SingleInstance();
            container.RegisterType<OpenCensusTags>().As<ITags>().IfNotRegistered(typeof(ITags)).SingleInstance();

            container.RegisterType<MetricsEndpoint>().SingleInstance();
            container.RegisterType<MetricsEndpointOwinMiddleware>().SingleInstance();
        }
    }
}
