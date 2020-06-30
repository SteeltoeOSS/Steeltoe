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
using Steeltoe.Common.Transaction;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Support;
using System;
using System.Collections.Generic;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class RabbitResourceHolder : ResourceHolderSupport
    {
        private readonly List<IConnection> _connections = new List<IConnection>();

        private readonly List<R.IModel> _channels = new List<R.IModel>();

        private readonly Dictionary<IConnection, List<R.IModel>> _channelsPerConnection = new Dictionary<IConnection, List<R.IModel>>();

        private readonly Dictionary<R.IModel, List<ulong>> _deliveryTags = new Dictionary<R.IModel, List<ulong>>();

        private readonly ILogger _logger;

        public RabbitResourceHolder(ILogger logger = null)
        {
            _logger = logger;
            ReleaseAfterCompletion = true;
        }

        public RabbitResourceHolder(R.IModel channel, bool releaseAfterCompletion, ILogger logger = null)
        {
            AddChannel(channel);
            ReleaseAfterCompletion = releaseAfterCompletion;
            _logger = logger;
        }

        public bool ReleaseAfterCompletion { get; }

        public bool RequeueOnRollback { get; set; } = true;

        public void AddConnection(IConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (!_connections.Contains(connection))
            {
                _connections.Add(connection);
            }
        }

        public void AddChannel(R.IModel channel)
        {
            AddChannel(channel, null);
        }

        public void AddChannel(R.IModel channel, IConnection connection)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            if (!_channels.Contains(channel))
            {
                _channels.Add(channel);
                if (connection != null)
                {
                    if (!_channelsPerConnection.TryGetValue(connection, out var channelsForConnection))
                    {
                        channelsForConnection = new List<R.IModel>();
                        _channelsPerConnection[connection] = channelsForConnection;
                    }

                    channelsForConnection.Add(channel);
                }
            }
        }

        public bool ContainsChannel(R.IModel channel)
        {
            return _channels.Contains(channel);
        }

        public IConnection GetConnection()
        {
            return _connections.Count > 0 ? _connections[0] : null;
        }

        public R.IModel GetChannel()
        {
            return _channels.Count > 0 ? _channels[0] : null;
        }

        public void CommitAll()
        {
            try
            {
                foreach (var channel in _channels)
                {
                    if (_deliveryTags.TryGetValue(channel, out var tags))
                    {
                        foreach (var deliveryTag in tags)
                        {
                            channel.BasicAck(deliveryTag, false);
                        }
                    }

                    channel.TxCommit();
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "failed to commit RabbitMQ transaction");
                throw new RabbitException("failed to commit RabbitMQ transaction", e);
            }
        }

        public void CloseAll()
        {
            foreach (var channel in _channels)
            {
                try
                {
                    if (channel != ConsumerChannelRegistry.GetConsumerChannel())
                    {
                        channel.Close();
                    }
                    else
                    {
                        _logger?.LogDebug("Skipping close of consumer channel: {channel} ", channel);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Could not close synchronized Rabbit Channel after transaction");
                }
            }

            foreach (var con in _connections)
            {
                RabbitUtils.CloseConnection(con);
            }

            _connections.Clear();
            _channels.Clear();
            _channelsPerConnection.Clear();
        }

        public void AddDeliveryTag(R.IModel channel, ulong deliveryTag)
        {
            if (_deliveryTags.TryGetValue(channel, out var tags))
            {
                tags.Add(deliveryTag);
            }
        }

        public void RollbackAll()
        {
            foreach (var channel in _channels)
            {
                _logger?.LogDebug("Rolling back messages to channel: {channel}", channel);

                RabbitUtils.RollbackIfNecessary(channel);
                if (_deliveryTags.TryGetValue(channel, out var tags))
                {
                    foreach (var deliveryTag in tags)
                    {
                        try
                        {
                            channel.BasicReject(deliveryTag, RequeueOnRollback);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error Rolling back messages to {channel} ", channel);
                            throw RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
                        }
                    }

                    // Need to commit the reject (=nack)
                    RabbitUtils.CommitIfNecessary(channel);
                }
            }
        }
    }
}
