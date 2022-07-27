// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Listener;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Config;

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
        var container = new MessageListenerTestContainer(endpoint);
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