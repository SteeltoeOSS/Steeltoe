// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connectors.Services;
using Xunit;

namespace Steeltoe.Connectors.Test;

public class ConnectionStringManagerTest
{
    [Theory]
    [InlineData("squirrelQL")]
    [InlineData("anyqueue")]
    public void ConnectionTypeLocatorThrowsOnUnknown(string value)
    {
        var manager = new ConnectionStringManager(new ConfigurationBuilder().Build());
        var exception = Assert.Throws<ConnectorException>(() => manager.GetByTypeName(value));
        Assert.Contains(value, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionTypeLocatorFromInfoThrowsOnUnknown()
    {
        var manager = new ConnectionStringManager(new ConfigurationBuilder().Build());
        var exception = Assert.Throws<ConnectorException>(() => manager.GetFromServiceInfo(new Db2ServiceInfo("id", "http://idk")));
        Assert.Contains("Db2ServiceInfo", exception.Message, StringComparison.Ordinal);
    }
}
