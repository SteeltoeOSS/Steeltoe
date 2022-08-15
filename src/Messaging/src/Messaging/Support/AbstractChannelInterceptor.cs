// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Order;

namespace Steeltoe.Messaging.Support;

public abstract class AbstractChannelInterceptor : AbstractOrdered, IChannelInterceptor
{
    protected AbstractChannelInterceptor()
    {
    }

    protected AbstractChannelInterceptor(int order)
        : base(order)
    {
    }

    public virtual void AfterReceiveCompletion(IMessage message, IMessageChannel channel, Exception exception)
    {
    }

    public virtual void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception exception)
    {
    }

    public virtual IMessage PostReceive(IMessage message, IMessageChannel channel)
    {
        return message;
    }

    public virtual void PostSend(IMessage message, IMessageChannel channel, bool sent)
    {
    }

    public virtual bool PreReceive(IMessageChannel channel)
    {
        return true;
    }

    public virtual IMessage PreSend(IMessage message, IMessageChannel channel)
    {
        return message;
    }
}
