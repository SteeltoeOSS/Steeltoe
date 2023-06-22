// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Mappings;

/// <summary>
/// Middleware for displaying <see cref="IRouteMappings"/> information. 
/// </summary>
internal sealed class MappingsEndpointMiddleware : EndpointMiddleware<object, ApplicationMappings>
{
    public MappingsEndpointMiddleware(IOptionsMonitor<ManagementEndpointOptions> managementOptions, IOptionsMonitor<HttpMiddlewareOptions> endpointOptions,
        IMappingsEndpointHandler endpointHandler, ILoggerFactory loggerFactory)
        : base(endpointHandler, managementOptions, loggerFactory)
    {
    }

    protected override async Task<ApplicationMappings> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(null, context.RequestAborted);
    }
}
