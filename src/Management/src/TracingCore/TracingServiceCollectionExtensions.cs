// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Tracing.Observer;
using System;

namespace Steeltoe.Management.Tracing
{
    public static class TracingServiceCollectionExtensions
    {
        public static void AddDistributedTracing(this IServiceCollection services, IConfiguration config)
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, TracingService>());

            services.TryAddSingleton<ITracingOptions>((p) =>
            {
#if NETCOREAPP3_0
                var h = p.GetRequiredService<IHostEnvironment>();
#else
                if (!(p.GetService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>() is IHostingEnvironment h))
                {
                    h = p.GetRequiredService<IHostingEnvironment>();
                }
#endif
                return new TracingOptions(h.ApplicationName, config);
            });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreHostingObserver>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreMvcActionObserver>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreMvcViewObserver>());

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientDesktopObserver>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientCoreObserver>());
            services.TryAddSingleton<ITracing, OpenCensusTracing>();
            services.TryAddSingleton<IDynamicMessageProcessor, TracingLogProcessor>();
        }
    }
}
