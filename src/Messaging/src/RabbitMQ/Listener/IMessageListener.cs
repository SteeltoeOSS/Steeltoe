// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Data;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public interface IMessageListener
    {
        AcknowledgeMode ContainerAckMode { get; set; }

        void OnMessage(Message message);

        void OnMessageBatch(List<Message> messages);
    }
}
