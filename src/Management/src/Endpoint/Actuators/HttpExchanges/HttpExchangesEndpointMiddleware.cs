// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

internal sealed class HttpExchangesEndpointMiddleware(
    IHttpExchangesEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<object?, HttpExchangesResult>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    protected override async Task<HttpExchangesResult> InvokeEndpointHandlerAsync(object? request, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(request, cancellationToken);
    }
}
