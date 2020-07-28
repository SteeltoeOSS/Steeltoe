// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Listener.Exceptions;
using System;

namespace Steeltoe.Messaging.Rabbit.Retry
{
    public class RejectAndDontRequeueRecoverer : IMessageRecoverer
    {
        public void Recover(IMessage message, Exception exception)
        {
            throw new ListenerExecutionFailedException("Retry Policy Exhausted", new RabbitRejectAndDontRequeueException(exception), message);
        }
    }
}
