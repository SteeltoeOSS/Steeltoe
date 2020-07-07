// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public interface IChannelAwareMessageListener : IMessageListener
    {
        void OnMessage(IMessage message, IModel channel);

        void OnMessageBatch(List<IMessage> messages, IModel channel);
    }
}
