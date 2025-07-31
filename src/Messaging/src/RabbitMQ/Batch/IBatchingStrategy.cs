// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Batch;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IBatchingStrategy
{
    MessageBatch? AddToBatch(string exchange, string routingKey, IMessage message);

    DateTime? NextRelease();

    ICollection<MessageBatch> ReleaseBatches();

    bool CanDebatch(IMessageHeaders properties);

    void DeBatch(IMessage message, Action<IMessage> fragmentConsumer);
}