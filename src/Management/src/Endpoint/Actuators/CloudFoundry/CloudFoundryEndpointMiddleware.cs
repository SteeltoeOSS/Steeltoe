// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

/// <summary>
/// Used in conjunction with <see cref="CloudFoundryEndpointHandler" /> to generate hypermedia list of links to all endpoints activated in the
/// application.
/// </summary>
internal sealed class CloudFoundryEndpointMiddleware(
    ICloudFoundryEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<string, Links>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    protected override Task<string?> ParseRequestAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        string scheme = httpContext.Request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme)
            ? headerScheme.ToString()
            : httpContext.Request.Scheme;

        string uri = $"{scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}{httpContext.Request.Path}";
        return Task.FromResult<string?>(uri);
    }

    protected override async Task<Links> InvokeEndpointHandlerAsync(string? uri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return await EndpointHandler.InvokeAsync(uri, cancellationToken);
    }
}
