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

using Steeltoe.Messaging.Rabbit.Listener;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class RabbitListenerContainerTestFactory : IRabbitListenerContainerFactory<MessageListenerTestContainer>
    {
        private static int counter = 1;

        public RabbitListenerContainerTestFactory(string name = null)
        {
            if (name != null)
            {
                ServiceName = name;
            }
            else
            {
                ServiceName = "RabbitListenerContainerTestFactory@" + GetHashCode();
            }
        }

        public Dictionary<string, MessageListenerTestContainer> ListenerContainers { get; } = new Dictionary<string, MessageListenerTestContainer>();

        public List<MessageListenerTestContainer> GetListenerContainers()
        {
            return new List<MessageListenerTestContainer>(ListenerContainers.Values);
        }

        public string ServiceName { get; set; }

        public MessageListenerTestContainer CreateListenerContainer(IRabbitListenerEndpoint endpoint)
        {
            MessageListenerTestContainer container = new MessageListenerTestContainer(endpoint);
            if (endpoint.Id == null && endpoint is AbstractRabbitListenerEndpoint)
            {
                var id = Interlocked.Increment(ref counter);
                endpoint.Id = "endpoint#" + id;
            }

            Assert.NotNull(endpoint.Id);
            ListenerContainers.Add(endpoint.Id, container);
            return container;
        }

        public MessageListenerTestContainer GetListenerContainer(string id)
        {
            ListenerContainers.TryGetValue(id, out var result);
            return result;
        }

        IMessageListenerContainer IRabbitListenerContainerFactory.CreateListenerContainer(IRabbitListenerEndpoint endpoint)
        {
            return CreateListenerContainer(endpoint);
        }
    }
}
