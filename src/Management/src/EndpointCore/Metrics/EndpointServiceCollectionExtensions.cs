// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Endpoint.Diagnostics;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Metrics.Observer;
using System;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public static class EndpointServiceCollectionExtensions
    {
        public static void AddMetricsActuator(this IServiceCollection services, IConfiguration config)
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPolledDiagnosticSource, CLRRuntimeSource>());

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(config)));
            var options = new MetricsEndpointOptions(config);
            services.TryAddSingleton<IMetricsOptions>(options);
            services.RegisterEndpointOptions(options);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreHostingObserver>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, CLRRuntimeObserver>());

            services.TryAddSingleton<IStats, OpenCensusStats>();
            services.TryAddSingleton<ITags, OpenCensusTags>();

            services.TryAddSingleton<MetricsEndpoint>();
        }

        public static void AddPrometheusActuator(this IServiceCollection services, IConfiguration config)
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPolledDiagnosticSource, CLRRuntimeSource>());

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(config)));

            var metricsOptions = new MetricsEndpointOptions(config);
            services.TryAddSingleton<IMetricsOptions>(metricsOptions);

            var options = new PrometheusEndpointOptions(config);
            services.TryAddSingleton<IPrometheusOptions>(options);
            services.RegisterEndpointOptions(options);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreHostingObserver>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, CLRRuntimeObserver>());

            services.TryAddSingleton<IStats, OpenCensusStats>();
            services.TryAddSingleton<ITags, OpenCensusTags>();

            services.TryAddSingleton<PrometheusScraperEndpoint>();
        }
    }
}
