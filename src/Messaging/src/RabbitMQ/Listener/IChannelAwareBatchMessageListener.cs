// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener
{
    public interface IChannelAwareBatchMessageListener : IChannelAwareMessageListener
    {
        new void OnMessage(IMessage message, RC.IModel channel)
        {
            throw new InvalidOperationException("Should never be called by the container");
        }

        new void OnMessageBatch(List<IMessage> messages, RC.IModel channel);
    }
}
