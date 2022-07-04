// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpEndpointMiddlewareV2 : EndpointMiddleware<ThreadDumpResult>
{
    private readonly RequestDelegate _next;

    public ThreadDumpEndpointMiddlewareV2(RequestDelegate next, ThreadDumpEndpointV2 endpoint, IManagementOptions mgmtOptions, ILogger<ThreadDumpEndpointMiddlewareV2> logger = null)
        : base(endpoint, mgmtOptions, logger: logger)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        if (_endpoint.ShouldInvoke(_mgmtOptions, _logger))
        {
            return HandleThreadDumpRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    protected internal Task HandleThreadDumpRequestAsync(HttpContext context)
    {
        var serialInfo = HandleRequest();
        _logger?.LogDebug("Returning: {0}", serialInfo);
        context.Response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
        return context.Response.WriteAsync(serialInfo);
    }
}
