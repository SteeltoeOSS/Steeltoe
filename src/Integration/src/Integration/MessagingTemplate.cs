// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;

namespace Steeltoe.Integration;

public class MessagingTemplate : MessageChannelTemplate
{
    public IMessageChannel DefaultDestination
    {
        // Default Receive and Send are kept the same
        get => DefaultReceiveDestination;
        set => DefaultSendDestination = DefaultReceiveDestination = value;
    }

    public override IMessageChannel DefaultReceiveDestination
    {
        get => base.DefaultReceiveDestination;
        set => base.DefaultReceiveDestination = base.DefaultSendDestination = value;
    }

    public override IMessageChannel DefaultSendDestination
    {
        get => base.DefaultSendDestination;
        set => base.DefaultSendDestination = DefaultReceiveDestination = value;
    }

    public MessagingTemplate(IApplicationContext context, ILogger logger = null)
        : base(context, logger)
    {
    }

    public MessagingTemplate(IApplicationContext context, IMessageChannel defaultChannel, ILogger logger = null)
        : base(context, logger)
    {
        DefaultSendDestination = DefaultReceiveDestination = defaultChannel;
    }

    public object ReceiveAndConvert(IMessageChannel destination, int timeout)
    {
        IMessage message = DoReceive(destination, timeout);

        if (message != null)
        {
            return DoConvert<object>(message);
        }

        return Task.FromResult<object>(null);
    }

    public IMessage Receive(IMessageChannel destination, int timeout)
    {
        return DoReceive(destination, timeout);
    }
}
