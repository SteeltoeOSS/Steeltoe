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
    private readonly ILogger _logger = loggerFactory.CreateLogger<CloudFoundryEndpointMiddleware>();

    protected override async Task<Links> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug("InvokeAsync({Method}, {Path})", context.Request.Method, context.Request.Path.Value);
        string uri = GetRequestUri(context);
        return await EndpointHandler.InvokeAsync(uri, cancellationToken);
    }

    private string GetRequestUri(HttpContext context)
    {
        HttpRequest request = context.Request;
        string scheme = request.Scheme;

        if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
        {
            scheme = headerScheme.ToString();
        }

        return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
    }
}
