// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using System;
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

internal sealed class TestMiddleware1 : EndpointMiddleware<string>
{
    public TestMiddleware1(IEndpoint<string> endpoint, IManagementOptions mgmtOptions, ILogger logger)
        : base(endpoint, mgmtOptions, logger: logger)
    {
    }
}

internal sealed class TestMiddleware2 : EndpointMiddleware<string, string>
{
    public TestMiddleware2(IEndpoint<string, string> endpoint, IManagementOptions mgmtOptions, ILogger logger)
        : base(endpoint, mgmtOptions, logger: logger)
    {
    }
}
