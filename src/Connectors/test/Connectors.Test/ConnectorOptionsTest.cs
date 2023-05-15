// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connectors.Test;

public class ConnectorOptionsTest
{
    [Fact]
    public void Value_Returns_Expected()
    {
        var myOpt = new MyOption();
        var opt = new ConnectorOptions<MyOption>(myOpt);

        Assert.NotNull(opt.Value);
        Assert.True(opt.Value == myOpt);
    }
}
