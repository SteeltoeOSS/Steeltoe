// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Transaction;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class RabbitResourceHolder : ResourceHolderSupport
{
    private readonly List<IConnection> _connections = new();

    private readonly List<RC.IModel> _channels = new();

    private readonly Dictionary<IConnection, List<RC.IModel>> _channelsPerConnection = new();

    private readonly Dictionary<RC.IModel, List<ulong>> _deliveryTags = new();

    private readonly ILogger _logger;

    public bool ReleaseAfterCompletion { get; }

    public bool RequeueOnRollback { get; set; } = true;
    public RabbitResourceHolder()
        : this(null, true, new LoggerFactory())
    {
       
    }

    public RabbitResourceHolder(ILoggerFactory loggerFactory)
        :this(null, true, loggerFactory)
    {
    
    }
    public RabbitResourceHolder(RC.IModel channel, bool releaseAfterCompletion)
        : this (channel, releaseAfterCompletion, new LoggerFactory())
    {

    }
    public RabbitResourceHolder(RC.IModel channel, bool releaseAfterCompletion, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RabbitResourceHolder>();
        ReleaseAfterCompletion = releaseAfterCompletion;
        if (channel != null)
        {
            AddChannel(channel);
        }
    }

    public void AddConnection(IConnection connection)
    {
        ArgumentGuard.NotNull(connection);

        if (!_connections.Contains(connection))
        {
            _connections.Add(connection);
        }
    }

    public void AddChannel(RC.IModel channel)
    {
        AddChannel(channel, null);
    }

    public void AddChannel(RC.IModel channel, IConnection connection)
    {
        ArgumentGuard.NotNull(channel);

        if (!_channels.Contains(channel))
        {
            _channels.Add(channel);

            if (connection != null)
            {
                if (!_channelsPerConnection.TryGetValue(connection, out List<RC.IModel> channelsForConnection))
                {
                    channelsForConnection = new List<RC.IModel>();
                    _channelsPerConnection[connection] = channelsForConnection;
                }

                channelsForConnection.Add(channel);
            }
        }
    }

    public bool ContainsChannel(RC.IModel channel)
    {
        return _channels.Contains(channel);
    }

    public IConnection GetConnection()
    {
        return _connections.Count > 0 ? _connections[0] : null;
    }

    public RC.IModel GetChannel()
    {
        return _channels.Count > 0 ? _channels[0] : null;
    }

    public void CommitAll()
    {
        try
        {
            foreach (RC.IModel channel in _channels)
            {
                if (_deliveryTags.TryGetValue(channel, out List<ulong> tags))
                {
                    foreach (ulong deliveryTag in tags)
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
        foreach (RC.IModel channel in _channels)
        {
            try
            {
                if (channel != ConsumerChannelRegistry.GetConsumerChannel())
                {
                    channel.Close();
                }
                else
                {
                    _logger?.LogDebug("Skipping close of consumer channel: {channel}", channel);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Could not close synchronized Rabbit Channel after transaction");
            }
        }

        foreach (IConnection con in _connections)
        {
            RabbitUtils.CloseConnection(con);
        }

        _connections.Clear();
        _channels.Clear();
        _channelsPerConnection.Clear();
    }

    public void AddDeliveryTag(RC.IModel channel, ulong deliveryTag)
    {
        if (_deliveryTags.TryGetValue(channel, out List<ulong> tags))
        {
            tags.Add(deliveryTag);
        }
        else
        {
            tags = new List<ulong>
            {
                deliveryTag
            };

            _deliveryTags.Add(channel, tags);
        }
    }

    public void RollbackAll()
    {
        foreach (RC.IModel channel in _channels)
        {
            _logger?.LogDebug("Rolling back messages to channel: {channel}", channel);

            RabbitUtils.RollbackIfNecessary(channel);

            if (_deliveryTags.TryGetValue(channel, out List<ulong> tags))
            {
                foreach (ulong deliveryTag in tags)
                {
                    try
                    {
                        channel.BasicReject(deliveryTag, RequeueOnRollback);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error Rolling back messages to {channel}", channel);
                        throw RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
                    }
                }

                // Need to commit the reject (=nack)
                RabbitUtils.CommitIfNecessary(channel);
            }
        }
    }
}
