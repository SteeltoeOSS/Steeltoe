// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Threading;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public static class ConsumerChannelRegistry
{
    private static readonly AsyncLocal<ChannelHolder> ConsumerChannel = new ();

    public static void RegisterConsumerChannel(RC.IModel channel, IConnectionFactory connectionFactory, ILogger logger = null)
    {
        logger?.LogDebug("Registering consumer channel {channel} from factory {factory}", channel, connectionFactory);
        ConsumerChannel.Value = new ChannelHolder(channel, connectionFactory);
    }

    public static void UnRegisterConsumerChannel(ILogger logger = null)
    {
        logger?.LogDebug("Unregistering consumer channel {channel}", ConsumerChannel.Value);
        ConsumerChannel.Value = null;
    }

    public static RC.IModel GetConsumerChannel()
    {
        var channelHolder = ConsumerChannel.Value;
        RC.IModel channel = null;
        if (channelHolder != null)
        {
            channel = channelHolder.Channel;
        }

        return channel;
    }

    public static RC.IModel GetConsumerChannel(IConnectionFactory connectionFactory)
    {
        var channelHolder = ConsumerChannel.Value;
        RC.IModel channel = null;
        if (channelHolder != null && channelHolder.ConnectionFactory == connectionFactory)
        {
            channel = channelHolder.Channel;
        }

        return channel;
    }

    private sealed class ChannelHolder
    {
        public RC.IModel Channel { get; }

        public IConnectionFactory ConnectionFactory { get; }

        public ChannelHolder(RC.IModel channel, IConnectionFactory connectionFactory)
        {
            Channel = channel;
            ConnectionFactory = connectionFactory;
        }
    }
}
