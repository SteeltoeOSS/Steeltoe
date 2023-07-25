// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Loggers;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Loggers;

public sealed class LoggersRequestTest : BaseTest
{
    [Fact]
    public void Constructor_ThrowsOnNull_Name()
    {
        Assert.Throws<ArgumentNullException>(() => new LoggersRequest(null, "foobar"));
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var cr = new LoggersRequest("foo", "bar");
        Assert.Equal("foo", cr.Name);
        Assert.Equal("bar", cr.Level);
    }

    [Fact]
    public void Constructor_AllowsReset()
    {
        var cr = new LoggersRequest("foo", null);
        Assert.Equal("foo", cr.Name);
        Assert.Null(cr.Level);
    }
}
