// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Middleware.Test;

public class EndpointMiddlewareTest : BaseTest
{
    [Fact]
    public void Constructor_ThrowsIfEndpointNull()
    {
        Assert.Throws<ArgumentNullException>(() => new TestMiddleware1(null, null, null));
        Assert.Throws<ArgumentNullException>(() => new TestMiddleware2(null, null, null));
    }
}