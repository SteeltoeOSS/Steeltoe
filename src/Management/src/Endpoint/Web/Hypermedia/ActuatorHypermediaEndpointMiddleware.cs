// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Web.Hypermedia;

internal sealed class ActuatorHypermediaEndpointMiddleware : EndpointMiddleware<string, Links>
{
    public ActuatorHypermediaEndpointMiddleware(IActuatorEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<ActuatorHypermediaEndpointMiddleware> logger)
        : base(endpointHandler, managementOptions, logger)
    {
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


    protected override async Task<Links> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(context);
        Logger.LogDebug("InvokeAsync({method}, {path})", context.Request.Method, context.Request.Path.Value);
        var requestUri = GetRequestUri(context.Request);
        return await EndpointHandler.InvokeAsync(requestUri, cancellationToken);
    }
}
