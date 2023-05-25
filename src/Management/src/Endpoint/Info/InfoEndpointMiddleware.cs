// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Info;

internal sealed class InfoEndpointMiddleware : EndpointMiddleware<object, Dictionary<string, object>>
{
    public InfoEndpointMiddleware(IInfoEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger<InfoEndpointMiddleware> logger)
        : base(endpointHandler, managementOptions, logger)
    {
    }

    protected override async Task<Dictionary<string, object>> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(null, cancellationToken);
    }

}
