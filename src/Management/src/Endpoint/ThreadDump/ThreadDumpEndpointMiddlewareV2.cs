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
    private readonly ILogger<ThreadDumpEndpointMiddlewareV2> _logger;

    public ThreadDumpEndpointMiddlewareV2(IThreadDumpEndpointV2Handler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILoggerFactory loggerFactory)
        : base(endpointHandler, managementOptions, loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ThreadDumpEndpointMiddlewareV2>();
    }

    protected override async Task<ThreadDumpResult> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing ThreadDumpV2 handler");
        return await EndpointHandler.InvokeAsync(null, cancellationToken);
    }
}
