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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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
    public static class ActuatorRouteBuilderExtensions
    {
        public static (Type middleware, Type options) LookupMiddleware(Type endpointType)
        {
            switch (endpointType)
            {
                case Type _ when endpointType.IsAssignableFrom(typeof(ActuatorEndpoint)):
                    return (typeof(ActuatorHypermediaEndpointMiddleware), typeof(IActuatorHypermediaOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(DbMigrationsEndpoint)):
                    return (typeof(DbMigrationsEndpointMiddleware), typeof(IDbMigrationsOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(EnvEndpoint)):
                    return (typeof(EnvEndpointMiddleware), typeof(IEnvOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(HealthEndpointCore)):
                    return (typeof(HealthEndpointMiddleware), typeof(IHealthOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(HeapDumpEndpoint)):
                    return (typeof(HeapDumpEndpointMiddleware), typeof(IHeapDumpOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(InfoEndpoint)):
                    return (typeof(InfoEndpointMiddleware), typeof(IInfoOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(LoggersEndpoint)):
                    return (typeof(LoggersEndpointMiddleware), typeof(ILoggersOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(MappingsEndpoint)):
                    return (typeof(MappingsEndpointMiddleware), typeof(IMappingsOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(MetricsEndpoint)):
                    return (typeof(MetricsEndpointMiddleware), typeof(IMetricsEndpointOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(PrometheusScraperEndpoint)):
                    return (typeof(PrometheusScraperEndpointMiddleware), typeof(IPrometheusEndpointOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(RefreshEndpoint)):
                    return (typeof(RefreshEndpointMiddleware), typeof(IRefreshOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(ThreadDumpEndpoint)):
                    return (typeof(ThreadDumpEndpointMiddleware), typeof(IThreadDumpOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(ThreadDumpEndpoint_v2)):
                    return (typeof(ThreadDumpEndpointMiddleware_v2), typeof(IThreadDumpOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(TraceEndpoint)):
                    return (typeof(TraceEndpointMiddleware), typeof(ITraceOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(HttpTraceEndpoint)):
                    return (typeof(HttpTraceEndpointMiddleware), typeof(ITraceOptions));
                case Type _ when endpointType.IsAssignableFrom(typeof(CloudFoundryEndpoint)):
                    return (typeof(CloudFoundryEndpointMiddleware), typeof(ICloudFoundryOptions));
            }

            throw new InvalidOperationException($"Could not find middleware for Type: {endpointType.Name} ");
        }

        /// <summary>
        /// Generic routebuilder extension for Actuators.
        /// </summary>
        /// <param name="endpoints">IEndpointRouteBuilder to Map route.</param>
        /// <typeparam name="TEndpoint">Middleware for which the route is mapped.</typeparam>
        /// <exception cref="InvalidOperationException">When T is not found in service container</exception>
        public static void Map<TEndpoint>(this IEndpointRouteBuilder endpoints)
        where TEndpoint : IEndpoint
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var (middleware, optionsType) = LookupMiddleware(typeof(TEndpoint));
            var options = endpoints.ServiceProvider.GetService(optionsType) as IEndpointOptions;
            var mgmtOptionsCollection = endpoints.ServiceProvider.GetServices<IManagementOptions>();

            foreach (var mgmtOptions in mgmtOptionsCollection)
            {
                if ((mgmtOptions is CloudFoundryManagementOptions && options is IActuatorHypermediaOptions)
                    || (mgmtOptions is ActuatorManagementOptions && options is ICloudFoundryOptions))
                {
                    continue;
                }

                var fullPath = options.GetContextPath(mgmtOptions);

                var pipeline = endpoints.CreateApplicationBuilder()
                    .UseMiddleware(middleware, mgmtOptions)
                    .Build();

                if (options.AllowedVerbs == null)
                {
                    endpoints.Map(fullPath, pipeline);
                }
                else
                {
                    endpoints.MapMethods(fullPath, options.AllowedVerbs, pipeline);
                }
            }
        }

        public static void MapAllActuators(this IEndpointRouteBuilder endpoints, MediaTypeVersion version = MediaTypeVersion.V2)
        {
            endpoints.Map<ActuatorEndpoint>();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (version == MediaTypeVersion.V2)
                {
                    endpoints.Map<ThreadDumpEndpoint_v2>();
                }
                else
                {
                    endpoints.Map<ThreadDumpEndpoint>();
                }

                endpoints.Map<HeapDumpEndpoint>();
            }

            endpoints.Map<EnvEndpoint>();
            endpoints.Map<RefreshEndpoint>();
            endpoints.Map<InfoEndpoint>();
            endpoints.Map<HealthEndpoint>();
            endpoints.Map<LoggersEndpoint>();
            if (version == MediaTypeVersion.V2)
            {
                endpoints.Map<HttpTraceEndpoint>();
            }
            else
            {
                endpoints.Map<TraceEndpoint>();
            }

            endpoints.Map<MappingsEndpoint>();
            endpoints.Map<MetricsEndpoint>();
            endpoints.Map<PrometheusScraperEndpoint>();
        }
    }
}
