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
using Steeltoe.Messaging.Support;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.Converter.Test
{
    public class StringMessageConverterTest
    {
        [Fact]
        public void FromByteArrayMessage()
        {
            var message = MessageBuilder.WithPayload(
                    Encoding.UTF8.GetBytes("ABC")).SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN).Build();
            var converter = new StringMessageConverter();
            Assert.Equal("ABC", converter.FromMessage<string>(message));
        }

        [Fact]
        public void FromStringMessage()
        {
            var message = MessageBuilder.WithPayload(
                    "ABC").SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN).Build();
            var converter = new StringMessageConverter();
            Assert.Equal("ABC", converter.FromMessage<string>(message));
        }

        [Fact]
        public void FromMessageNoContentTypeHeader()
        {
            var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
            var converter = new StringMessageConverter();
            Assert.Equal("ABC", converter.FromMessage<string>(message));
        }

        [Fact]
        public void FromMessageCharset()
        {
            var payload = "H\u00e9llo W\u00f6rld";
            var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(payload);
            var message = MessageBuilder.WithPayload(bytes)
                    .SetHeader(MessageHeaders.CONTENT_TYPE, new MimeType("text", "plain", Encoding.GetEncoding("ISO-8859-1"))).Build();
            var converter = new StringMessageConverter();
            Assert.Equal(payload, converter.FromMessage<string>(message));
        }

        [Fact]
        public void FromMessageDefaultCharset()
        {
            var payload = "H\u00e9llo W\u00f6rld";
            var bytes = Encoding.UTF8.GetBytes(payload);
            var message = MessageBuilder.WithPayload(bytes).Build();
            var converter = new StringMessageConverter();
            Assert.Equal(payload, converter.FromMessage<string>(message));
        }

        [Fact]
        public void FromMessageTargetClassNotSupported()
        {
            var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("ABC")).Build();
            var converter = new StringMessageConverter();
            Assert.Null(converter.FromMessage<object>(message));
        }

        [Fact]
        public void FromMessageByteArray()
        {
            var message = MessageBuilder.WithPayload(
                    Encoding.UTF8.GetBytes("ABC")).SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN).Build();
            var converter = new StringMessageConverter();
            Assert.Equal("ABC", converter.FromMessage<string>(message));
        }

        [Fact]
        public void ToMessage()
        {
            var map = new Dictionary<string, object>();
            map.Add(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN);
            var headers = new MessageHeaders(map);
            var converter = new StringMessageConverter();
            var message = converter.ToMessage("ABC", headers);
            var result = Encoding.UTF8.GetString((byte[])message.Payload);
            Assert.Equal("ABC", result);
        }
    }
}
