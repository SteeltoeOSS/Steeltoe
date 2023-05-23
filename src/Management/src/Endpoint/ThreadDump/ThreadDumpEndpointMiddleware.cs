// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.ThreadDump;

internal sealed class ThreadDumpEndpointMiddleware : EndpointMiddleware<object, IList<ThreadInfo>>
{
    
    public ThreadDumpEndpointMiddleware(IThreadDumpEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions, IOptionsMonitor<HttpMiddlewareOptions> endpointOptions, ILogger<ThreadDumpEndpointMiddleware> logger) : base(endpointHandler, managementOptions, endpointOptions, logger)
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

    public override bool ShouldInvoke(HttpContext context)
    {
        throw new NotImplementedException();
    }

    protected override async Task<IList<ThreadInfo>> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Executing ThreadDumpHandler");

        return await EndpointHandler.InvokeAsync(null, cancellationToken);
        
    }

    //internal async Task HandleThreadDumpRequestAsync(HttpContext context)
    //{
    //    string serialInfo = await HandleRequestAsync(context.RequestAborted);
       
    //    context.HandleContentNegotiation(Logger);
    //    await context.Response.WriteAsync(serialInfo);
    //}
}
