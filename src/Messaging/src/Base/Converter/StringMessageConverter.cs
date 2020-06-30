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
using System.Text;

namespace Steeltoe.Messaging.Converter
{
    public class StringMessageConverter : AbstractMessageConverter
    {
        public const string DEFAULT_SERVICE_NAME = nameof(StringMessageConverter);

        private readonly Encoding defaultCharset;

        public StringMessageConverter()
            : this(Encoding.UTF8)
        {
        }

        public StringMessageConverter(Encoding defaultCharset)
        : base(new MimeType("text", "plain", defaultCharset))
        {
            if (defaultCharset == null)
            {
                throw new ArgumentNullException(nameof(defaultCharset));
            }

            this.defaultCharset = defaultCharset;
        }

        public override string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        protected override bool Supports(Type clazz)
        {
            return typeof(string) == clazz;
        }

        protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
        {
            var charset = GetContentTypeCharset(GetMimeType(message.Headers));
            var payload = message.Payload;

            return payload is string ? payload : new string(charset.GetChars((byte[])(object)payload));
        }

        protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
        {
            if (typeof(byte[]) == SerializedPayloadClass)
            {
                var charset = GetContentTypeCharset(GetMimeType(headers));
                var payStr = (string)payload;
                payload = charset.GetBytes(payStr);
            }

            return payload;
        }

        private Encoding GetContentTypeCharset(MimeType mimeType)
        {
            if (mimeType != null && mimeType.Encoding != null)
            {
                return mimeType.Encoding;
            }
            else
            {
                return defaultCharset;
            }
        }
    }
}
