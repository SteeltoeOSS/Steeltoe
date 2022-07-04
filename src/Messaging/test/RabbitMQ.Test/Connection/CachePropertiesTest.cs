// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

[Trait("Category", "Integration")]
public class CachePropertiesTest
{
    [Fact]
    public void TestChannelCache()
    {
        var channelCf = new CachingConnectionFactory("localhost")
        {
            ServiceName = "testChannelCache",
            ChannelCacheSize = 4
        };
        var c1 = channelCf.CreateConnection();
        var c2 = channelCf.CreateConnection();
        Assert.Same(c1, c2);
        var ch1 = c1.CreateChannel();
        var ch2 = c1.CreateChannel();
        var ch3 = c1.CreateChannel(true);
        var ch4 = c1.CreateChannel(true);
        var ch5 = c1.CreateChannel(true);
        ch1.Close();
        ch2.Close();
        ch3.Close();
        ch4.Close();
        ch5.Close();
        var props = channelCf.GetCacheProperties();
        Assert.StartsWith("testChannelCache", (string)props["connectionName"]);
        Assert.Equal(4, props["channelCacheSize"]);
        Assert.Equal(2, props["idleChannelsNotTx"]);
        Assert.Equal(3, props["idleChannelsTx"]);
        Assert.Equal(2, props["idleChannelsNotTxHighWater"]);
        Assert.Equal(3, props["idleChannelsTxHighWater"]);
        c1.CreateChannel();
        c1.CreateChannel(true);
        props = channelCf.GetCacheProperties();
        Assert.Equal(1, props["idleChannelsNotTx"]);
        Assert.Equal(2, props["idleChannelsTx"]);
        Assert.Equal(2, props["idleChannelsNotTxHighWater"]);
        Assert.Equal(3, props["idleChannelsTxHighWater"]);
        ch1 = c1.CreateChannel();
        ch2 = c1.CreateChannel();
        ch3 = c1.CreateChannel(true);
        ch4 = c1.CreateChannel(true);
        ch5 = c1.CreateChannel(true);
        var ch6 = c1.CreateChannel(true);
        var ch7 = c1.CreateChannel(true); // #5
        ch1.Close();
        ch2.Close();
        ch3.Close();
        ch4.Close();
        ch5.Close();
        ch6.Close();
        ch7.Close();
        props = channelCf.GetCacheProperties();
        Assert.Equal(2, props["idleChannelsNotTx"]);
        Assert.Equal(4, props["idleChannelsTx"]);
        Assert.Equal(2, props["idleChannelsNotTxHighWater"]);
        Assert.Equal(4, props["idleChannelsTxHighWater"]);
    }

    [Fact]
    public void TestConnectionCache()
    {
        var connectionCf = new CachingConnectionFactory("localhost")
        {
            ChannelCacheSize = 10,
            ConnectionCacheSize = 5,
            CacheMode = CachingMode.Connection,
            ServiceName = "testConnectionCache"
        };
        var c1 = connectionCf.CreateConnection();
        var c2 = connectionCf.CreateConnection();
        var ch1 = c1.CreateChannel();
        var ch2 = c1.CreateChannel();
        var ch3 = c2.CreateChannel(true);
        var ch4 = c2.CreateChannel(true);
        var ch5 = c2.CreateChannel();
        ch1.Close();
        ch2.Close();
        ch3.Close();
        ch4.Close();
        ch5.Close();
        c1.Close();
        var props = connectionCf.GetCacheProperties();
        Assert.Equal(10, props["channelCacheSize"]);
        Assert.Equal(5, props["connectionCacheSize"]);
        Assert.Equal(2, props["openConnections"]);
        Assert.Equal(1, props["idleConnections"]);
        c2.Close();
        props = connectionCf.GetCacheProperties();
        Assert.Equal(2, props["idleConnections"]);
        Assert.Equal(2, props["idleConnectionsHighWater"]);
        var c1Port = c1.LocalPort;
        var c2Port = c2.LocalPort;
        Assert.StartsWith("testConnectionCache:1", (string)props[$"connectionName:{c1Port}"]);
        Assert.StartsWith("testConnectionCache:2", (string)props[$"connectionName:{c2Port}"]);
        Assert.Equal(2, props[$"idleChannelsNotTx:{c1Port}"]);
        Assert.Equal(0, props[$"idleChannelsTx:{c1Port}"]);
        Assert.Equal(2, props[$"idleChannelsNotTxHighWater:{c1Port}"]);
        Assert.Equal(0, props[$"idleChannelsTxHighWater:{c1Port}"]);
        Assert.Equal(1, props[$"idleChannelsNotTx:{c2Port}"]);
        Assert.Equal(2, props[$"idleChannelsTx:{c2Port}"]);
        Assert.Equal(1, props[$"idleChannelsNotTxHighWater:{c2Port}"]);
        Assert.Equal(2, props[$"idleChannelsTxHighWater:{c2Port}"]);
    }
}
