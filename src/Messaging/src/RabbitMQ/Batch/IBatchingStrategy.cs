// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Data;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Batch
{
    public interface IBatchingStrategy
    {
        MessageBatch AddToBatch(string exchange, string routingKey, Message message);

        DateTime NextRelease();

        ICollection<MessageBatch> ReleaseBatches();

        bool CanDebatch(MessageProperties properties);

        void DeBatch(Message message, Action<Message> fragmentConsumer);
    }
}
