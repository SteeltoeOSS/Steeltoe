// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

public sealed class LoggersRequestTest : BaseTest
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var request = new LoggersRequest("foo", "bar");
        request.Name.Should().Be("foo");
        request.Level.Should().Be("bar");
    }

    [Fact]
    public void Constructor_AllowsReset()
    {
        var request = new LoggersRequest("foo", null);
        request.Name.Should().Be("foo");
        request.Level.Should().BeNull();
    }
}
