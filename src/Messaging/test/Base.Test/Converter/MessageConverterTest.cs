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
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Converter.Test
{
    public class MessageConverterTest
    {
        [Fact]
        public void SupportsTargetClass()
        {
            var message = MessageBuilder<string>.WithPayload("ABC").Build();
            var converter = new TestMessageConverter();
            Assert.Equal("success-from", converter.FromMessage(message, typeof(string)));

            Assert.Null(converter.FromMessage(message, typeof(int)));
        }

        [Fact]
        public void SupportsMimeType()
        {
            var message = MessageBuilder<string>.WithPayload(
                    "ABC").SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN).Build();
            var converter = new TestMessageConverter();
            Assert.Equal("success-from", converter.FromMessage(message, typeof(string)));
        }

        [Fact]
        public void SupportsMimeTypeNotSupported()
        {
            var message = MessageBuilder<string>.WithPayload(
                    "ABC").SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_JSON).Build();
            var converter = new TestMessageConverter();
            Assert.Null(converter.FromMessage(message, typeof(string)));
        }

        [Fact]
        public void SupportsMimeTypeNotSpecified()
        {
            var message = MessageBuilder<string>.WithPayload("ABC").Build();
            var converter = new TestMessageConverter();
            Assert.Equal("success-from", converter.FromMessage(message, typeof(string)));
        }

        [Fact]
        public void SupportsMimeTypeNoneConfigured()
        {
            var message = MessageBuilder<string>.WithPayload(
                    "ABC").SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_JSON).Build();
            var converter = new TestMessageConverter(new List<MimeType>());

            Assert.Equal("success-from", converter.FromMessage(message, typeof(string)));
        }

        [Fact]
        public void CanConvertFromStrictContentTypeMatch()
        {
            var converter = new TestMessageConverter(new List<MimeType>() { MimeTypeUtils.TEXT_PLAIN });
            converter.StrictContentTypeMatch = true;

            var message = MessageBuilder<string>.WithPayload("ABC").Build();
            Assert.False(converter.CanConvertFrom(message, typeof(string)));

            message = MessageBuilder<string>.WithPayload("ABC")
                    .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN).Build();
            Assert.True(converter.CanConvertFrom(message, typeof(string)));
        }

        [Fact]
        public void SetStrictContentTypeMatchWithNoSupportedMimeTypes()
        {
            var converter = new TestMessageConverter(new List<MimeType>());
            Assert.Throws<InvalidOperationException>(() => converter.StrictContentTypeMatch = true);
        }

        [Fact]
        public void ToMessageWithHeaders()
        {
            IDictionary<string, object> map = new Dictionary<string, object>();
            map.Add("foo", "bar");
            var headers = new MessageHeaders(map);
            var converter = new TestMessageConverter();
            var message = converter.ToMessage("ABC", headers);

            Assert.NotNull(message.Headers.Id);
            Assert.NotNull(message.Headers.Timestamp);
            Assert.Equal(MimeTypeUtils.TEXT_PLAIN, message.Headers[MessageHeaders.CONTENT_TYPE]);
            Assert.Equal("bar", message.Headers["foo"]);
        }

        [Fact]
        public void ToMessageWithMutableMessageHeaders()
        {
            var accessor = new MessageHeaderAccessor();
            accessor.SetHeader("foo", "bar");
            accessor.LeaveMutable = true;

            var headers = accessor.MessageHeaders;
            var converter = new TestMessageConverter();
            var message = converter.ToMessage("ABC", headers);

            Assert.Same(headers, message.Headers);
            Assert.Null(message.Headers.Id);
            Assert.Null(message.Headers.Timestamp);
            Assert.Equal(MimeTypeUtils.TEXT_PLAIN, message.Headers[MessageHeaders.CONTENT_TYPE]);
        }

        [Fact]
        public void ToMessageContentTypeHeader()
        {
            var converter = new TestMessageConverter();
            var message = converter.ToMessage("ABC", null);
            Assert.Equal(MimeTypeUtils.TEXT_PLAIN, message.Headers[MessageHeaders.CONTENT_TYPE]);
        }

        private class TestMessageConverter : AbstractMessageConverter
        {
            public TestMessageConverter()
                : base(MimeTypeUtils.TEXT_PLAIN)
            {
            }

            public TestMessageConverter(ICollection<MimeType> supportedMimeTypes)
            : base(supportedMimeTypes)
            {
            }

            protected override bool Supports(Type clazz)
            {
                return typeof(string).Equals(clazz);
            }

            protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
            {
                return "success-from";
            }

            protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
            {
                return "success-to";
            }
        }
    }
}
