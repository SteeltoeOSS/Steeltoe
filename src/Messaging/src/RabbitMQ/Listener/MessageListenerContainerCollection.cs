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

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class MessageListenerContainerCollection : IMessageListenerContainerCollection
    {
        private List<IMessageListenerContainer> _containers = new List<IMessageListenerContainer>();

        public MessageListenerContainerCollection(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException(nameof(groupName));
            }

            ServiceName = groupName;
        }

        public string ServiceName { get; set; }

        public string GroupName => ServiceName;

        public IList<IMessageListenerContainer> Containers
        {
            get
            {
                return new List<IMessageListenerContainer>(_containers);
            }
        }

        internal void AddContainer(IMessageListenerContainer messageListenerContainer)
        {
            _containers.Add(messageListenerContainer);
        }

        internal void RemoveContainer(IMessageListenerContainer messageListenerContainer)
        {
            _containers.Remove(messageListenerContainer);
        }
    }
}
