﻿// Copyright 2017 the original author or authors.
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

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;

namespace Steeltoe.Management.Endpoint
{
    public static class ActuatorRouteBuilderExtensions
    {
        private static readonly Dictionary<Type,Type> MiddlewareLookup = new Dictionary<Type, Type>()
        {
            { typeof(ActuatorEndpoint), typeof(ActuatorHypermediaEndpointHandler) },
            { typeof(DbMigrationsEndpoint), typeof(DbMigrationsEndpointHandler) },
            { typeof(EnvEndpoint), typeof(EnvEndpointHandler) },
            { typeof(HeapDumpEndpoint), typeof(HeapDumpEndpointHandler) },
            { typeof(InfoEndpoint), typeof(InfoEndpointHandler) },
            { typeof(LoggersEndpoint), typeof(LoggersEndpointHandler) },
            { typeof(MappingsEndpoint), typeof(MappingsEndpointHandler) },
            { typeof(MetricsEndpoint), typeof(MetricsEndpointHandler) },
            { typeof(PrometheusScraperEndpoint), typeof(PrometheusScraperEndpointHandler)},
            { typeof(RefreshEndpoint), typeof(RefreshEndpointHandler) },
            { typeof(ThreadDumpEndpoint_v2), typeof(ThreadDumpEndpointHandler) },
            { typeof(HttpTraceEndpoint), typeof(HttpTraceEndpointHandler) },

            //  { typeof(DbMigrationsEndpoint), typeof(DbMigrationsEndpointHandler) },
           // endpoints.Map<LoggersEndpoint>();
            // endpoints.Map<MappingsEndpoint>();
        };
        /// <summary>
        /// Generic routebuilder extension for Actuators.
        /// </summary>
        /// <param name="endpoints">IEndpointRouteBuilder to Map route.</param>
        /// <typeparam name="TEndpoint">IEndpoint for which the route is mapped.</typeparam>
        /// <typeparam name="TMiddleware">Type of Middleware</typeparam>
        /// <exception cref="InvalidOperationException">When T is not found in service container</exception>
        public static void Map<TEndpoint>(this IEndpointRouteBuilder endpoints)
        where TEndpoint : IEndpoint
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var actuator = endpoints.ServiceProvider.GetService<TEndpoint>()
                ?? throw new InvalidOperationException($"Could not find type {typeof(TEndpoint)} in service container");

            var options =
                endpoints.ServiceProvider
                    .GetService<IEnumerable<IManagementOptions>>();


            foreach (var mgmtOptions in options)
            {
                var fullPath = actuator.GetContextPath(mgmtOptions);
                var middle = MiddlewareLookup[actuator.GetType()];
                var pipeline = endpoints.CreateApplicationBuilder()
                    .UseMiddleware(middle)
                    .Build();

                if (actuator.AllowedVerbs == null)
                {
                    endpoints.Map(fullPath, pipeline);
                }
                else
                {
                    endpoints.MapMethods(fullPath, actuator.AllowedVerbs, pipeline);
                }
            }
        }
    }
}
