// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Config
{
    public class QueueInformation
    {
        public QueueInformation(string name, uint messageCount, uint consumerCount)
        {
            Name = name;
            MessageCount = messageCount;
            ConsumerCount = consumerCount;
        }

        public string Name { get; }

        public uint MessageCount { get; }

        public uint ConsumerCount { get; }

        public override int GetHashCode()
        {
            var prime = 31;
            var result = 1;
            result = (prime * result) + (Name == null ? 0 : Name.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                return false;
            }

            var other = (QueueInformation)obj;
            if (Name == null)
            {
                if (other.Name != null)
                {
                    return false;
                }
            }
            else if (!Name.Equals(other.Name))
            {
                return false;
            }

            return true;
        }
    }
}
