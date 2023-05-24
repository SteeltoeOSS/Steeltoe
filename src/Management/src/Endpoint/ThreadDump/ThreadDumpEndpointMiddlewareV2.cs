// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.ThreadDump;

internal sealed class ThreadDumpEndpointMiddlewareV2 : EndpointMiddleware<object, ThreadDumpResult>
{
    public ThreadDumpEndpointMiddlewareV2(IThreadDumpEndpointV2Handler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<ThreadDumpEndpointMiddlewareV2> logger)
        : base(endpointHandler, managementOptions, logger)
    {
    }

    //public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    //{
    //    if (EndpointOptions.CurrentValue.ShouldInvoke(ManagementOptions, context, Logger))
    //    {
    //        return HandleThreadDumpRequestAsync(context);
    //    }

    //    return Task.CompletedTask;
    //}

    protected override async Task<ThreadDumpResult> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Executing ThreadDumpV2 handler");
        return await EndpointHandler.InvokeAsync(null, cancellationToken);
    }

    //internal async Task HandleThreadDumpRequestAsync(HttpContext context)
    //{
    //    string serialInfo = await HandleRequestAsync(context.RequestAborted);

    //    Logger.LogDebug("Returning: {info}", serialInfo);
    //    context.Response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
    //    await context.Response.WriteAsync(serialInfo);
    //}
}
