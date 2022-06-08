// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Handler;

public abstract class AbstractReplyProducingMessageHandler : AbstractMessageProducingHandler
{
    protected AbstractReplyProducingMessageHandler(IApplicationContext context)
        : base(context)
    {
    }

    public bool RequiresReply { get; set; }

    protected override void HandleMessageInternal(IMessage message)
    {
        var result = HandleRequestMessage(message);
        if (result != null)
        {
            SendOutputs(result, message);
        }
        else if (RequiresReply)
        {
            throw new ReplyRequiredException(
                message,
                $"No reply produced by handler '{GetType().Name}', and its 'requiresReply' property is set to true.");
        }
    }

    protected abstract object HandleRequestMessage(IMessage requestMessage);
}
