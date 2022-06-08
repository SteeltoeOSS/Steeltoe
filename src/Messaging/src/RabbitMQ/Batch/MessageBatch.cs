// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Batch;

public struct MessageBatch
{
    public string Exchange { get; }

    public string RoutingKey { get; }

    public IMessage Message { get; }

    public MessageBatch(string exchange, string routingKey, IMessage message)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
        Message = message;
    }
}
