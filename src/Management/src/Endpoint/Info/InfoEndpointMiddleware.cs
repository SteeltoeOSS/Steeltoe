// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Info;

public class InfoEndpointMiddleware : EndpointMiddleware<Dictionary<string, object>>
{
    private readonly RequestDelegate _next;

    public InfoEndpointMiddleware(RequestDelegate next, InfoEndpoint endpoint, IManagementOptions managementOptions,
        ILogger<InfoEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        logger.LogDebug("Info middleware InvokeAsync({path})", context.Request.Path.Value);

        if (Endpoint.ShouldInvoke(managementOptions, logger))
        {
            return HandleInfoRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal Task HandleInfoRequestAsync(HttpContext context)
    {
        string serialInfo = HandleRequest();
        logger?.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(logger);
        return context.Response.WriteAsync(serialInfo);
    }
}
