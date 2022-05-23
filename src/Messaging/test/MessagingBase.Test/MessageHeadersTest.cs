// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Steeltoe.Messaging.Test
{
    public class MessageHeadersTest
    {
        [Fact]
        public void TestTimestamp()
        {
            var headers = new MessageHeaders();
            Assert.NotNull(headers.Timestamp);
        }

        [Fact]
        public void TestTimestampOverwritten()
        {
            var headers1 = new MessageHeaders();
            Thread.Sleep(50);
            var headers2 = new MessageHeaders(headers1);
            Assert.NotEqual(headers1.Timestamp, headers2.Timestamp);
        }

        [Fact]
        public void TestTimestampProvided()
        {
            var headers = new MessageHeaders(null, null, 10L);
            Assert.Equal(10L, (long)headers.Timestamp);
        }

        [Fact]
        public void TestTimestampProvidedNullValue()
        {
            var input = new Dictionary<string, object> { { MessageHeaders.TIMESTAMP, 1L } };
            var headers = new MessageHeaders(input, null, null);
            Assert.NotNull(headers.Timestamp);
        }

        [Fact]
        public void TestTimestampNone()
        {
            var headers = new MessageHeaders(null, null, -1L);
            Assert.Null(headers.Timestamp);
        }

        [Fact]
        public void TestIdOverwritten()
        {
            var headers1 = new MessageHeaders();
            var headers2 = new MessageHeaders(headers1);
            Assert.NotEqual(headers1.Id, headers2.Id);
        }

        [Fact]
        public void TestId()
        {
            var headers = new MessageHeaders();
            Assert.NotNull(headers.Id);
        }

        [Fact]
        public void TestIdProvided()
        {
            var id = Guid.NewGuid();
            var headers = new MessageHeaders(null, id.ToString(), null);
            Assert.Equal(id.ToString(), headers.Id);
        }

        [Fact]
        public void TestIdProvidedNullValue()
        {
            var id = Guid.NewGuid();
            var input = new Dictionary<string, object> { { MessageHeaders.ID, id } };
            var headers = new MessageHeaders(input, null, null);
            Assert.NotNull(headers.Id);
        }

        [Fact]
        public void TestIdNone()
        {
            var headers = new MessageHeaders(null, MessageHeaders.ID_VALUE_NONE, null);
            Assert.Null(headers.Id);
        }

        [Fact]
        public void TestNonTypedAccessOfHeaderValue()
        {
            var map = new Dictionary<string, object>
            {
                { "test", 123 }
            };
            var headers = new MessageHeaders(map);
            Assert.Equal(123, headers["test"]);
        }

        [Fact]
        public void TestTypedAccessOfHeaderValue()
        {
            var map = new Dictionary<string, object>
            {
                { "test", 123 }
            };
            var headers = new MessageHeaders(map);
            Assert.Equal(123, headers.Get<int>("test"));
        }

        [Fact]
        public void TestHeaderValueAccessWithIncorrectType()
        {
            var map = new Dictionary<string, object>
            {
                { "test", 123 }
            };
            var headers = new MessageHeaders(map);
            Assert.Throws<InvalidCastException>(() => headers.Get<string>("test"));
        }

        [Fact]
        public void TestNullHeaderValue()
        {
            var map = new Dictionary<string, object>();
            var headers = new MessageHeaders(map);
            headers.TryGetValue("nosuchattribute", out var val);
            Assert.Null(val);
        }

        [Fact]
        public void TestNullHeaderValueWithTypedAccess()
        {
            var map = new Dictionary<string, object>();
            var headers = new MessageHeaders(map);
            Assert.Null(headers.Get<string>("nosuchattribute"));
        }

        [Fact]
        public void TestHeaderKeys()
        {
            var map = new Dictionary<string, object>
            {
                { "key1", "val1" },
                { "key2", 123 }
            };
            var headers = new MessageHeaders(map);
            var keys = headers.Keys;
            Assert.True(keys.Contains("key1"));
            Assert.True(keys.Contains("key2"));
        }

        [Fact]
        public void SubclassWithCustomIdAndNoTimestamp()
        {
            var id = Guid.NewGuid();
            MessageHeaders headers = new MyMH(id);
            Assert.Equal(id.ToString(), headers.Id);
            Assert.Single(headers);
        }

        private class MyMH : MessageHeaders
        {
            public MyMH(Guid id)
                : base(null, id.ToString(), -1L)
            {
            }
        }
    }
}
