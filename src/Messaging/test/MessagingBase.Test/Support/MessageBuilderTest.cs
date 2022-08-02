// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Xunit;

namespace Steeltoe.Messaging.Support.Test;

public class MessageBuilderTest
{
    [Fact]
    public void TestSimpleIMessageCreation()
    {
        IMessage message = MessageBuilder.WithPayload("foo").Build();
        Assert.Equal("foo", message.Payload);
    }

    [Fact]
    public void TestHeaderValues()
    {
        IMessage message = MessageBuilder.WithPayload("test").SetHeader("foo", "bar").SetHeader("count", 123).Build();
        Assert.Equal("bar", message.Headers.Get<string>("foo"));
        Assert.Equal(123, message.Headers.Get<int>("count"));
    }

    [Fact]
    public void TestCopiedHeaderValues()
    {
        IMessage message1 = MessageBuilder.WithPayload("test1").SetHeader("foo", "1").SetHeader("bar", "2").Build();
        IMessage message2 = MessageBuilder.WithPayload("test2").CopyHeaders(message1.Headers).SetHeader("foo", "42").SetHeaderIfAbsent("bar", "99").Build();
        Assert.Equal("test1", message1.Payload);
        Assert.Equal("test2", message2.Payload);
        Assert.Equal("1", message1.Headers.Get<string>("foo"));
        Assert.Equal("42", message2.Headers.Get<string>("foo"));
        Assert.Equal("2", message1.Headers.Get<string>("bar"));
        Assert.Equal("2", message2.Headers.Get<string>("bar"));
    }

    [Fact]
    public void TestIdHeaderValueReadOnly()
    {
        var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => MessageBuilder.WithPayload("test").SetHeader(MessageHeaders.IdName, id));
    }

    [Fact]
    public void TestTimestampValueReadOnly()
    {
        long timestamp = 12345L;
        Assert.Throws<ArgumentException>(() => MessageBuilder.WithPayload("test").SetHeader(MessageHeaders.TimestampName, timestamp).Build());
    }

    [Fact]
    public void CopyHeadersIfAbsent()
    {
        IMessage message1 = MessageBuilder.WithPayload("test1").SetHeader("foo", "bar").Build();
        IMessage message2 = MessageBuilder.WithPayload("test2").SetHeader("foo", 123).CopyHeadersIfAbsent(message1.Headers).Build();
        Assert.Equal("test2", message2.Payload);
        Assert.Equal(123, message2.Headers.Get<int>("foo"));
    }

    [Fact]
    public void CreateFromIMessage()
    {
        IMessage message1 = MessageBuilder.WithPayload("test").SetHeader("foo", "bar").Build();
        IMessage message2 = MessageBuilder.FromMessage(message1).Build();
        Assert.Equal("test", message2.Payload);
        Assert.Equal("bar", message2.Headers.Get<string>("foo"));
    }

    [Fact]
    public void CreateIdRegenerated()
    {
        IMessage message1 = MessageBuilder.WithPayload("test").SetHeader("foo", "bar").Build();
        IMessage message2 = MessageBuilder.FromMessage(message1).SetHeader("another", 1).Build();
        Assert.Equal("bar", message2.Headers.Get<string>("foo"));
        Assert.NotEqual(message1.Headers.Id, message2.Headers.Id);
    }

    [Fact]
    public void TestRemove()
    {
        IMessage message1 = MessageBuilder.WithPayload(1).SetHeader("foo", "bar").Build();
        IMessage message2 = MessageBuilder.FromMessage(message1).RemoveHeader("foo").Build();
        Assert.False(message2.Headers.ContainsKey("foo"));
    }

    [Fact]
    public void TestSettingToNullRemoves()
    {
        IMessage message1 = MessageBuilder.WithPayload(1).SetHeader("foo", "bar").Build();
        IMessage message2 = MessageBuilder.FromMessage(message1).SetHeader("foo", null).Build();
        Assert.False(message2.Headers.ContainsKey("foo"));
    }

    [Fact]
    public void TestNotModifiedSameIMessage()
    {
        IMessage original = MessageBuilder.WithPayload("foo").Build();
        IMessage result = MessageBuilder.FromMessage(original).Build();
        Assert.Equal(original, result);
    }

    [Fact]
    public void TestContainsHeaderNotModifiedSameIMessage()
    {
        IMessage original = MessageBuilder.WithPayload("foo").SetHeader("bar", 42).Build();
        IMessage result = MessageBuilder.FromMessage(original).Build();
        Assert.Equal(original, result);
    }

    [Fact]
    public void TestSameHeaderValueAddedNotModifiedSameIMessage()
    {
        IMessage original = MessageBuilder.WithPayload("foo").SetHeader("bar", 42).Build();
        IMessage result = MessageBuilder.FromMessage(original).SetHeader("bar", 42).Build();
        Assert.Equal(original, result);
    }

    [Fact]
    public void TestCopySameHeaderValuesNotModifiedSameIMessage()
    {
        DateTime current = DateTime.Now;

        IDictionary<string, object> originalHeaders = new Dictionary<string, object>
        {
            { "b", "xyz" },
            { "c", current }
        };

        IMessage original = MessageBuilder.WithPayload("foo").SetHeader("a", 123).CopyHeaders(originalHeaders).Build();

        IDictionary<string, object> newHeaders = new Dictionary<string, object>
        {
            { "a", 123 },
            { "b", "xyz" },
            { "c", current }
        };

        IMessage result = MessageBuilder.FromMessage(original).CopyHeaders(newHeaders).Build();
        Assert.Equal(original, result);
    }

    [Fact]
    public void TestBuildIMessageWithMutableHeaders()
    {
        var accessor = new MessageHeaderAccessor
        {
            LeaveMutable = true
        };

        IMessageHeaders headers = accessor.MessageHeaders;
        IMessage<string> message = MessageBuilder.CreateMessage("payload", headers);
        accessor.SetHeader("foo", "bar");

        Assert.Equal("bar", headers.Get<string>("foo"));
        Assert.Equal(accessor, MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor)));
    }

    [Fact]
    public void TestBuildIMessageWithDefaultMutability()
    {
        var accessor = new MessageHeaderAccessor();
        IMessageHeaders headers = accessor.MessageHeaders;
        IMessage<string> message = MessageBuilder.CreateMessage("foo", headers);

        Assert.Throws<InvalidOperationException>(() => accessor.SetHeader("foo", "bar"));

        Assert.Equal(accessor, MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor)));
    }

    [Fact]
    public void TestBuildIMessageWithoutIdAndTimestamp()
    {
        var headerAccessor = new MessageHeaderAccessor
        {
            IdGenerator = new TestIdGenerator()
        };

        IMessage<string> message = MessageBuilder.CreateMessage("foo", headerAccessor.MessageHeaders);
        Assert.Null(message.Headers.Id);
        Assert.Null(message.Headers.Timestamp);
    }

    [Fact]
    public void TestBuildMultipleIMessages()
    {
        var headerAccessor = new MessageHeaderAccessor();
        AbstractMessageBuilder messageBuilder = MessageBuilder.WithPayload("payload").SetHeaders(headerAccessor);

        headerAccessor.SetHeader("foo", "bar1");
        IMessage message1 = messageBuilder.Build();

        headerAccessor.SetHeader("foo", "bar2");
        IMessage message2 = messageBuilder.Build();

        headerAccessor.SetHeader("foo", "bar3");
        IMessage message3 = messageBuilder.Build();

        Assert.Equal("bar1", message1.Headers.Get<string>("foo"));
        Assert.Equal("bar2", message2.Headers.Get<string>("foo"));
        Assert.Equal("bar3", message3.Headers.Get<string>("foo"));
    }

    private sealed class TestIdGenerator : IIdGenerator
    {
        public string GenerateId()
        {
            return MessageHeaders.IdValueNone;
        }
    }
}
