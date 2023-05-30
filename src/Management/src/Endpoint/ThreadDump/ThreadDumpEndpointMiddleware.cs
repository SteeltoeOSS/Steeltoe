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
    
    public ThreadDumpEndpointMiddleware(IThreadDumpEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger<ThreadDumpEndpointMiddleware> logger)
        : base(endpointHandler, managementOptions, logger)
    {
    }

    protected override async Task<IList<ThreadInfo>> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        Logger.LogDebug("Executing ThreadDumpHandler");

        return await EndpointHandler.InvokeAsync(null, cancellationToken);
        
    }

}
