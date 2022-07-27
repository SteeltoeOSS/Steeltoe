// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Support;

public abstract class AbstractTaskSchedulerChannelInterceptor : AbstractChannelInterceptor, ITaskSchedulerChannelInterceptor
{
    public virtual void AfterMessageHandled(IMessage message, IMessageChannel channel, IMessageHandler handler, Exception exception)
    {
    }

    public virtual IMessage BeforeHandled(IMessage message, IMessageChannel channel, IMessageHandler handler)
    {
        return message;
    }
}