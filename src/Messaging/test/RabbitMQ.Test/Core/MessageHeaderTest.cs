// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Support;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Test.Core;

public sealed class MessageHeaderTest
{
    [Fact]
    public void TestReplyTo()
    {
        var properties = new RabbitHeaderAccessor
        {
            ReplyTo = "foo/bar"
        };

        Assert.Equal("bar", properties.ReplyToAddress.RoutingKey);
    }

    [Fact]
    public void TestReplyToNullByDefault()
    {
        var properties = new RabbitHeaderAccessor();
        Assert.Null(properties.ReplyTo);
        Assert.Null(properties.ReplyToAddress);
    }

    [Fact]
    public void TestDelayHeader()
    {
        var properties = new RabbitHeaderAccessor();
        const int delay = 100;
        properties.Delay = delay;
        IMessageHeaders headers = properties.ToMessageHeaders();
        Assert.Equal(delay, headers.Get<int>(RabbitHeaderAccessor.XDelay));
        properties.Delay = null;
        headers = properties.ToMessageHeaders();
        Assert.False(headers.ContainsKey(RabbitHeaderAccessor.XDelay));
    }

    [Fact]
    public void TestContentLengthSet()
    {
        var properties = new RabbitHeaderAccessor
        {
            ContentLength = 1L
        };

        Assert.True(properties.IsContentLengthSet);
    }

    [Fact]
    public void TesNoNullPointerInEquals()
    {
        var mp = new RabbitHeaderAccessor
        {
            LeaveMutable = true
        };

        var mp2 = new RabbitHeaderAccessor
        {
            LeaveMutable = true
        };

        Assert.True(mp.MessageHeaders.Equals(mp2.MessageHeaders));
    }

    [Fact]
    public void TesNoNullPointerInHashCode()
    {
        var messageList = new HashSet<RabbitHeaderAccessor>
        {
            new()
        };

        Assert.Single(messageList);
    }
}
