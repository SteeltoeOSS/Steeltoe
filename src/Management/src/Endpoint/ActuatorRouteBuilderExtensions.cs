// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Internal;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint;

public static class ActuatorRouteBuilderExtensions
{
    public static (Type MiddlewareType, Type OptionsType) LookupMiddleware(Type endpointType)
    {
        //return endpointType switch
        //{
        //    not null when endpointType.IsAssignableFrom(typeof(ActuatorEndpoint)) => (typeof(ActuatorHypermediaEndpointMiddleware),
        //        typeof(IActuatorHypermediaOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(DbMigrationsEndpoint)) => (typeof(DbMigrationsEndpointMiddleware), typeof(IDbMigrationsOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(EnvEndpoint)) => (typeof(EnvEndpointMiddleware), typeof(IEnvOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(HealthEndpointCore)) => (typeof(HealthEndpointMiddleware), typeof(IHealthOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(HeapDumpEndpoint)) => (typeof(HeapDumpEndpointMiddleware), typeof(IHeapDumpOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(InfoEndpoint)) => (typeof(InfoEndpointMiddleware), typeof(IInfoOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(LoggersEndpoint)) => (typeof(LoggersEndpointMiddleware), typeof(ILoggersOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(MappingsEndpoint)) => (typeof(MappingsEndpointMiddleware), typeof(IMappingsOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(MetricsEndpoint)) => (typeof(MetricsEndpointMiddleware), typeof(IMetricsEndpointOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(RefreshEndpoint)) => (typeof(RefreshEndpointMiddleware), typeof(IRefreshOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(ThreadDumpEndpoint)) => (typeof(ThreadDumpEndpointMiddleware), typeof(IThreadDumpOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(ThreadDumpEndpointV2)) => (typeof(ThreadDumpEndpointMiddlewareV2), typeof(IThreadDumpOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(TraceEndpoint)) => (typeof(TraceEndpointMiddleware), typeof(ITraceOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(HttpTraceEndpoint)) => (typeof(HttpTraceEndpointMiddleware), typeof(ITraceOptions)),
        //    not null when endpointType.IsAssignableFrom(typeof(CloudFoundryEndpoint)) => (typeof(CloudFoundryEndpointMiddleware), typeof(ICloudFoundryOptions)),
        //    _ => throw new InvalidOperationException($"Could not find middleware for Type: {endpointType?.Name} ")
        //};
        return (null, null);
    }

    /// <summary>
    /// Generic route builder extension for Actuators.
    /// </summary>
    /// <param name="endpoints">
    /// IEndpointRouteBuilder to Map route.
    /// </param>
    /// <param name="convention">
    /// A convention builder action that applies a convention to the whole collection.
    /// </param>
    /// <typeparam name="TEndpoint">
    /// Middleware for which the route is mapped.
    /// </typeparam>
    /// <exception cref="InvalidOperationException">
    /// When T is not found in service container.
    /// </exception>
    //public static void Map<TEndpoint>(this IEndpointRouteBuilder endpoints, Action<IEndpointConventionBuilder> convention = null)
    //    where TEndpoint : IEndpoint
    //{
    //    //MapActuatorEndpoint(endpoints, typeof(TEndpoint), convention);
    //}

    /// <summary>
    /// Maps all actuators that have been registered in <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="endpoints">
    /// The endpoint builder.
    /// </param>
    /// <param name="convention">
    /// The convention action to apply.
    /// </param>
    public static void MapAllActuators(this IEndpointRouteBuilder endpoints, Action<IEndpointConventionBuilder> convention)
    {
        foreach (EndpointMappingEntry endpointEntry in endpoints.ServiceProvider.GetServices<EndpointMappingEntry>())
        {
            // Some actuators only work on some platforms. i.e. Windows and Linux
            // Some actuators have different implementation depending on the MediaTypeVersion

            // Previously those checks where performed here and when adding things to the IServiceCollection
            // Now all that logic is handled in the IServiceCollection setup; no need to keep code in two different places in sync

            // This function just takes what has been registered, and sets up the endpoints
            // This keeps this method flexible; new actuators that are added later should automatically become available
            endpointEntry.SetupConvention(endpoints, convention);
        }
    }

    // New:
    public static void MapTheActuators(this IEndpointRouteBuilder endpoints, Action<IEndpointConventionBuilder> convention)
    {
        RequestDelegate pipeline = endpoints.CreateApplicationBuilder().UseMiddleware(typeof(ActuatorsMiddleware)).Build();

        IEnumerable<string> allowedVerbs = new List<string>  // Get all common verbs here , but add a filter later
                {
                    "Get"
                };
        var managementOptions = endpoints.ServiceProvider.GetService<IOptionsMonitor<ManagementEndpointOptions>>();

        foreach (var name in EndpointContextNames.All) // TODO: Only map Cf on Cloudfoundry
        {
            var mgmtOption = managementOptions.Get(name);
            IEndpointConventionBuilder conventionBuilder = endpoints.MapMethods(mgmtOption.Path+"/{**_}", allowedVerbs, pipeline);


            convention?.Invoke(conventionBuilder);
        }
        
    }
    
    //internal static void MapActuatorEndpoint(this IEndpointRouteBuilder endpoints, Type typeEndpoint, Action<IEndpointConventionBuilder> convention)
    //{
    //    ArgumentGuard.NotNull(endpoints);

    //    ConnectEndpointOptionsWithManagementOptions(endpoints);

    //    (Type middlewareType, Type optionsType) = LookupMiddleware(typeEndpoint);
    //    var options = endpoints.ServiceProvider.GetService(optionsType) as IEndpointOptions;
    //    IEnumerable<IManagementOptions> managementOptionsCollection = endpoints.ServiceProvider.GetServices<IManagementOptions>();

    //    foreach (IManagementOptions managementOptions in managementOptionsCollection)
    //    {
    //        if ((managementOptions is CloudFoundryManagementOptions && options is IActuatorHypermediaOptions) ||
    //            (managementOptions is ActuatorManagementOptions && options is ICloudFoundryOptions))
    //        {
    //            continue;
    //        }

    //        string fullPath = options.GetContextPath(managementOptions);

    //        RoutePattern pattern = RoutePatternFactory.Parse(fullPath);

    //        // only add middleware if the route hasn't already been mapped
    //        if (!endpoints.DataSources.Any(d => d.Endpoints.Any(ep => ep is RouteEndpoint endpoint && endpoint.RoutePattern.RawText == pattern.RawText)))
    //        {
    //            RequestDelegate pipeline = endpoints.CreateApplicationBuilder().UseMiddleware(middlewareType, managementOptions).Build();

    //            IEnumerable<string> allowedVerbs = options.AllowedVerbs ?? new List<string>
    //            {
    //                "Get"
    //            };

    //            IEndpointConventionBuilder conventionBuilder = endpoints.MapMethods(fullPath, allowedVerbs, pipeline);
    //            convention?.Invoke(conventionBuilder);
    //        }
    //    }
    //}

    //private static void ConnectEndpointOptionsWithManagementOptions(IEndpointRouteBuilder endpoints)
    //{
    //    IServiceProvider serviceProvider = endpoints.ServiceProvider;
    //    IEnumerable<IManagementOptions> managementOptions = serviceProvider.GetServices<IManagementOptions>();

    //    foreach (IEndpointOptions endpointOption in serviceProvider.GetServices<IEndpointOptions>())
    //    {
    //        foreach (IManagementOptions managementOption in managementOptions)
    //        {
    //            // hypermedia endpoint is not exposed when running cloudfoundry
    //            if (managementOption is CloudFoundryManagementOptions && endpointOption is HypermediaEndpointOptions)
    //            {
    //                continue;
    //            }

    //            if (!managementOption.EndpointOptions.Contains(endpointOption))
    //            {
    //                managementOption.EndpointOptions.Add(endpointOption);
    //            }
    //        }
    //    }
    //}
}
