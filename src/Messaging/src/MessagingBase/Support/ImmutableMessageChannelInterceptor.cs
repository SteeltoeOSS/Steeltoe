// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Support;

public class ImmutableMessageChannelInterceptor : AbstractChannelInterceptor
{
    public ImmutableMessageChannelInterceptor()
        : base(0)
    {
    }

    public ImmutableMessageChannelInterceptor(int order)
        : base(order)
    {
    }

    public override IMessage PreSend(IMessage message, IMessageChannel channel)
    {
        var accessor = MessageHeaderAccessor.GetAccessor(message, typeof(MessageHeaderAccessor));
        if (accessor != null && accessor.IsMutable)
        {
            accessor.SetImmutable();
        }

        return message;
    }
}
