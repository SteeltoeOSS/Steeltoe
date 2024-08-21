// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

internal sealed class ThreadDumpEndpointMiddleware(
    IThreadDumpEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<object?, IList<ThreadInfo>>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private readonly ILogger<ThreadDumpEndpointMiddleware> _logger = loggerFactory.CreateLogger<ThreadDumpEndpointMiddleware>();

    protected override async Task<IList<ThreadInfo>> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing ThreadDumpHandler");

        return await EndpointHandler.InvokeAsync(null, cancellationToken);
    }
}
