// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint;

internal abstract class ActuatorMapper
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly ILogger<ActuatorMapper> _logger;
    private readonly IEndpointMiddleware[] _middlewares;

    protected ActuatorMapper(IEnumerable<IEndpointMiddleware> middlewares, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(middlewares);
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        IEndpointMiddleware[] middlewareArray = middlewares.ToArray();
        ArgumentGuard.ElementsNotNull(middlewareArray);

        _middlewares = middlewareArray;
        _managementOptionsMonitor = managementOptionsMonitor;
        _logger = loggerFactory.CreateLogger<ActuatorMapper>();
    }

    protected IEnumerable<(string RoutePattern, IEndpointMiddleware Middleware)> GetEndpointsToMap()
    {
        ManagementOptions managementOptions = _managementOptionsMonitor.CurrentValue;

        foreach (IEndpointMiddleware middleware in _middlewares.Where(middleware => middleware is not CloudFoundryEndpointMiddleware))
        {
            string routePattern = middleware.EndpointOptions.GetPathMatchPattern(managementOptions.Path);
            yield return (routePattern, middleware);
        }

        if (Platform.IsCloudFoundry)
        {
            if (managementOptions is { IsCloudFoundryEnabled: true, HasCloudFoundrySecurity: false })
            {
                _logger.LogWarning(
                    $"Actuators at the {ConfigureManagementOptions.DefaultCloudFoundryPath} endpoint are disabled because the Cloud Foundry security middleware is not active. " +
                    $"Call {nameof(EndpointApplicationBuilderExtensions.UseCloudFoundrySecurity)}() from your custom middleware pipeline to enable them.");
            }

            foreach (IEndpointMiddleware middleware in _middlewares.Where(middleware => middleware is not HypermediaEndpointMiddleware))
            {
                string routePattern = middleware.EndpointOptions.GetPathMatchPattern(ConfigureManagementOptions.DefaultCloudFoundryPath);
                yield return (routePattern, middleware);
            }
        }
    }

    protected void LogErrorForDuplicateRoute(string routePattern, IEndpointMiddleware existingMiddleware, IEndpointMiddleware duplicateMiddleware)
    {
        _logger.LogError("Skipping over duplicate route '{Route}' from {DuplicateMiddlewareType}, which was already added by {ExistingMiddlewareType}",
            routePattern, duplicateMiddleware.GetType(), existingMiddleware.GetType());
    }
}
