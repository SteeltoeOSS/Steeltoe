// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Core
{
    public class Envelope
    {
        public Envelope(ulong deliveryTag, bool redeliver, string exchange, string routingKey)
        {
            DeliveryTag = deliveryTag;
            Redeliver = redeliver;
            Exchange = exchange;
            RoutingKey = routingKey;
        }

        public ulong DeliveryTag { get; }

        public bool Redeliver { get; }

        public string Exchange { get; }

        public string RoutingKey { get; }
    }
}
