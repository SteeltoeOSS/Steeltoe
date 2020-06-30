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

using Steeltoe.Messaging.Rabbit.Extensions;
using System.Text;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class SimpleMessageConverterTest
    {
        [Fact]
        public void BytesAsDefaultMessageBodyType()
        {
            var converter = new SimpleMessageConverter();
            var message = Message.Create(Encoding.UTF8.GetBytes("test"), new MessageHeaders());
            var result = converter.FromMessage<byte[]>(message);
            Assert.Equal("test", Encoding.UTF8.GetString(result));
        }

        [Fact]
        public void MessageToString()
        {
            var converter = new SimpleMessageConverter();
            var message = Message.Create(Encoding.UTF8.GetBytes("test"), new MessageHeaders());
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ContentType = MessageHeaders.CONTENT_TYPE_TEXT_PLAIN;
            var result = converter.FromMessage<string>(message);
            Assert.Equal("test", result);
        }

        [Fact]
        public void MessageToBytes()
        {
            var converter = new SimpleMessageConverter();
            var message = Message.Create(new byte[] { 1, 2, 3 }, new MessageHeaders());
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ContentType = MessageHeaders.CONTENT_TYPE_BYTES;
            var result = converter.FromMessage<byte[]>(message);
            Assert.Equal(3, result.Length);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void StringToMessage()
        {
            var converter = new SimpleMessageConverter();
            var message = converter.ToMessage("test", new MessageHeaders());
            var contentType = message.Headers.ContentType();
            var contentEncoding = message.Headers.ContentEncoding();
            var encoding = Encoding.GetEncoding(contentEncoding);
            var content = encoding.GetString((byte[])message.Payload);
            Assert.Equal("text/plain", contentType);
            Assert.Equal("test", content);
        }

        [Fact]
        public void BytesToMessage()
        {
            var converter = new SimpleMessageConverter();
            var message = converter.ToMessage(new byte[] { 1, 2, 3 }, new MessageHeaders());
            var contentType = message.Headers.ContentType();
            byte[] body = message.Payload as byte[];
            Assert.Equal(MessageHeaders.CONTENT_TYPE_BYTES, contentType);
            Assert.Equal(3, body.Length);
            Assert.Equal(1, body[0]);
            Assert.Equal(2, body[1]);
            Assert.Equal(3, body[2]);
        }
    }
}
