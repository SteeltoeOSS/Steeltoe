// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Env;

internal sealed class EnvironmentEndpointMiddleware : EndpointMiddleware<object, EnvironmentDescriptor>
{
    public EnvironmentEndpointMiddleware(IEnvironmentEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger<EnvironmentEndpointMiddleware> logger)
        : base(endpointHandler, managementOptions,  logger)
    {
    }

    protected override async Task<EnvironmentDescriptor> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(null, context.RequestAborted); 
    }

}
