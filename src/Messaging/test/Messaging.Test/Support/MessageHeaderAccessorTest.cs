// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Support;
using Xunit;
using HeadersDictionary = System.Collections.Generic.IDictionary<string, object>;

namespace Steeltoe.Messaging.Test.Support;

public class MessageHeaderAccessorTest
{
    [Fact]
    public void NewEmptyHeaders()
    {
        var accessor = new MessageHeaderAccessor();
        Assert.Equal(0, accessor.ToDictionary().Count);
    }

    [Fact]
    public void ExistingHeaders()
    {
        HeadersDictionary map = new Dictionary<string, object>
        {
            { "foo", "bar" },
            { "bar", "baz" }
        };

        IMessage<string> message = Message.Create("payload", map);

        var accessor = new MessageHeaderAccessor(message);
        IMessageHeaders actual = accessor.MessageHeaders;

        Assert.Equal(3, ((HeadersDictionary)actual).Count);
        Assert.Equal("bar", actual.Get<string>("foo"));
        Assert.Equal("baz", actual.Get<string>("bar"));
    }

    [Fact]
    public void ExistingHeadersModification()
    {
        HeadersDictionary map = new Dictionary<string, object>
        {
            { "foo", "bar" },
            { "bar", "baz" }
        };

        IMessage<string> message = Message.Create("payload", map);

        Thread.Sleep(50);

        var accessor = new MessageHeaderAccessor(message);
        accessor.SetHeader("foo", "BAR");
        IMessageHeaders actual = accessor.MessageHeaders;

        Assert.Equal(3, ((HeadersDictionary)actual).Count);
        Assert.NotEqual(message.Headers.Id, actual.Id);
        Assert.Equal("BAR", actual.Get<string>("foo"));
        Assert.Equal("baz", actual.Get<string>("bar"));
    }

    [Fact]
    public void TestRemoveHeader()
    {
        IMessage message = Message.Create("payload", SingletonMap("foo", "bar"));
        var accessor = new MessageHeaderAccessor(message);
        accessor.RemoveHeader("foo");
        HeadersDictionary headers = accessor.ToDictionary();
        Assert.False(headers.ContainsKey("foo"));
    }

    [Fact]
    public void TestRemoveHeaderEvenIfNull()
    {
        IMessage<string> message = Message.Create("payload", SingletonMap("foo", null));
        var accessor = new MessageHeaderAccessor(message);
        accessor.RemoveHeader("foo");
        HeadersDictionary headers = accessor.ToDictionary();
        Assert.False(headers.ContainsKey("foo"));
    }

    [Fact]
    public void RemoveHeaders()
    {
        HeadersDictionary map = new Dictionary<string, object>
        {
            { "foo", "bar" },
            { "bar", "baz" }
        };

        IMessage<string> message = Message.Create("payload", map);
        var accessor = new MessageHeaderAccessor(message);

        accessor.RemoveHeaders("fo*");

        IMessageHeaders actual = accessor.MessageHeaders;
        Assert.Equal(2, ((HeadersDictionary)actual).Count);
        Assert.Null(actual.Get<string>("foo"));
        Assert.Equal("baz", actual.Get<string>("bar"));
    }

    [Fact]
    public void CopyHeaders()
    {
        HeadersDictionary map1 = new Dictionary<string, object>
        {
            { "foo", "bar" }
        };

        IMessage<string> message = Message.Create("payload", map1);
        var accessor = new MessageHeaderAccessor(message);

        HeadersDictionary map2 = new Dictionary<string, object>
        {
            { "foo", "BAR" },
            { "bar", "baz" }
        };

        accessor.CopyHeaders(map2);

        IMessageHeaders actual = accessor.MessageHeaders;
        Assert.Equal(3, ((HeadersDictionary)actual).Count);
        Assert.Equal("BAR", actual.Get<string>("foo"));
        Assert.Equal("baz", actual.Get<string>("bar"));
    }

    [Fact]
    public void CopyHeadersIfAbsent()
    {
        HeadersDictionary map1 = new Dictionary<string, object>
        {
            { "foo", "bar" }
        };

        IMessage<string> message = Message.Create("payload", map1);
        var accessor = new MessageHeaderAccessor(message);

        HeadersDictionary map2 = new Dictionary<string, object>
        {
            { "foo", "BAR" },
            { "bar", "baz" }
        };

        accessor.CopyHeadersIfAbsent(map2);

        IMessageHeaders actual = accessor.MessageHeaders;
        Assert.Equal(3, ((HeadersDictionary)actual).Count);
        Assert.Equal("bar", actual.Get<string>("foo"));
        Assert.Equal("baz", actual.Get<string>("bar"));
    }

    [Fact]
    public void CopyHeadersFromNullMap()
    {
        var headers = new MessageHeaderAccessor();
        headers.CopyHeaders(null);
        headers.CopyHeadersIfAbsent(null);

        Assert.Equal(1, ((HeadersDictionary)headers.MessageHeaders).Count);
        Assert.Contains("id", ((HeadersDictionary)headers.MessageHeaders).Keys);
    }

    [Fact]
    public void ToDictionary()
    {
        var accessor = new MessageHeaderAccessor();

        accessor.SetHeader("foo", "bar1");
        HeadersDictionary map1 = accessor.ToDictionary();

        accessor.SetHeader("foo", "bar2");
        HeadersDictionary map2 = accessor.ToDictionary();

        accessor.SetHeader("foo", "bar3");
        HeadersDictionary map3 = accessor.ToDictionary();

        Assert.Equal(1, map1.Count);
        Assert.Equal(1, map2.Count);
        Assert.Equal(1, map3.Count);

        Assert.Equal("bar1", map1["foo"]);
        Assert.Equal("bar2", map2["foo"]);
        Assert.Equal("bar3", map3["foo"]);
    }

    [Fact]
    public void LeaveMutable()
    {
        var accessor = new MessageHeaderAccessor();
        accessor.SetHeader("foo", "bar");
        accessor.LeaveMutable = true;
        IMessageHeaders headers = accessor.MessageHeaders;
        IMessage<string> message = MessageBuilder.CreateMessage("payload", headers);

        accessor.SetHeader("foo", "baz");

        Assert.Equal("baz", headers.Get<string>("foo"));
        Assert.Same(accessor, MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor)));
    }

    [Fact]
    public void LeaveMutableDefaultBehavior()
    {
        var accessor = new MessageHeaderAccessor();
        accessor.SetHeader("foo", "bar");
        IMessageHeaders headers = accessor.MessageHeaders;
        IMessage<string> message = MessageBuilder.CreateMessage("payload", headers);

        Assert.Throws<InvalidOperationException>(() => accessor.LeaveMutable = true);

        Assert.Throws<InvalidOperationException>(() => accessor.SetHeader("foo", "baz"));

        Assert.Equal("bar", headers.Get<string>("foo"));
        Assert.Same(accessor, MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor)));
    }

    [Fact]
    public void GetAccessor()
    {
        var expected = new MessageHeaderAccessor();
        IMessage<string> message = MessageBuilder.CreateMessage("payload", expected.MessageHeaders);
        Assert.Same(expected, MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor)));
    }

    [Fact]
    public void GetMutableAccessorSameInstance()
    {
        var expected = new TestMessageHeaderAccessor
        {
            LeaveMutable = true
        };

        IMessage<string> message = MessageBuilder.CreateMessage("payload", expected.MessageHeaders);

        MessageHeaderAccessor actual = MessageHeaderAccessor.GetMutableAccessor(message);
        Assert.NotNull(actual);
        Assert.True(actual.IsMutable);
        Assert.Same(expected, actual);

        actual.SetHeader("foo", "bar");
        Assert.Equal("bar", message.Headers.Get<string>("foo"));
    }

    [Fact]
    public void GetMutableAccessorNewInstance()
    {
        IMessage message = MessageBuilder.WithPayload("payload").Build();

        MessageHeaderAccessor actual = MessageHeaderAccessor.GetMutableAccessor(message);
        Assert.NotNull(actual);
        Assert.True(actual.IsMutable);

        actual.SetHeader("foo", "bar");
    }

    [Fact]
    public void GetMutableAccessorNewInstanceMatchingType()
    {
        var expected = new TestMessageHeaderAccessor();
        IMessage message = MessageBuilder.CreateMessage("payload", expected.MessageHeaders);

        MessageHeaderAccessor actual = MessageHeaderAccessor.GetMutableAccessor(message);
        Assert.NotNull(actual);
        Assert.True(actual.IsMutable);
        Assert.Equal(typeof(TestMessageHeaderAccessor), actual.GetType());
    }

    [Fact]
    public void TimestampEnabled()
    {
        var accessor = new MessageHeaderAccessor
        {
            EnableTimestamp = true
        };

        Assert.NotNull(accessor.MessageHeaders.Timestamp);
    }

    [Fact]
    public void TimestampDefaultBehavior()
    {
        var accessor = new MessageHeaderAccessor();
        Assert.Null(accessor.MessageHeaders.Timestamp);
    }

    [Fact]
    public void IdGeneratorCustom()
    {
        var id = Guid.NewGuid();

        var accessor = new MessageHeaderAccessor
        {
            IdGenerator = new TestIdGenerator
            {
                Id = id.ToString()
            }
        };

        Assert.Equal(id.ToString(), accessor.MessageHeaders.Id);
    }

    [Fact]
    public void IdGeneratorDefaultBehavior()
    {
        var accessor = new MessageHeaderAccessor();
        Assert.NotNull(accessor.MessageHeaders.Id);
    }

    [Fact]
    public void IdTimestampWithMutableHeaders()
    {
        var accessor = new MessageHeaderAccessor
        {
            IdGenerator = new TestIdGenerator
            {
                Id = MessageHeaders.IdValueNone
            },
            EnableTimestamp = false,
            LeaveMutable = true
        };

        IMessageHeaders headers = accessor.MessageHeaders;

        Assert.Null(headers.Id);
        Assert.Null(headers.Timestamp);

        var id = Guid.NewGuid();

        accessor.IdGenerator = new TestIdGenerator
        {
            Id = id.ToString()
        };

        accessor.EnableTimestamp = true;
        accessor.SetImmutable();

        Assert.Equal(id.ToString(), accessor.MessageHeaders.Id);
        Assert.NotNull(headers.Timestamp);
    }

    private static HeadersDictionary SingletonMap(string key, object value)
    {
        return new Dictionary<string, object>
        {
            { key, value }
        };
    }

    private sealed class TestIdGenerator : IIdGenerator
    {
        public string Id { get; set; }

        public string GenerateId()
        {
            return Id;
        }
    }

    private sealed class TestMessageHeaderAccessor : MessageHeaderAccessor
    {
        public TestMessageHeaderAccessor()
        {
        }

        private TestMessageHeaderAccessor(IMessage message)
            : base(message)
        {
        }

        private TestMessageHeaderAccessor(MessageHeaders headers)
            : base(headers)
        {
        }

        protected override MessageHeaderAccessor CreateMutableAccessor(IMessage message)
        {
            return new TestMessageHeaderAccessor(message);
        }

        protected override MessageHeaderAccessor CreateMutableAccessor(IMessageHeaders messageHeaders)
        {
            return new TestMessageHeaderAccessor((MessageHeaders)messageHeaders);
        }
    }
}
