// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public interface IChannelAwareMessageListener : IMessageListener
{
    void OnMessage(IMessage message, RC.IModel channel);

    void OnMessageBatch(List<IMessage> messages, RC.IModel channel);
}
