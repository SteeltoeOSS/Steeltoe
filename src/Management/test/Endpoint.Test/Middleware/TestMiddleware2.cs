// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Test.Middleware;

internal sealed class TestMiddleware2 : EndpointMiddleware<string, string>
{
    public TestMiddleware2(IEndpointHandler<string, string> endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger logger)
        : base(endpointHandler, managementOptions, logger)
    {
    }

    protected override Task<string> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
