// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Test.Middleware;

internal sealed class TestMiddleware1 : EndpointMiddleware<object, string>
{
    public TestMiddleware1(IEndpointHandler<object, string> endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILoggerFactory loggerFactory)
        : base(endpoint, managementOptions, loggerFactory)
    {
    }

    protected override Task<string> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
