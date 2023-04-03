// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpEndpointMiddlewareV2 : EndpointMiddleware<ThreadDumpResult>
{
    public ThreadDumpEndpointMiddlewareV2(IThreadDumpEndpointV2 endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<ThreadDumpEndpointMiddlewareV2> logger = null)
        : base(endpoint, managementOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (Endpoint.Options.ShouldInvoke(managementOptions, context, logger))
        {
            return HandleThreadDumpRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal Task HandleThreadDumpRequestAsync(HttpContext context)
    {
        string serialInfo = HandleRequest();

        logger?.LogDebug("Returning: {info}", serialInfo);
        context.Response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
        return context.Response.WriteAsync(serialInfo);
    }
}
