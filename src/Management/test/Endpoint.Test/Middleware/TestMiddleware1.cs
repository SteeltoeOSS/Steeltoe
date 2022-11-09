// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Test.Middleware;

internal sealed class TestMiddleware1 : EndpointMiddleware<string>
{
    public TestMiddleware1(IEndpoint<string> endpoint, IManagementOptions managementOptions, ILogger logger)
        : base(endpoint, managementOptions, logger)
    {
    }
}
