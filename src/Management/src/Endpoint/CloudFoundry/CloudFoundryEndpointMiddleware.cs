// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

/// <summary>
/// CloudFoundry endpoint provides hypermedia: a page is added with links to all the endpoints that are enabled. When deployed to CloudFoundry this
/// endpoint is used for apps manager integration when <see cref="CloudFoundrySecurityMiddleware" /> is added.
/// </summary>
internal sealed class CloudFoundryEndpointMiddleware : EndpointMiddleware<Links, string>
{
    public CloudFoundryEndpointMiddleware(ICloudFoundryEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IOptionsMonitor<HttpMiddlewareOptions> endpointOptions,
        ILogger<CloudFoundryEndpointMiddleware> logger)
        : base(endpoint, managementOptions, endpointOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentGuard.NotNull(context);

        Logger.LogDebug("InvokeAsync({method}, {path})", context.Request.Method, context.Request.Path.Value);

        if (EndpointOptions.CurrentValue.ShouldInvoke(ManagementOptions, context, Logger))
        {
            return HandleCloudFoundryRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    internal async Task HandleCloudFoundryRequestAsync(HttpContext context)
    {
        string serialInfo = await HandleRequestAsync(GetRequestUri(context.Request), context.RequestAborted);
        Logger.LogDebug("Returning: {info}", serialInfo);
        context.HandleContentNegotiation(Logger);
        await context.Response.WriteAsync(serialInfo);
    }

    private string GetRequestUri(HttpRequest request)
    {
        string scheme = request.Scheme;

        if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
        {
            scheme = headerScheme.ToString();
        }

        return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
    }
}
