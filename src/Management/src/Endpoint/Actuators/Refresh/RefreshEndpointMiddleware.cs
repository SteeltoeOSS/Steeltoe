// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.Refresh;

internal sealed class RefreshEndpointMiddleware(
    IRefreshEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<object?, IList<string>>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    public override ActuatorMetadataProvider GetMetadataProvider()
    {
        return new RefreshActuatorMetadataProvider(ContentType);
    }

    protected override async Task<IList<string>> InvokeEndpointHandlerAsync(object? request, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(request, cancellationToken);
    }
}
