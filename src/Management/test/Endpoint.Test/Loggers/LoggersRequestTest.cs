// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Loggers;

public sealed class LoggersRequestTest : BaseTest
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var request = new LoggersRequest("foo", "bar");
        Assert.Equal("foo", request.Name);
        Assert.Equal("bar", request.Level);
    }

    [Fact]
    public void Constructor_AllowsReset()
    {
        var request = new LoggersRequest("foo", null);
        Assert.Equal("foo", request.Name);
        Assert.Null(request.Level);
    }
}
