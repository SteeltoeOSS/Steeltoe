// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Trace.Exporter.Zipkin;
using Steeltoe.Management.Tracing.Observer;
using System;

namespace Steeltoe.Management.Tracing
{
    public static class TracingServiceCollectionExtensions
    {
        public static void AddDistributedTracing(this IServiceCollection services, IConfiguration config, Action<TracerBuilder> configureTracer = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var appInstanceInfo = services.GetApplicationInstanceInfo();

            services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, TracingService>());

            services.TryAddSingleton<ITracingOptions>((p) =>
            {
                return new TracingOptions(appInstanceInfo, config);
            });

            services.TryAddSingleton<ITraceExporterOptions>((p) =>
            {
                return new TraceExporterOptions(appInstanceInfo, config);
            });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreHostingObserver>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreMvcActionObserver>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreMvcViewObserver>());

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientDesktopObserver>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientCoreObserver>());

            services.TryAddSingleton<ITracing>((p) => { return new OpenTelemetryTracing(p.GetService<ITracingOptions>(), configureTracer); });
            services.TryAddSingleton<IDynamicMessageProcessor, TracingLogProcessor>();
        }

        public static void UseZipkinWithTraceOptions(this TracerBuilder builder, IServiceCollection services)
        {
            var options = services.BuildServiceProvider().GetService<ITraceExporterOptions>();
            builder.UseZipkin(zipkinOptions =>
            {
                zipkinOptions.Endpoint = new Uri(options.Endpoint);
                zipkinOptions.ServiceName = options.ServiceName;
                zipkinOptions.TimeoutSeconds = new TimeSpan(0, 0, options.TimeoutSeconds);
                zipkinOptions.UseShortTraceIds = options.UseShortTraceIds;
            });
        }
    }
}
