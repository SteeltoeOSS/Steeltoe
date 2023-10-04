// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Retry;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using System;

namespace Steeltoe.Messaging.RabbitMQ.Retry;

public class RejectAndDontRequeueRecoverer : IMessageRecoverer, IRecoveryCallback
{
    public void Recover(IMessage message, Exception exception)
    {
        throw new ListenerExecutionFailedException("Retry Policy Exhausted", new RabbitRejectAndDontRequeueException(exception), message);
    }

    public object Recover(IRetryContext context)
    {
        var cause = context.LastException as ListenerExecutionFailedException;
        Recover(cause.FailedMessage, cause);
        return null;
    }
}