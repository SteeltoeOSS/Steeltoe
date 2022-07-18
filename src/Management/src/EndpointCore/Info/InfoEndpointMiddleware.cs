// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Info;

public class InfoEndpointMiddleware : EndpointMiddleware<Dictionary<string, object>>
{
    private readonly RequestDelegate _next;

    public InfoEndpointMiddleware(RequestDelegate next, InfoEndpoint endpoint, IManagementOptions managementOptions, ILogger<InfoEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger: logger)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        logger.LogDebug("Info middleware Invoke({0})", context.Request.Path.Value);

        if (endpoint.ShouldInvoke(managementOptions, logger))
        {
            return HandleInfoRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal Task HandleInfoRequestAsync(HttpContext context)
    {
        var serialInfo = HandleRequest();
        logger?.LogDebug("Returning: {0}", serialInfo);

        context.HandleContentNegotiation(logger);
        return context.Response.WriteAsync(serialInfo);
    }
}
