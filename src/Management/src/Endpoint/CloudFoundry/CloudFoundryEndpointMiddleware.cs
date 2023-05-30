// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

/// <summary>
/// CloudFoundry endpointHandler provides hypermedia: a page is added with links to all the endpoints that are enabled. When deployed to CloudFoundry
/// this endpointHandler is used for apps manager integration when <see cref="CloudFoundrySecurityMiddleware" /> is added.
/// </summary>
internal sealed class CloudFoundryEndpointMiddleware : EndpointMiddleware<string, Links>
{
    public CloudFoundryEndpointMiddleware(ICloudFoundryEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<CloudFoundryEndpointMiddleware> logger)
        : base(endpointHandler, managementOptions, logger)
    {
    }

    protected override async Task<Links> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        Logger.LogDebug("InvokeAsync({method}, {path})", context.Request.Method, context.Request.Path.Value);
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
