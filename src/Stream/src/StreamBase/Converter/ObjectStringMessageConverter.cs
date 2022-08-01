// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;

namespace Steeltoe.Stream.Converter;

public class ObjectStringMessageConverter : AbstractMessageConverter
{
    public const string DefaultServiceName = nameof(ObjectStringMessageConverter);

    public ObjectStringMessageConverter()
        : base(new MimeType("text", "*", EncodingUtils.Utf8))
    {
        StrictContentTypeMatch = true;
    }

    public override string ServiceName { get; set; } = DefaultServiceName;

    // only supports the conversion to String
    public override bool CanConvertFrom(IMessage message, Type targetClass) => SupportsMimeType(message.Headers);

    protected override bool Supports(Type clazz) => true;

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
            return message.Payload switch
            {
                byte[] v => typeof(byte[]).IsAssignableFrom(targetClass) ? message.Payload : EncodingUtils.Utf8.GetString(v),
                _ => typeof(byte[]).IsAssignableFrom(targetClass) ? EncodingUtils.Utf8.GetBytes(message.Payload.ToString()) : message.Payload,
            };
        }

        return null;
    }

    protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
    {
        if (payload != null)
        {
            return payload switch
            {
                byte[] => payload,
                _ => EncodingUtils.Utf8.GetBytes(payload.ToString())
            };
        }

        return null;
    }
}
