// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Trace;

internal sealed class HttpTraceEndpointMiddleware : EndpointMiddleware<HttpTraceResult>
{
    public HttpTraceEndpointMiddleware(IHttpTraceEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<HttpTraceEndpointMiddleware> logger)
        : base(endpoint, managementOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (Endpoint.Options.ShouldInvoke(ManagementOptions, context, Logger))
        {
            return HandleTraceRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    internal async Task HandleTraceRequestAsync(HttpContext context)
    {
        string serialInfo = await HandleRequestAsync(context.RequestAborted);

        Logger.LogDebug("Returning: {info}", serialInfo);

        context.HandleContentNegotiation(Logger);
        await context.Response.WriteAsync(serialInfo);
    }
}
