// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using System;

namespace Steeltoe.Management.Endpoint
{
    public static class ActuatorServiceCollectionExtensions
    {
        public static void RegisterEndpointOptions(this IServiceCollection services, IEndpointOptions options)
        {
            var mgmtOptions = services.BuildServiceProvider().GetServices<IManagementOptions>();
            foreach (var mgmtOption in mgmtOptions)
            {
                mgmtOption.EndpointOptions.Add(options);
            }
        }

        public static void AddAllActuators(this IServiceCollection services, IConfiguration config, MediaTypeVersion version)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddHypermediaActuator(config);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                services.AddThreadDumpActuator(config, version);
                services.AddHeapDumpActuator(config);
            }

            services.AddDbMigrationsActuator(config);
            services.AddEnvActuator(config);
            services.AddInfoActuator(config);
            services.AddHealthActuator(config);
            services.AddLoggersActuator(config);
            services.AddTraceActuator(config, version);
            services.AddMappingsActuator(config);
            services.AddMetricsActuator(config);
            services.AddPrometheusActuator(config);
            services.AddRefreshActuator(config);
        }
    }
}
