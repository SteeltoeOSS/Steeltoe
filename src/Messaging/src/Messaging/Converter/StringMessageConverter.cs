// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.Converter;

public class StringMessageConverter : AbstractMessageConverter
{
    public const string DefaultServiceName = nameof(StringMessageConverter);

    private readonly Encoding _defaultCharset;

    public override string ServiceName { get; set; } = DefaultServiceName;

    public StringMessageConverter()
        : this(Encoding.UTF8)
    {
    }

    public StringMessageConverter(Encoding defaultCharset)
        : base(new MimeType("text", "plain", defaultCharset))
    {
        _defaultCharset = defaultCharset ?? throw new ArgumentNullException(nameof(defaultCharset));
    }

    protected override bool Supports(Type clazz)
    {
        return typeof(string) == clazz;
    }

    protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
    {
        Encoding charset = GetContentTypeCharset(GetMimeType(message.Headers));
        object payload = message.Payload;

        return payload is string ? payload : new string(charset.GetChars((byte[])payload));
    }

    protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
    {
        if (typeof(byte[]) == SerializedPayloadClass)
        {
            Encoding charset = GetContentTypeCharset(GetMimeType(headers));
            string payStr = (string)payload;
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

        return _defaultCharset;
    }
}
