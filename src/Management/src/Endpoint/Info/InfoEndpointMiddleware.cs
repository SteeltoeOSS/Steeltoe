// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Info;

public class InfoEndpointMiddleware : EndpointMiddleware<Dictionary<string, object>>, IMiddleware
{
    public InfoEndpointMiddleware(InfoEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<InfoEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
    }
    

    public  Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        logger.LogDebug("Info middleware InvokeAsync({path})", context.Request.Path.Value);

        if (Endpoint.Options.ShouldInvoke(managementOptions, context, logger))
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
