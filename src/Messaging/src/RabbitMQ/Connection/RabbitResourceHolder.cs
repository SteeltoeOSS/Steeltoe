// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            else
            {
                tags = new List<ulong>();
                tags.Add(deliveryTag);
                _deliveryTags.Add(channel, tags);
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
