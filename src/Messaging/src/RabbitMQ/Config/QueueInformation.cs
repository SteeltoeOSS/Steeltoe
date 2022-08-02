// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class QueueInformation
{
    public string Name { get; }

    public uint MessageCount { get; }

    public uint ConsumerCount { get; }

    public QueueInformation(string name, uint messageCount, uint consumerCount)
    {
        Name = name;
        MessageCount = messageCount;
        ConsumerCount = consumerCount;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, MessageCount, ConsumerCount);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not QueueInformation other || GetType() != obj.GetType())
        {
            return false;
        }

        return Name == other.Name;
    }
}
