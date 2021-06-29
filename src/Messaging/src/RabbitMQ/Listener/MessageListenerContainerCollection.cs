// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Listener
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
