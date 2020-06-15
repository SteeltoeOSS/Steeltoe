// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Support.Test
{
    public class MessageBuilderTest
    {
        [Fact]
        public void TestSimpleIMessageCreation()
        {
            var message = MessageBuilder<string>.WithPayload("foo").Build();
            Assert.Equal("foo", message.Payload);
        }

        [Fact]
        public void TestHeaderValues()
        {
            var message = MessageBuilder<string>.WithPayload("test")
                    .SetHeader("foo", "bar")
                    .SetHeader("count", 123)
                    .Build();
            Assert.Equal("bar", message.Headers.Get<string>("foo"));
            Assert.Equal(123, message.Headers.Get<int>("count"));
        }

        [Fact]
        public void TestCopiedHeaderValues()
        {
            var message1 = MessageBuilder<string>.WithPayload("test1")
                    .SetHeader("foo", "1")
                    .SetHeader("bar", "2")
                    .Build();
            var message2 = MessageBuilder<string>.WithPayload("test2")
                    .CopyHeaders(message1.Headers)
                    .SetHeader("foo", "42")
                    .SetHeaderIfAbsent("bar", "99")
                    .Build();
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
            Assert.Throws<ArgumentException>(() => MessageBuilder<string>.WithPayload("test").SetHeader(MessageHeaders.ID, id));
        }

        [Fact]
        public void TestTimestampValueReadOnly()
        {
            var timestamp = 12345L;
            Assert.Throws<ArgumentException>(() => MessageBuilder<string>.WithPayload("test").SetHeader(MessageHeaders.TIMESTAMP, timestamp).Build());
        }

        [Fact]
        public void CopyHeadersIfAbsent()
        {
            var message1 = MessageBuilder<string>.WithPayload("test1")
                    .SetHeader("foo", "bar").Build();
            var message2 = MessageBuilder<string>.WithPayload("test2")
                    .SetHeader("foo", 123)
                    .CopyHeadersIfAbsent(message1.Headers)
                    .Build();
            Assert.Equal("test2", message2.Payload);
            Assert.Equal(123, message2.Headers.Get<int>("foo"));
        }

        [Fact]
        public void CreateFromIMessage()
        {
            var message1 = MessageBuilder<string>.WithPayload("test")
                    .SetHeader("foo", "bar").Build();
            var message2 = MessageBuilder<string>.FromMessage(message1).Build();
            Assert.Equal("test", message2.Payload);
            Assert.Equal("bar", message2.Headers.Get<string>("foo"));
        }

        [Fact]
        public void CreateIdRegenerated()
        {
            var message1 = MessageBuilder<string>.WithPayload("test")
                    .SetHeader("foo", "bar").Build();
            var message2 = MessageBuilder<string>.FromMessage(message1).SetHeader("another", 1).Build();
            Assert.Equal("bar", message2.Headers.Get<string>("foo"));
            Assert.NotEqual(message1.Headers.Id, message2.Headers.Id);
        }

        [Fact]
        public void TestRemove()
        {
            var message1 = MessageBuilder<int>.WithPayload(1)
                .SetHeader("foo", "bar").Build();
            var message2 = MessageBuilder<int>.FromMessage(message1)
                .RemoveHeader("foo")
                .Build();
            Assert.False(message2.Headers.ContainsKey("foo"));
        }

        [Fact]
        public void TestSettingToNullRemoves()
        {
            var message1 = MessageBuilder<int>.WithPayload(1)
                .SetHeader("foo", "bar").Build();
            var message2 = MessageBuilder<int>.FromMessage(message1)
                .SetHeader("foo", null)
                .Build();
            Assert.False(message2.Headers.ContainsKey("foo"));
        }

        [Fact]
        public void TestNotModifiedSameIMessage()
        {
            var original = MessageBuilder<string>.WithPayload("foo").Build();
            var result = MessageBuilder<string>.FromMessage(original).Build();
            Assert.Equal(original, result);
        }

        [Fact]
        public void TestContainsHeaderNotModifiedSameIMessage()
        {
            var original = MessageBuilder<string>.WithPayload("foo").SetHeader("bar", 42).Build();
            var result = MessageBuilder<string>.FromMessage(original).Build();
            Assert.Equal(original, result);
        }

        [Fact]
        public void TestSameHeaderValueAddedNotModifiedSameIMessage()
        {
            var original = MessageBuilder<string>.WithPayload("foo").SetHeader("bar", 42).Build();
            var result = MessageBuilder<string>.FromMessage(original).SetHeader("bar", 42).Build();
            Assert.Equal(original, result);
        }

        [Fact]
        public void TestCopySameHeaderValuesNotModifiedSameIMessage()
        {
            var current = DateTime.Now;
            IDictionary<string, object> originalHeaders = new Dictionary<string, object>();
            originalHeaders.Add("b", "xyz");
            originalHeaders.Add("c", current);
            var original = MessageBuilder<string>.WithPayload("foo").SetHeader("a", 123).CopyHeaders(originalHeaders).Build();
            IDictionary<string, object> newHeaders = new Dictionary<string, object>();
            newHeaders.Add("a", 123);
            newHeaders.Add("b", "xyz");
            newHeaders.Add("c", current);
            var result = MessageBuilder<string>.FromMessage(original).CopyHeaders(newHeaders).Build();
            Assert.Equal(original, result);
        }

        [Fact]
        public void TestBuildIMessageWithMutableHeaders()
        {
            var accessor = new MessageHeaderAccessor();
            accessor.LeaveMutable = true;
            var headers = accessor.MessageHeaders;
            var message = MessageBuilder<string>.CreateMessage("payload", headers);
            accessor.SetHeader("foo", "bar");

            Assert.Equal("bar", headers.Get<string>("foo"));
            Assert.Equal(accessor, MessageHeaderAccessor.GetAccessor<MessageHeaderAccessor>(message, typeof(MessageHeaderAccessor)));
        }

        [Fact]
        public void TestBuildIMessageWithDefaultMutability()
        {
            var accessor = new MessageHeaderAccessor();
            var headers = accessor.MessageHeaders;
            var message = MessageBuilder<string>.CreateMessage("foo", headers);

            Assert.Throws<InvalidOperationException>(() => accessor.SetHeader("foo", "bar"));

            Assert.Equal(accessor, MessageHeaderAccessor.GetAccessor<MessageHeaderAccessor>(message, typeof(MessageHeaderAccessor)));
        }

        [Fact]
        public void TestBuildIMessageWithoutIdAndTimestamp()
        {
            var headerAccessor = new MessageHeaderAccessor();
            headerAccessor.IdGenerator = new TestIdGenerater();

            var message = MessageBuilder<string>.CreateMessage("foo", headerAccessor.MessageHeaders);
            Assert.Null(message.Headers.Id);
            Assert.Null(message.Headers.Timestamp);
        }

        [Fact]
        public void TestBuildMultipleIMessages()
        {
            var headerAccessor = new MessageHeaderAccessor();
            var messageBuilder = MessageBuilder<string>.WithPayload("payload").SetHeaders(headerAccessor);

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

        private class TestIdGenerater : IIDGenerator
        {
            public Guid GenerateId()
            {
                return MessageHeaders.ID_VALUE_NONE;
            }
        }
    }
}
