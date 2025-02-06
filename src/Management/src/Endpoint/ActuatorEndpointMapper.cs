// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint;

internal sealed class ActuatorEndpointMapper : ActuatorMapper
{
    private readonly IOptionsMonitor<ActuatorConventionOptions> _conventionOptionsMonitor;

    public ActuatorEndpointMapper(IEnumerable<IEndpointMiddleware> middlewares, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<ActuatorConventionOptions> conventionOptionsMonitor, ILoggerFactory loggerFactory)
        : base(middlewares, managementOptionsMonitor, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(conventionOptionsMonitor);

        _conventionOptionsMonitor = conventionOptionsMonitor;
    }

    public void Map(IEndpointRouteBuilder endpointRouteBuilder, ActuatorConventionBuilder actuatorConventionBuilder)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);
        ArgumentNullException.ThrowIfNull(actuatorConventionBuilder);

        var routesMapped = new Dictionary<string, IEndpointMiddleware>(StringComparer.OrdinalIgnoreCase);

        foreach ((string routePattern, IEndpointMiddleware middleware) in GetEndpointsToMap())
        {
            if (!routesMapped.TryAdd(routePattern, middleware))
            {
                LogErrorForDuplicateRoute(routePattern, routesMapped[routePattern], middleware);
                continue;
            }

            Delegate pipeline = CreatePipeline(endpointRouteBuilder, middleware);
            RouteHandlerBuilder routeHandlerBuilder = endpointRouteBuilder.Map(routePattern, pipeline);

            // Actuator endpoint exposure in OpenAPI is likely undesired in apps. They will show up in the mappings actuator.
            ActuatorMetadataProvider metadataProvider = middleware.GetMetadataProvider();
            routeHandlerBuilder.WithMetadata(metadataProvider);
            routeHandlerBuilder.ExcludeFromDescription();

            ConfigureConventions(routeHandlerBuilder, actuatorConventionBuilder);
        }
    }

    private static Delegate CreatePipeline(IEndpointRouteBuilder endpointRouteBuilder, IEndpointMiddleware middleware)
    {
        IApplicationBuilder builder = endpointRouteBuilder.CreateApplicationBuilder();
        builder.UseMiddleware(middleware.GetType());
        return builder.Build();
    }

    private void ConfigureConventions(IEndpointConventionBuilder endpointConventionBuilder, ActuatorConventionBuilder actuatorConventionBuilder)
    {
        foreach (Action<IEndpointConventionBuilder> configureAction in _conventionOptionsMonitor.CurrentValue.ConfigureActions)
        {
            configureAction(endpointConventionBuilder);
        }

        actuatorConventionBuilder.TrackTarget(endpointConventionBuilder);
    }
}
