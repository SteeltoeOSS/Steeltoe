// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Test;

internal sealed class TestEndpointHandler: IEndpointHandler<object, int>
{
    public HttpMiddlewareOptions Options => throw new NotImplementedException();

    public Task<int> InvokeAsync(object arg, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
