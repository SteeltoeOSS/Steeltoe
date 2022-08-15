// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Test;

public class ConnectorExceptionTest
{
    [Fact]
    public void Constructor_CapturesMessage()
    {
        var ex = new ConnectorException("Test");
        Assert.Equal("Test", ex.Message);

        var ex2 = new ConnectorException("Test2", new Exception());
        Assert.Equal("Test2", ex2.Message);
    }

    [Fact]
    public void Constructor_CapturesNestedException()
    {
        var inner = new Exception();
        var ex = new ConnectorException("Test2", inner);
        Assert.Equal(inner, ex.InnerException);
    }
}
