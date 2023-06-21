// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Trace;

internal sealed class HttpTraceEndpointMiddleware : EndpointMiddleware<object, HttpTraceResult>
{
    public HttpTraceEndpointMiddleware(IHttpTraceEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILoggerFactory loggerFactory) : base(endpointHandler, managementOptions, loggerFactory)
    {
    }

    protected override async Task<HttpTraceResult> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(null, cancellationToken);
    }
}
