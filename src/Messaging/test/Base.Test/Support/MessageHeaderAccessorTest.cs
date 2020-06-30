// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Steeltoe.Messaging.Support.Test
{
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
            IDictionary<string, object> map = new Dictionary<string, object>();
            map.Add("foo", "bar");
            map.Add("bar", "baz");
            var message = Message.Create<string>("payload", map);

            var accessor = new MessageHeaderAccessor(message);
            var actual = accessor.MessageHeaders;

            Assert.Equal(3, actual.Count);
            Assert.Equal("bar", actual.Get<string>("foo"));
            Assert.Equal("baz", actual.Get<string>("bar"));
        }

        [Fact]
        public void ExistingHeadersModification()
        {
            IDictionary<string, object> map = new Dictionary<string, object>();
            map.Add("foo", "bar");
            map.Add("bar", "baz");
            var message = Message.Create<string>("payload", map);

            Thread.Sleep(50);

            var accessor = new MessageHeaderAccessor(message);
            accessor.SetHeader("foo", "BAR");
            var actual = accessor.MessageHeaders;

            Assert.Equal(3, actual.Count);
            Assert.NotEqual(message.Headers.Id, actual.Id);
            Assert.Equal("BAR", actual.Get<string>("foo"));
            Assert.Equal("baz", actual.Get<string>("bar"));
        }

        [Fact]
        public void TestRemoveHeader()
        {
            IMessage message = Message.Create<string>("payload", SingletonMap("foo", "bar"));
            var accessor = new MessageHeaderAccessor(message);
            accessor.RemoveHeader("foo");
            var headers = accessor.ToDictionary();
            Assert.False(headers.ContainsKey("foo"));
        }

        [Fact]
        public void TestRemoveHeaderEvenIfNull()
        {
            IMessage<string> message = Message.Create<string>("payload", SingletonMap("foo", null));
            var accessor = new MessageHeaderAccessor(message);
            accessor.RemoveHeader("foo");
            var headers = accessor.ToDictionary();
            Assert.False(headers.ContainsKey("foo"));
        }

        [Fact]
        public void RemoveHeaders()
        {
            IDictionary<string, object> map = new Dictionary<string, object>();
            map.Add("foo", "bar");
            map.Add("bar", "baz");
            var message = Message.Create<string>("payload", map);
            var accessor = new MessageHeaderAccessor(message);

            accessor.RemoveHeaders("fo*");

            var actual = accessor.MessageHeaders;
            Assert.Equal(2, actual.Count);
            Assert.Null(actual.Get<string>("foo"));
            Assert.Equal("baz", actual.Get<string>("bar"));
        }

        [Fact]
        public void CopyHeaders()
        {
            IDictionary<string, object> map1 = new Dictionary<string, object>();
            map1.Add("foo", "bar");
            var message = Message.Create<string>("payload", map1);
            var accessor = new MessageHeaderAccessor(message);

            IDictionary<string, object> map2 = new Dictionary<string, object>();
            map2.Add("foo", "BAR");
            map2.Add("bar", "baz");
            accessor.CopyHeaders(map2);

            var actual = accessor.MessageHeaders;
            Assert.Equal(3, actual.Count);
            Assert.Equal("BAR", actual.Get<string>("foo"));
            Assert.Equal("baz", actual.Get<string>("bar"));
        }

        [Fact]
        public void CopyHeadersIfAbsent()
        {
            IDictionary<string, object> map1 = new Dictionary<string, object>();
            map1.Add("foo", "bar");
            var message = Message.Create<string>("payload", map1);
            var accessor = new MessageHeaderAccessor(message);

            IDictionary<string, object> map2 = new Dictionary<string, object>();
            map2.Add("foo", "BAR");
            map2.Add("bar", "baz");
            accessor.CopyHeadersIfAbsent(map2);

            var actual = accessor.MessageHeaders;
            Assert.Equal(3, actual.Count);
            Assert.Equal("bar", actual.Get<string>("foo"));
            Assert.Equal("baz", actual.Get<string>("bar"));
        }

        [Fact]
        public void CopyHeadersFromNullMap()
        {
            var headers = new MessageHeaderAccessor();
            headers.CopyHeaders(null);
            headers.CopyHeadersIfAbsent(null);

            Assert.Equal(1, headers.MessageHeaders.Count);
            Assert.Contains("id", headers.MessageHeaders.Keys);
        }

        [Fact]
        public void ToDictionary()
        {
            var accessor = new MessageHeaderAccessor();

            accessor.SetHeader("foo", "bar1");
            var map1 = accessor.ToDictionary();

            accessor.SetHeader("foo", "bar2");
            var map2 = accessor.ToDictionary();

            accessor.SetHeader("foo", "bar3");
            var map3 = accessor.ToDictionary();

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
            var headers = accessor.MessageHeaders;
            var message = MessageBuilder.CreateMessage("payload", headers);

            accessor.SetHeader("foo", "baz");

            Assert.Equal("baz", headers.Get<string>("foo"));
            Assert.Same(accessor, MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor)));
        }

        [Fact]
        public void LeaveMutableDefaultBehavior()
        {
            var accessor = new MessageHeaderAccessor();
            accessor.SetHeader("foo", "bar");
            var headers = accessor.MessageHeaders;
            var message = MessageBuilder.CreateMessage("payload", headers);

            Assert.Throws<InvalidOperationException>(() => accessor.LeaveMutable = true);

            Assert.Throws<InvalidOperationException>(() => accessor.SetHeader("foo", "baz"));

            Assert.Equal("bar", headers.Get<string>("foo"));
            Assert.Same(accessor, MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor)));
        }

        [Fact]
        public void GetAccessor()
        {
            var expected = new MessageHeaderAccessor();
            var message = MessageBuilder.CreateMessage("payload", expected.MessageHeaders);
            Assert.Same(expected, MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor)));
        }

        [Fact]
        public void GetMutableAccessorSameInstance()
        {
            var expected = new TestMessageHeaderAccessor();
            expected.LeaveMutable = true;
            var message = MessageBuilder.CreateMessage("payload", expected.MessageHeaders);

            var actual = MessageHeaderAccessor.GetMutableAccessor(message);
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

            var actual = MessageHeaderAccessor.GetMutableAccessor(message);
            Assert.NotNull(actual);
            Assert.True(actual.IsMutable);

            actual.SetHeader("foo", "bar");
        }

        [Fact]
        public void GetMutableAccessorNewInstanceMatchingType()
        {
            var expected = new TestMessageHeaderAccessor();
            IMessage message = MessageBuilder.CreateMessage("payload", expected.MessageHeaders);

            var actual = MessageHeaderAccessor.GetMutableAccessor(message);
            Assert.NotNull(actual);
            Assert.True(actual.IsMutable);
            Assert.Equal(typeof(TestMessageHeaderAccessor), actual.GetType());
        }

        [Fact]
        public void TimestampEnabled()
        {
            var accessor = new MessageHeaderAccessor();
            accessor.EnableTimestamp = true;
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
            var accessor = new MessageHeaderAccessor();
            accessor.IdGenerator = new TestIdGenerator()
            {
                Id = id.ToString()
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
            var accessor = new MessageHeaderAccessor();
            accessor.IdGenerator = new TestIdGenerator()
            {
                Id = MessageHeaders.ID_VALUE_NONE
            };
            accessor.EnableTimestamp = false;
            accessor.LeaveMutable = true;
            var headers = accessor.MessageHeaders;

            Assert.Null(headers.Id);
            Assert.Null(headers.Timestamp);

            var id = Guid.NewGuid();
            accessor.IdGenerator = new TestIdGenerator()
            {
                Id = id.ToString()
            };

            accessor.EnableTimestamp = true;
            accessor.SetImmutable();

            Assert.Equal(id.ToString(), accessor.MessageHeaders.Id);
            Assert.NotNull(headers.Timestamp);
        }

        private static IDictionary<string, object> SingletonMap(string key, object value)
        {
            return new Dictionary<string, object>() { { key, value } };
        }

        private class TestIdGenerator : IIDGenerator
        {
            public string Id;

            public string GenerateId()
            {
                return Id;
            }
        }

        private class TestMessageHeaderAccessor : MessageHeaderAccessor
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

            protected override MessageHeaderAccessor CreateMutableAccessor(IMessageHeaders headers)
            {
                return new TestMessageHeaderAccessor((MessageHeaders)headers);
            }
        }
    }
}
