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

using RabbitMQ.Client;
using Steeltoe.Messaging.Rabbit.Core;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public class Delivery
    {
        public Delivery(string consumerTag, Envelope envelope, IBasicProperties properties, byte[] body, string queue)
        {
            ConsumerTag = consumerTag;
            Envelope = envelope;
            Properties = properties;
            Body = body;
            Queue = queue;
        }

        public string ConsumerTag { get; }

        public Envelope Envelope { get; }

        public IBasicProperties Properties { get; }

        public byte[] Body { get; }

        public string Queue { get; }
    }
}
