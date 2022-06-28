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
        var contentType = _mimeType;

        /*
         * NOTE: The below code for BINDER_ORIGINAL_CONTENT_TYPE is to support legacy
         * message format established in 1.x version of the Java Streams framework and should/will
         * no longer be supported in 3.x of Java Streams
         */

        if (message.Headers.ContainsKey(BinderHeaders.BINDER_ORIGINAL_CONTENT_TYPE))
        {
            var ct = message.Headers.Get<object>(BinderHeaders.BINDER_ORIGINAL_CONTENT_TYPE);
            switch (ct)
            {
                case string strval:
                    contentType = MimeType.ToMimeType(strval);
                    break;
                case MimeType mimeval:
                    contentType = mimeval;
                    break;
            }

            messageHeaders.RawHeaders.Remove(BinderHeaders.BINDER_ORIGINAL_CONTENT_TYPE);
            if (messageHeaders.RawHeaders.ContainsKey(MessageHeaders.CONTENT_TYPE))
            {
                messageHeaders.RawHeaders.Remove(MessageHeaders.CONTENT_TYPE);
            }
        }

        if (!message.Headers.ContainsKey(MessageHeaders.CONTENT_TYPE))
        {
            messageHeaders.RawHeaders.Add(MessageHeaders.CONTENT_TYPE, contentType);
        }
        else if (message.Headers.TryGetValue(MessageHeaders.CONTENT_TYPE, out var header) && header is string strheader)
        {
            messageHeaders.RawHeaders[MessageHeaders.CONTENT_TYPE] = MimeType.ToMimeType(strheader);
        }

        return message;
    }
}
