// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using System;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Support;

public class RabbitHeaderAccessorTest
{
    [Fact]
    public void NewEmptyHeaders()
    {
        var accessor = new RabbitHeaderAccessor();
        Assert.Equal(0, accessor.ToDictionary().Count);
    }

    [Fact]
    public void ValidateAmqpHeaders()
    {
        var accessor = new RabbitHeaderAccessor();
        var correlationId = "correlation-id-1234";
        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        accessor.AppId = "app-id-1234";
        accessor.ClusterId = "cluster-id-1234";
        accessor.ContentEncoding = "UTF-16";
        accessor.ContentLength = 200L;
        accessor.ContentType = "text/plain";
        accessor.CorrelationId = correlationId;
        accessor.ReceivedDeliveryMode = MessageDeliveryMode.NON_PERSISTENT;
        accessor.DeliveryTag = 555L;
        accessor.Expiration = "expiration-1234";
        accessor.MessageCount = 42;
        accessor.MessageId = "message-id-1234";
        accessor.Priority = 9;
        accessor.ReceivedExchange = "received-exchange-1234";
        accessor.ReceivedRoutingKey = "received-routing-key-1234";
        accessor.ReceivedDelay = 1234;
        accessor.Redelivered = true;
        accessor.ReplyTo = "reply-to-1234";
        accessor.Timestamp = time;
        accessor.Type = "type-1234";
        accessor.ReceivedUserId = "user-id-1234";
        accessor.ConsumerTag = "consumer.tag";
        accessor.ConsumerQueue = "consumer.queue";

        var message = RabbitMessageBuilder.WithPayload("test").SetHeaders(accessor).Build();

        var headerAccessor = RabbitHeaderAccessor.GetAccessor(message);

        Assert.Equal("app-id-1234", headerAccessor.AppId);
        Assert.Equal("cluster-id-1234", headerAccessor.ClusterId);
        Assert.Equal("UTF-16", headerAccessor.ContentEncoding);
        Assert.Equal(200, headerAccessor.ContentLength);
        Assert.Equal("text/plain", headerAccessor.ContentType);
        Assert.Equal(correlationId, headerAccessor.CorrelationId);
        Assert.Equal(MessageDeliveryMode.NON_PERSISTENT, headerAccessor.ReceivedDeliveryMode);
        Assert.Equal(555ul, headerAccessor.DeliveryTag.Value);
        Assert.Equal("expiration-1234", headerAccessor.Expiration);
        Assert.Equal(42u, headerAccessor.MessageCount.Value);
        Assert.Equal("message-id-1234", headerAccessor.MessageId);
        Assert.Equal(9, headerAccessor.Priority);
        Assert.Equal("received-exchange-1234", headerAccessor.ReceivedExchange);
        Assert.Equal("received-routing-key-1234", headerAccessor.ReceivedRoutingKey);
        Assert.Equal(1234, headerAccessor.ReceivedDelay);
        Assert.Equal(true, headerAccessor.Redelivered);
        Assert.Equal("reply-to-1234", headerAccessor.ReplyTo);
        Assert.Equal(time, headerAccessor.Timestamp);
        Assert.Equal("type-1234", headerAccessor.Type);
        Assert.Equal("user-id-1234", headerAccessor.ReceivedUserId);
        Assert.Equal("consumer.tag", headerAccessor.ConsumerTag);
        Assert.Equal("consumer.queue", headerAccessor.ConsumerQueue);

        // Making sure replyChannel is not mixed with replyTo
        Assert.Null(headerAccessor.ReplyChannel);
    }

    [Fact]
    public void PrioritySet()
    {
        var message = RabbitMessageBuilder.WithPayload("payload").SetHeader(RabbitMessageHeaders.PRIORITY, 90).Build();
        var accessor = RabbitHeaderAccessor.GetAccessor(message);
        Assert.Equal(90, accessor.Priority.Value);
    }

    [Fact]
    public void PriorityMustBeInteger()
    {
        var accessor = new RabbitHeaderAccessor(RabbitMessageBuilder.WithPayload("foo").Build());
        Assert.Throws<ArgumentException>(() => accessor.SetHeader(RabbitMessageHeaders.PRIORITY, "foo"));
    }
}