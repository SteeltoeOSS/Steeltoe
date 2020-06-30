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
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using System;

namespace Steeltoe.Stream.Converter
{
    public class ObjectStringMessageConverter : AbstractMessageConverter
    {
        public const string DEFAULT_SERVICE_NAME = nameof(ObjectStringMessageConverter);

        public ObjectStringMessageConverter()
        : base(new MimeType("text", "*", EncodingUtils.Utf8))
        {
            StrictContentTypeMatch = true;
        }

        public override string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        public override bool CanConvertFrom(IMessage message, Type targetClass)
        {
            // only supports the conversion to String
            return SupportsMimeType(message.Headers);
        }

        protected override bool Supports(Type clazz)
        {
            return true;
        }

        protected override bool SupportsMimeType(IMessageHeaders headers)
        {
            var mimeType = GetMimeType(headers);
            if (mimeType != null)
            {
                foreach (var current in SupportedMimeTypes)
                {
                    if (current.Type.Equals(mimeType.Type))
                    {
                        return true;
                    }
                }
            }

            return base.SupportsMimeType(headers);
        }

        protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
        {
            if (message.Payload != null)
            {
                if (message.Payload is byte[])
                {
                    if (typeof(byte[]).IsAssignableFrom(targetClass))
                    {
                        return message.Payload;
                    }
                    else
                    {
                        return EncodingUtils.Utf8.GetString((byte[])message.Payload);
                    }
                }
                else
                {
                    if (typeof(byte[]).IsAssignableFrom(targetClass))
                    {
                        return EncodingUtils.Utf8.GetBytes(message.Payload.ToString());
                    }
                    else
                    {
                        return message.Payload;
                    }
                }
            }

            return null;
        }

        protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
        {
            if (payload != null)
            {
                if (payload is byte[])
                {
                    return payload;
                }
                else
                {
                    return EncodingUtils.Utf8.GetBytes(payload.ToString());
                }
            }

            return null;
        }
    }
}
