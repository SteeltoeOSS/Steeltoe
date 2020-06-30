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

using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Integration.Support.Test
{
    public class MutableMessageTest
    {
        [Fact]
        public void TestMessageIdTimestampRemains()
        {
            var uuid = Guid.NewGuid();
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var payload = new object();
            var headerDictionary = new Dictionary<string, object>();

            headerDictionary.Add(MessageHeaders.ID, uuid);
            headerDictionary.Add(MessageHeaders.TIMESTAMP, timestamp);

            var mutableMessage = new MutableMessage<object>(payload, headerDictionary);
            var headers = mutableMessage.Headers as MutableMessageHeaders;

            Assert.Equal(uuid, headers.RawHeaders[MessageHeaders.ID]);
            Assert.Equal(timestamp, headers.RawHeaders[MessageHeaders.TIMESTAMP]);
        }

        [Fact]
        public void TestMessageHeaderIsSettable()
        {
            var payload = new object();
            var headerDictionary = new Dictionary<string, object>();
            var additional = new Dictionary<string, object>();

            var mutableMessage = new MutableMessage<object>(payload, headerDictionary);
            var headers = mutableMessage.Headers as MutableMessageHeaders;

            // Should not throw an UnsupportedOperationException
            headers.Add("foo", "bar");
            headers.Add("eep", "bar");
            headers.Remove("eep");
            headers.AddRange(additional);

            Assert.Equal("bar", headers.RawHeaders["foo"]);
        }

        [Fact]
        public void TestMessageHeaderIsSerializable()
        {
            var payload = new object();

            var uuid = Guid.NewGuid();
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            // UUID as string; timestamp as string
            var headerDictionarystrings = new Dictionary<string, object>();
            headerDictionarystrings.Add(MessageHeaders.ID, uuid.ToString());
            headerDictionarystrings.Add(MessageHeaders.TIMESTAMP, timestamp.ToString());
            var mutableMessagestrings = new MutableMessage<object>(payload, headerDictionarystrings);
            Assert.Equal(uuid.ToString(), mutableMessagestrings.Headers.Id);
            Assert.Equal(timestamp, mutableMessagestrings.Headers.Timestamp);

            // UUID as byte[]; timestamp as Long
            var headerDictionaryByte = new Dictionary<string, object>();
            var uuidAsBytes = uuid.ToByteArray();

            headerDictionaryByte.Add(MessageHeaders.ID, uuidAsBytes);
            headerDictionaryByte.Add(MessageHeaders.TIMESTAMP, timestamp);
            var mutableMessageBytes = new MutableMessage<object>(payload, headerDictionaryByte);
            Assert.Equal(uuid.ToString(), mutableMessageBytes.Headers.Id);
            Assert.Equal(timestamp, mutableMessageBytes.Headers.Timestamp);
        }
    }
}
