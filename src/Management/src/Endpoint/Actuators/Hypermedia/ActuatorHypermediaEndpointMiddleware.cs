// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.Hypermedia;

internal sealed class ActuatorHypermediaEndpointMiddleware : EndpointMiddleware<string, Links>
{
    private readonly ILogger _logger;

    public ActuatorHypermediaEndpointMiddleware(IActuatorEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        ILoggerFactory loggerFactory)
        : base(endpointHandler, managementOptionsMonitor, loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ActuatorHypermediaEndpointMiddleware>();
    }

    protected override async Task<Links> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug("InvokeAsync({Method}, {Path})", context.Request.Method, context.Request.Path.Value);
        string requestUri = GetRequestUri(context.Request);
        return await EndpointHandler.InvokeAsync(requestUri, cancellationToken);
    }

    private static string GetRequestUri(HttpRequest request)
    {
        string scheme = request.Scheme;

        if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
        {
            scheme = headerScheme.ToString();
        }

        // request.Host automatically includes or excludes the port based on whether it is standard for the scheme
        // ... except when we manually change the scheme to match the X-Forwarded-Proto
        if (scheme == "https" && request.Host.Port == 443)
        {
            return $"{scheme}://{request.Host.Host}{request.PathBase}{request.Path}";
        }

        return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
    }
}
