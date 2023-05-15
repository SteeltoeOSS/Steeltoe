// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Trace;

internal sealed class TraceEndpointMiddleware : EndpointMiddleware<IList<TraceResult>>
{
    public TraceEndpointMiddleware(ITraceEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions, IOptionsMonitor<HttpMiddlewareOptions> endpointOptions, ILogger<TraceEndpointMiddleware> logger) : base(endpoint, managementOptions, endpointOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (EndpointOptions.CurrentValue.ShouldInvoke(ManagementOptions, context, Logger))
        {
            return HandleTraceRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    internal async Task HandleTraceRequestAsync(HttpContext context)
    {
        ArgumentGuard.NotNull(context);

        string serialInfo = await HandleRequestAsync(context.RequestAborted);

        Logger.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(Logger);
        await context.Response.WriteAsync(serialInfo);
    }
}
