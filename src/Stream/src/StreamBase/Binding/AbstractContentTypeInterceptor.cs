// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Stream.Binding;

internal abstract class AbstractContentTypeInterceptor : AbstractChannelInterceptor
{
    protected readonly MimeType _mimeType;

    protected AbstractContentTypeInterceptor(string contentType)
    {
        _mimeType = MimeTypeUtils.ParseMimeType(contentType);
    }

    public override IMessage PreSend(IMessage message, IMessageChannel channel)
    {
        return message is ErrorMessage ? message : DoPreSend(message, channel);
    }

    public abstract IMessage DoPreSend(IMessage message, IMessageChannel channel);
}
