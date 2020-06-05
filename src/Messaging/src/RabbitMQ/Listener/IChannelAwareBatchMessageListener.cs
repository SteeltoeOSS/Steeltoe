// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using Steeltoe.Messaging.Rabbit.Data;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public interface IChannelAwareBatchMessageListener : IChannelAwareMessageListener
    {
        new void OnMessage(Message message, IModel channel)
        {
            throw new InvalidOperationException("Should never be called by the container");
        }

        new void OnMessageBatch(List<Message> messages, IModel channel);
    }
}
