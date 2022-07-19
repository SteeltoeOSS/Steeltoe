// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Stream.Binder;
using System;

namespace Steeltoe.Stream.Binding;

internal sealed class OutboundContentTypeConvertingInterceptor : AbstractContentTypeInterceptor
{
    private readonly IMessageConverter _messageConverter;

    public OutboundContentTypeConvertingInterceptor(string contentType, IMessageConverter messageConverter)
        : base(contentType)
    {
        _messageConverter = messageConverter;
    }

    public override IMessage DoPreSend(IMessage message, IMessageChannel channel)
    {
        // If handler is a function, FunctionInvoker will already perform message
        // conversion.
        // In fact in the future we should consider propagating knowledge of the
        // default content type
        // to MessageConverters instead of interceptors
        if (message.Payload is byte[] && message.Headers.ContainsKey(MessageHeaders.ContentType))
        {
            return message;
        }

        // ===== 1.3 backward compatibility code part-1 ===
        string oct = null;
        if (message.Headers.ContainsKey(MessageHeaders.ContentType))
        {
            oct = message.Headers.Get<MimeType>(MessageHeaders.ContentType).ToString();
        }

        var ct = oct;
        if (message.Payload is string)
        {
            ct = MimeTypeUtils.ApplicationJsonValue.Equals(oct)
                ? MimeTypeUtils.ApplicationJsonValue
                : MimeTypeUtils.TextPlainValue;
        }

        // ===== END 1.3 backward compatibility code part-1 ===
        if (!message.Headers.ContainsKey(MessageHeaders.ContentType))
        {
            var messageHeaders = message.Headers as MessageHeaders;
            messageHeaders.RawHeaders[MessageHeaders.ContentType] = MimeType;
        }

        var result = message.Payload is byte[] ? message : _messageConverter.ToMessage(message.Payload, message.Headers);

        if (result == null)
        {
            throw new InvalidOperationException($"Failed to convert message: '{message}' to outbound message.");
        }

        // ===== 1.3 backward compatibility code part-2 ===
        if (ct != null && !ct.Equals(oct) && oct != null)
        {
            var messageHeaders = result.Headers as MessageHeaders;
            messageHeaders.RawHeaders[MessageHeaders.ContentType] = MimeType.ToMimeType(ct);
            messageHeaders.RawHeaders[BinderHeaders.BinderOriginalContentType] = MimeType.ToMimeType(oct);
        }

        // ===== END 1.3 backward compatibility code part-2 ===
        return result;
    }
}
