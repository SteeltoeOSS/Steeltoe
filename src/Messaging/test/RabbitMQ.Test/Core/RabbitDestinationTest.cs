// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class RabbitDestinationTest
{
    [Fact]
    public void TestConvertRabbitDestinationToString()
    {
        var d = new RabbitDestination("foo", "bar");
        var str = d;
        Assert.Equal("foo/bar", str);
        var returned = ReceiveStringReturnDestination(str);
        Assert.Equal("foo", returned.ExchangeName);
        Assert.Equal("bar", returned.RoutingKey);
        Assert.Equal("bar", returned.QueueName);

        d = new RabbitDestination("bar");
        str = d;
        Assert.Equal("bar", str);
        returned = ReceiveString2ReturnDestination(str);
        Assert.Equal(string.Empty, returned.ExchangeName);
        Assert.Equal("bar", returned.RoutingKey);
        Assert.Equal("bar", returned.QueueName);
    }

    [Fact]
    public void TestConvertStringToRabbitDestination()
    {
        RabbitDestination d = "foo/bar";
        Assert.Equal("foo", d.ExchangeName);
        Assert.Equal("bar", d.RoutingKey);
        Assert.Equal("bar", d.QueueName);

        var returned = ReceiveDestinationReturnString(d);
        Assert.Equal("foo/bar", returned);

        d = "bar";
        Assert.Equal(string.Empty, d.ExchangeName);
        Assert.Equal("bar", d.RoutingKey);
        Assert.Equal("bar", d.QueueName);

        returned = ReceiveDestination2ReturnString(d);
        Assert.Equal("bar", returned);

        d = "/bar";
        Assert.Equal(string.Empty, d.ExchangeName);
        Assert.Equal("bar", d.RoutingKey);
        Assert.Equal("bar", d.QueueName);

        returned = ReceiveDestination2ReturnString(d);
        Assert.Equal("bar", returned);
    }

    public RabbitDestination ReceiveStringReturnDestination(string destination)
    {
        Assert.Equal("foo/bar", destination);
        return destination;
    }

    public RabbitDestination ReceiveString2ReturnDestination(string destination)
    {
        Assert.Equal("bar", destination);
        return destination;
    }

    public string ReceiveDestinationReturnString(RabbitDestination destination)
    {
        Assert.Equal("foo/bar", destination);
        return destination;
    }

    public string ReceiveDestination2ReturnString(RabbitDestination destination)
    {
        Assert.Equal("bar", destination);
        return destination;
    }
}