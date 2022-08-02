// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class MessageListenerContainerCollection : IMessageListenerContainerCollection
{
    private readonly List<IMessageListenerContainer> _containers = new();

    public string ServiceName { get; set; }

    public string GroupName => ServiceName;

    public IList<IMessageListenerContainer> Containers => new List<IMessageListenerContainer>(_containers);

    public MessageListenerContainerCollection(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            throw new ArgumentException(nameof(groupName));
        }

        ServiceName = groupName;
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
