// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

/// <summary>
/// Middleware for displaying the mapped ASP.NET routes.
/// </summary>
internal sealed class RouteMappingsEndpointMiddleware(
    IRouteMappingsEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<object?, RouteMappingsResponse>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private protected override string ContentType { get; } = "application/vnd.spring-boot.actuator.v2+json";

    protected override async Task<RouteMappingsResponse> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(null, context.RequestAborted);
    }
}
