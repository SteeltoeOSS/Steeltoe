// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Trace;

public class TraceEndpointMiddleware : EndpointMiddleware<List<TraceResult>>
{
    public TraceEndpointMiddleware(RequestDelegate next, TraceEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<TraceEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (((TraceEndpoint)Endpoint).Options.CurrentValue.EndpointOptions.ShouldInvoke(managementOptions.CurrentValue))
        {
            return HandleTraceRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal Task HandleTraceRequestAsync(HttpContext context)
    {
        string serialInfo = HandleRequest();
        logger?.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(logger);
        return context.Response.WriteAsync(serialInfo);
    }
}
