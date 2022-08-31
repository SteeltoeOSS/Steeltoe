// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Listener;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Configuration;

public class RabbitListenerContainerTestFactory : IRabbitListenerContainerFactory<MessageListenerTestContainer>
{
    private static int _counter = 1;

    public Dictionary<string, MessageListenerTestContainer> ListenerContainers { get; } = new();

    public string ServiceName { get; set; }

    public RabbitListenerContainerTestFactory(string name = null)
    {
        ServiceName = name ?? $"RabbitListenerContainerTestFactory@{GetHashCode()}";
    }

    public List<MessageListenerTestContainer> GetListenerContainers()
    {
        return new List<MessageListenerTestContainer>(ListenerContainers.Values);
    }

    public MessageListenerTestContainer CreateListenerContainer(IRabbitListenerEndpoint endpoint)
    {
        var container = new MessageListenerTestContainer(endpoint);

        if (endpoint.Id == null && endpoint is AbstractRabbitListenerEndpoint)
        {
            int id = Interlocked.Increment(ref _counter);
            endpoint.Id = $"endpoint#{id}";
        }

        Assert.NotNull(endpoint.Id);
        ListenerContainers.Add(endpoint.Id, container);
        return container;
    }

    public MessageListenerTestContainer GetListenerContainer(string id)
    {
        ListenerContainers.TryGetValue(id, out MessageListenerTestContainer result);
        return result;
    }

    IMessageListenerContainer IRabbitListenerContainerFactory.CreateListenerContainer(IRabbitListenerEndpoint endpoint)
    {
        return CreateListenerContainer(endpoint);
    }
}
