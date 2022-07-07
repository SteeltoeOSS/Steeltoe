// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Support;

public class Delivery
{
    public Delivery(string consumerTag, Envelope envelope, RC.IBasicProperties properties, byte[] body, string queue)
    {
        ConsumerTag = consumerTag;
        Envelope = envelope;
        Properties = properties;
        Body = body;
        Queue = queue;
    }

    public string ConsumerTag { get; }

    public Envelope Envelope { get; }

    public RC.IBasicProperties Properties { get; }

    public byte[] Body { get; }

    public string Queue { get; }
}
