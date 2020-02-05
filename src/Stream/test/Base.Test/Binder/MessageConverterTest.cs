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

using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System;
using System.Text;
using Xunit;

namespace Steeltoe.Stream.Binder
{
    public class MessageConverterTest
    {
        [Fact]
        public void TestHeaderEmbedding()
        {
            var message = MessageBuilder<byte[]>
                .WithPayload(Encoding.UTF8.GetBytes("Hello"))
                .SetHeader("foo", "bar")
                .SetHeader("baz", "quxx")
                .Build();
            var embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
            Assert.Equal(0xff, embedded[0]);
            var embeddedString = Encoding.UTF8.GetString(embedded);
            Assert.Equal("\u0002\u0003foo\u0000\u0000\u0000\u0005\"bar\"\u0003baz\u0000\u0000\u0000\u0006\"quxx\"Hello", embeddedString.Substring(1));
            var extracted = EmbeddedHeaderUtils.ExtractHeaders(MessageBuilder<byte[]>.WithPayload(embedded).Build(), false);
            var extractedString = Encoding.UTF8.GetString((byte[])extracted.Payload);
            Assert.Equal("Hello", extractedString);
            Assert.Equal("bar", extracted["foo"]);
            Assert.Equal("quxx", extracted["baz"]);
        }

        [Fact]
        public void TestConfigurableHeaders()
        {
            var message = MessageBuilder<byte[]>
                .WithPayload(Encoding.UTF8.GetBytes("Hello"))
                .SetHeader("foo", "bar")
                .SetHeader("baz", "quxx")
                .SetHeader("contentType", "text/plain")
                .Build();

            var headers = new string[] { "foo" };
            var embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), EmbeddedHeaderUtils.HeadersToEmbed(headers));
            Assert.Equal(0xff, embedded[0]);
            var embeddedString = Encoding.UTF8.GetString(embedded);
            Assert.Equal("\u0002\u000BcontentType\u0000\u0000\u0000\u000C\"text/plain\"\u0003foo\u0000\u0000\u0000\u0005\"bar\"Hello", embeddedString.Substring(1));
            var extracted = EmbeddedHeaderUtils.ExtractHeaders(MessageBuilder<byte[]>.WithPayload(embedded).Build(), false);

            Assert.Equal("Hello", Encoding.UTF8.GetString((byte[])extracted.Payload));
            Assert.Equal("bar", extracted["foo"]);
            Assert.Null(extracted["baz"]);
            Assert.Equal("text/plain", extracted["contentType"]);
            Assert.Null(extracted["timestamp"]);

            var extractedWithRequestHeaders = EmbeddedHeaderUtils.ExtractHeaders(MessageBuilder<byte[]>.WithPayload(embedded).Build(), true);
            Assert.Equal("bar", extractedWithRequestHeaders["foo"]);
            Assert.Null(extractedWithRequestHeaders["baz"]);
            Assert.Equal("text/plain", extractedWithRequestHeaders["contentType"]);
            Assert.NotNull(extractedWithRequestHeaders["timestamp"]);
        }

        [Fact]
        public void TestHeaderExtractionWithDirectPayload()
        {
            var message = MessageBuilder<byte[]>
                    .WithPayload(Encoding.UTF8.GetBytes("Hello"))
                    .SetHeader("foo", "bar")
                    .SetHeader("baz", "quxx")
                    .Build();
            var embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
            Assert.Equal(0xff, embedded[0]);
            var embeddedString = Encoding.UTF8.GetString(embedded);
            Assert.Equal("\u0002\u0003foo\u0000\u0000\u0000\u0005\"bar\"\u0003baz\u0000\u0000\u0000\u0006\"quxx\"Hello", embeddedString.Substring(1));

            var extracted = EmbeddedHeaderUtils.ExtractHeaders(embedded);
            Assert.Equal("Hello", Encoding.UTF8.GetString((byte[])extracted.Payload));
            Assert.Equal("bar", extracted["foo"]);
            Assert.Equal("quxx", extracted["baz"]);
        }

        [Fact]
        public void TestUnicodeHeader()
        {
            var message = MessageBuilder<byte[]>
                    .WithPayload(Encoding.UTF8.GetBytes("Hello"))
                    .SetHeader("foo", "bar")
                    .SetHeader("baz", "ØØØØØØØØ")
                    .Build();

            var embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
            Assert.Equal(0xff, embedded[0]);
            var embeddedString = Encoding.UTF8.GetString(embedded);
            Assert.Equal("\u0002\u0003foo\u0000\u0000\u0000\u0005\"bar\"\u0003baz\u0000\u0000\u0000\u0012\"ØØØØØØØØ\"Hello", embeddedString.Substring(1));

            var extracted = EmbeddedHeaderUtils.ExtractHeaders(MessageBuilder<byte[]>.WithPayload(embedded).Build(), false);
            Assert.Equal("Hello", Encoding.UTF8.GetString((byte[])extracted.Payload));
            Assert.Equal("bar", extracted["foo"]);
            Assert.Equal("ØØØØØØØØ", extracted["baz"]);
        }

        [Fact]
        public void TestHeaderEmbeddingMissingHeader()
        {
            var message = MessageBuilder<byte[]>
                    .WithPayload(Encoding.UTF8.GetBytes("Hello"))
                    .SetHeader("foo", "bar")
                    .Build();
            var embedded = EmbeddedHeaderUtils.EmbedHeaders(new MessageValues(message), "foo", "baz");
            Assert.Equal(0xff, embedded[0]);
            var embeddedString = Encoding.UTF8.GetString(embedded);
            Assert.Equal("\u0001\u0003foo\u0000\u0000\u0000\u0005\"bar\"Hello", embeddedString.Substring(1));
        }

        [Fact]
        public void TestBadDecode()
        {
            var bytes = new byte[] { (byte)0xff, 99 };
            IMessage<byte[]> message = new Steeltoe.Messaging.Support.GenericMessage<byte[]>(bytes);
            Assert.Throws<ArgumentOutOfRangeException>(() => EmbeddedHeaderUtils.ExtractHeaders(message, false));
        }
    }
}
