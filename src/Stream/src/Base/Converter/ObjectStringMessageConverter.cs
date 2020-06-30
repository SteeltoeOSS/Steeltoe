// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
