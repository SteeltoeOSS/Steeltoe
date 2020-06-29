// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Endpoint.Diagnostics;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Trace
{
    public static class EndpointServiceCollectionExtensions
    {
        /// <summary>
        /// Adds components of the Trace actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add trace to</param>
        /// <param name="config">Application configuration (this actuator looks for settings starting with management:endpoints:trace)</param>
        public static void AddTraceActuator(this IServiceCollection services, IConfiguration config)
        {
            services.AddTraceActuator(config, MediaTypeVersion.V2);
        }

        public static void AddTraceActuator(this IServiceCollection services, IConfiguration config, MediaTypeVersion version)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DiagnosticServices>());
            services.AddActuatorManagementOptions(config);
            switch (version)
            {
                case MediaTypeVersion.V1:
                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, TraceDiagnosticObserver>());
                    services.TryAddSingleton<ITraceRepository>((p) => p.GetServices<IDiagnosticObserver>().OfType<TraceDiagnosticObserver>().Single());
                    var options = new TraceEndpointOptions(config);
                    services.TryAddSingleton<ITraceOptions>(options);
                    services.RegisterEndpointOptions(options);
                    services.TryAddSingleton<TraceEndpoint>();
                    break;
                default:
                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpTraceDiagnosticObserver>());
                    var options2 = new HttpTraceEndpointOptions(config);
                    services.TryAddSingleton<ITraceOptions>(options2);
                    services.RegisterEndpointOptions(options2);
                    services.TryAddSingleton(p => new HttpTraceEndpoint(options2, p.GetServices<IDiagnosticObserver>().OfType<HttpTraceDiagnosticObserver>().Single()));
                    break;
            }
        }
    }
}
