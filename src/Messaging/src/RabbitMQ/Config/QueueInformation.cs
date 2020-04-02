// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Steeltoe.Messaging.Rabbit.Config
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
