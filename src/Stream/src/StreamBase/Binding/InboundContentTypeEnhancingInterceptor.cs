// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Stream.Binder;

namespace Steeltoe.Stream.Binding;

internal sealed class InboundContentTypeEnhancingInterceptor : AbstractContentTypeInterceptor
{
    public InboundContentTypeEnhancingInterceptor(string contentType)
        : base(contentType)
    {
    }

    public override IMessage DoPreSend(IMessage message, IMessageChannel channel)
    {
        var messageHeaders = message.Headers as MessageHeaders;
        var contentType = MimeType;

        /*
         * NOTE: The below code for BINDER_ORIGINAL_CONTENT_TYPE is to support legacy
         * message format established in 1.x version of the Java Streams framework and should/will
         * no longer be supported in 3.x of Java Streams
         */

        if (message.Headers.ContainsKey(BinderHeaders.BinderOriginalContentType))
        {
            var ct = message.Headers.Get<object>(BinderHeaders.BinderOriginalContentType);
            switch (ct)
            {
                case string stringValue:
                    contentType = MimeType.ToMimeType(stringValue);
                    break;
                case MimeType mimeType:
                    contentType = mimeType;
                    break;
            }

            messageHeaders.RawHeaders.Remove(BinderHeaders.BinderOriginalContentType);
            if (messageHeaders.RawHeaders.ContainsKey(MessageHeaders.ContentType))
            {
                messageHeaders.RawHeaders.Remove(MessageHeaders.ContentType);
            }
        }

        if (!message.Headers.ContainsKey(MessageHeaders.ContentType))
        {
            messageHeaders.RawHeaders.Add(MessageHeaders.ContentType, contentType);
        }
        else if (message.Headers.TryGetValue(MessageHeaders.ContentType, out var header) && header is string stringHeader)
        {
            messageHeaders.RawHeaders[MessageHeaders.ContentType] = MimeType.ToMimeType(stringHeader);
        }

        return message;
    }
}
