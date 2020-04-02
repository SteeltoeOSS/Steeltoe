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

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Threading;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public static class ConsumerChannelRegistry
    {
        private static readonly AsyncLocal<ChannelHolder> _consumerChannel = new AsyncLocal<ChannelHolder>();

        public static void RegisterConsumerChannel(IModel channel, IConnectionFactory connectionFactory, ILogger logger = null)
        {
            logger?.LogDebug("Registering consumer channel" + channel + " from factory " + connectionFactory);
            _consumerChannel.Value = new ChannelHolder(channel, connectionFactory);
        }

        public static void UnRegisterConsumerChannel(ILogger logger = null)
        {
            logger?.LogDebug("Unregistering consumer channel" + _consumerChannel.Value);
            _consumerChannel.Value = null;
        }

        public static IModel GetConsumerChannel()
        {
            var channelHolder = _consumerChannel.Value;
            IModel channel = null;
            if (channelHolder != null)
            {
                channel = channelHolder.Channel;
            }

            return channel;
        }

        public static IModel GetConsumerChannel(IConnectionFactory connectionFactory)
        {
            var channelHolder = _consumerChannel.Value;
            IModel channel = null;
            if (channelHolder != null && channelHolder.ConnectionFactory == connectionFactory)
            {
                channel = channelHolder.Channel;
            }

            return channel;
        }

        private class ChannelHolder
        {
            public IModel Channel { get; }

            public IConnectionFactory ConnectionFactory { get; }

            public ChannelHolder(IModel channel, IConnectionFactory connectionFactory)
            {
                Channel = channel;
                ConnectionFactory = connectionFactory;
            }
        }
    }
}
