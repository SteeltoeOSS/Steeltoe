// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
using Steeltoe.Management.Endpoint.Internal;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Steeltoe.Management.Endpoint
{
    public static class ActuatorRouteBuilderExtensions
    {
        public static (Type middleware, Type options) LookupMiddleware(Type endpointType)
        {
            return endpointType switch
            {
                Type _ when endpointType.IsAssignableFrom(typeof(ActuatorEndpoint)) => (typeof(ActuatorHypermediaEndpointMiddleware), typeof(IActuatorHypermediaOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(DbMigrationsEndpoint)) => (typeof(DbMigrationsEndpointMiddleware), typeof(IDbMigrationsOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(EnvEndpoint)) => (typeof(EnvEndpointMiddleware), typeof(IEnvOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(HealthEndpointCore)) => (typeof(HealthEndpointMiddleware), typeof(IHealthOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(HeapDumpEndpoint)) => (typeof(HeapDumpEndpointMiddleware), typeof(IHeapDumpOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(InfoEndpoint)) => (typeof(InfoEndpointMiddleware), typeof(IInfoOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(LoggersEndpoint)) => (typeof(LoggersEndpointMiddleware), typeof(ILoggersOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(MappingsEndpoint)) => (typeof(MappingsEndpointMiddleware), typeof(IMappingsOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(MetricsEndpoint)) => (typeof(MetricsEndpointMiddleware), typeof(IMetricsEndpointOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(PrometheusScraperEndpoint)) => (typeof(PrometheusScraperEndpointMiddleware), typeof(IPrometheusEndpointOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(RefreshEndpoint)) => (typeof(RefreshEndpointMiddleware), typeof(IRefreshOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(ThreadDumpEndpoint)) => (typeof(ThreadDumpEndpointMiddleware), typeof(IThreadDumpOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(ThreadDumpEndpoint_v2)) => (typeof(ThreadDumpEndpointMiddleware_v2), typeof(IThreadDumpOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(TraceEndpoint)) => (typeof(TraceEndpointMiddleware), typeof(ITraceOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(HttpTraceEndpoint)) => (typeof(HttpTraceEndpointMiddleware), typeof(ITraceOptions)),
                Type _ when endpointType.IsAssignableFrom(typeof(CloudFoundryEndpoint)) => (typeof(CloudFoundryEndpointMiddleware), typeof(ICloudFoundryOptions)),
                _ => throw new InvalidOperationException($"Could not find middleware for Type: {endpointType.Name} "),
            };
        }

        /// <summary>
        /// Generic routebuilder extension for Actuators.
        /// </summary>
        /// <param name="endpoints">IEndpointRouteBuilder to Map route.</param>
        /// <param name="conventionBuilder">A convention builder that applies a convention to the whole collection. </param>
        /// <typeparam name="TEndpoint">Middleware for which the route is mapped.</typeparam>
        /// <exception cref="InvalidOperationException">When T is not found in service container</exception>
        /// <remarks>
        /// Internally this method now calls MapAllActuators().
        /// During a refactor, things changed. Now endpoints are mapped depending what has been registered in <see cref="IServiceCollection"/>
        /// </remarks>
#pragma warning disable S2326 // Unused type parameters should be removed
        public static IEndpointConventionBuilder Map<TEndpoint>(this IEndpointRouteBuilder endpoints, EndpointCollectionConventionBuilder conventionBuilder = null)
#pragma warning restore S2326 // Unused type parameters should be removed
        where TEndpoint : IEndpoint
        {
            // this function is retained to keep binary compatibilty
            // since MapAllActuators() now maps whatever has been registered in IServiceCollection we can only rely on that.
            // Since this method may be used by a lot of clients, we don't mark it deprecated yet since that will generate a lot of warnings.
            return endpoints.MapAllActuators();
        }

        /// <summary>
        /// Maps all actutators that have been registered in <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="endpoints">The endpoint builder</param>
        /// <param name="version">Media Version</param>
        /// <returns>Endpoint convention builder</returns>
        [Obsolete("Use overload that doesn't take MediaTypeVersion")]
        [ExcludeFromCodeCoverage] // retained for binary compatibility
        public static IEndpointConventionBuilder MapAllActuators(this IEndpointRouteBuilder endpoints, MediaTypeVersion version) => endpoints.MapAllActuators();

        /// <summary>
        /// Maps all actutators that have been registered in <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="endpoints">The endpoint builder</param>
        /// <returns>Endpoint convention builder</returns>
        public static IEndpointConventionBuilder MapAllActuators(this IEndpointRouteBuilder endpoints)
        {
            var endpointTracking = endpoints.ServiceProvider.GetService<EndpointTracking>();
            var conventionBuilder = new EndpointCollectionConventionBuilder();

            foreach (var endpointEntry in endpoints.ServiceProvider.GetServices<EndpointEntry>())
            {
                // depending on how the client is using this library, there may be duplicate EndpointEntry instances registered
                // we use the name to identify duplicates so that we avoid duplicate endpoints
                if (!endpointTracking.Mapped.Contains(endpointEntry.Name))
                {
                    endpointEntry.Setup(endpoints, conventionBuilder);
                    endpointTracking.Mapped.Add(endpointEntry.Name);
                }
            }

            return conventionBuilder;
        }

        internal static IEndpointConventionBuilder MapInternal<TEndpoint>(this IEndpointRouteBuilder endpoints, EndpointCollectionConventionBuilder conventionBuilder = null)
            where TEndpoint : IEndpoint
        {
            return MapActuatorEndpoint(endpoints, typeof(TEndpoint), conventionBuilder);
        }

        internal static IEndpointConventionBuilder MapActuatorEndpoint(this IEndpointRouteBuilder endpoints, Type typeEndpoint, EndpointCollectionConventionBuilder conventionBuilder = null)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var (middleware, optionsType) = LookupMiddleware(typeEndpoint);
            var options = endpoints.ServiceProvider.GetService(optionsType) as IEndpointOptions;
            var mgmtOptionsCollection = endpoints.ServiceProvider.GetServices<IManagementOptions>();
            var builder = conventionBuilder ?? new EndpointCollectionConventionBuilder();

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
                var allowedVerbs = options.AllowedVerbs ?? new List<string> { "Get" };

                builder.AddConventionBuilder(endpoints.MapMethods(fullPath, allowedVerbs, pipeline));
            }

            return builder;
        }
    }

    /// <summary>
    /// Represents a collection of ConventionBuilders which need the same convention applied to all of them.
    /// </summary>
    public class EndpointCollectionConventionBuilder : IEndpointConventionBuilder
    {
        private List<IEndpointConventionBuilder> _conventionBuilders = new List<IEndpointConventionBuilder>();

        public void AddConventionBuilder(IEndpointConventionBuilder builder)
        {
            _conventionBuilders.Add(builder);
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            foreach (var conventionBuilder in _conventionBuilders)
            {
                conventionBuilder.Add(convention);
            }
        }
    }
}
