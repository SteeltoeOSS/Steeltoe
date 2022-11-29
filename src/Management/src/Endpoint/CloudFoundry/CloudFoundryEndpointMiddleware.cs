// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

/// <summary>
/// CloudFoundry endpoint provides hypermedia: a page is added with links to all the endpoints that are enabled. When deployed to CloudFoundry this
/// endpoint is used for apps manager integration when <see cref="CloudFoundrySecurityMiddleware" /> is added.
/// </summary>
public class CloudFoundryEndpointMiddleware : EndpointMiddleware<Links, string>
{
    public CloudFoundryEndpointMiddleware(RequestDelegate next, CloudFoundryEndpoint endpoint, IManagementOptions managementOptions,
        ILogger<CloudFoundryEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
    }

    public Task InvokeAsync(HttpContext context)
    {
        logger?.LogDebug("InvokeAsync({method}, {path})", context.Request.Method, context.Request.Path.Value);

        if (Endpoint.ShouldInvoke(managementOptions, logger))
        {
            return HandleCloudFoundryRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal Task HandleCloudFoundryRequestAsync(HttpContext context)
    {
        string serialInfo = HandleRequest(GetRequestUri(context.Request));
        logger?.LogDebug("Returning: {info}", serialInfo);
        context.HandleContentNegotiation(logger);
        return context.Response.WriteAsync(serialInfo);
    }

    protected internal string GetRequestUri(HttpRequest request)
    {
        string scheme = request.Scheme;

        if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
        {
            scheme = headerScheme.ToString();
        }

        return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
    }
}
