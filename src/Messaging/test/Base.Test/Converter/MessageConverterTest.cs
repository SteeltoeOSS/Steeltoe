// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
