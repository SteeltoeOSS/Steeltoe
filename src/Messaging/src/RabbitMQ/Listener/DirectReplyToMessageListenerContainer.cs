// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.RabbitMQ.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class DirectReplyToMessageListenerContainer : DirectMessageListenerContainer
{
    internal readonly ConcurrentDictionary<RC.IModel, SimpleConsumer> _inUseConsumerChannels = new ();
    internal readonly ConcurrentDictionary<SimpleConsumer, long> _whenUsed = new ();
    private const int DEFAULT_IDLE = 60000;
    private int _consumerCount;

    public DirectReplyToMessageListenerContainer(string name = null, ILoggerFactory loggerFactory = null)
        : this(null, null, name, loggerFactory)
    {
    }

    public DirectReplyToMessageListenerContainer(IApplicationContext applicationContext, string name = null, ILoggerFactory loggerFactory = null)
        : this(applicationContext, null, name, loggerFactory)
    {
    }

    public DirectReplyToMessageListenerContainer(IApplicationContext applicationContext, Connection.IConnectionFactory connectionFactory, string name = null, ILoggerFactory loggerFactory = null)
        : base(applicationContext, connectionFactory, name, loggerFactory)
    {
        base.SetQueueNames(Address.AMQ_RABBITMQ_REPLY_TO);
        AcknowledgeMode = AcknowledgeMode.NONE;
        base.ConsumersPerQueue = 0;
        IdleEventInterval = DEFAULT_IDLE;
    }

    public override int ConsumersPerQueue
    {
        get
        {
            return base.ConsumersPerQueue;
        }

        set
        {
            throw new NotSupportedException();
        }
    }

    public override long MonitorInterval
    {
        get
        {
            return base.MonitorInterval;
        }

        set
        {
            throw new NotSupportedException();
        }
    }

    public override void SetQueueNames(params string[] queueName)
    {
        throw new NotSupportedException();
    }

    public override void AddQueueNames(params string[] queueName)
    {
        throw new NotSupportedException();
    }

    public override bool RemoveQueueNames(params string[] queueName)
    {
        throw new NotSupportedException();
    }

    public override IMessageListener MessageListener
    {
        get
        {
            return base.MessageListener;
        }

        set
        {
            base.MessageListener = new ChannelAwareMessageListener(this, value);
        }
    }

    public ChannelHolder GetChannelHolder()
    {
        lock (_consumersMonitor)
        {
            ChannelHolder channelHolder = null;
            while (channelHolder == null)
            {
                if (!IsRunning)
                {
                    throw new InvalidOperationException("Direct reply-to container is not running");
                }

                foreach (var consumer in _consumers)
                {
                    var candidate = consumer.Model;
                    if (candidate.IsOpen && _inUseConsumerChannels.TryAdd(candidate, consumer))
                    {
                        channelHolder = new ChannelHolder(candidate, consumer.IncrementAndGetEpoch());
                        _whenUsed[consumer] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        break;
                    }
                }

                if (channelHolder == null)
                {
                    _consumerCount++;
                    base.ConsumersPerQueue = _consumerCount;
                }
            }

            return channelHolder;
        }
    }

    public void ReleaseConsumerFor(ChannelHolder channelHolder, bool cancelConsumer, string message)
    {
        lock (_consumersMonitor)
        {
            _inUseConsumerChannels.TryGetValue(channelHolder.Channel, out var consumer);
            if (consumer != null && consumer.Epoch == channelHolder.ConsumerEpoch)
            {
                _inUseConsumerChannels.Remove(channelHolder.Channel, out _);
                if (cancelConsumer)
                {
                    if (message == null)
                    {
                        throw new ArgumentNullException("A 'message' is required when 'cancelConsumer' is 'true'");
                    }

                    consumer.CancelConsumer($"Consumer {this} canceled due to {message}");
                }
            }
        }
    }

    internal void SetChannelAwareMessageListener(IChannelAwareMessageListener listener)
    {
        base.MessageListener = listener;
    }

    protected override void DoStart()
    {
        if (!IsRunning)
        {
            _consumerCount = 0;
            base.ConsumersPerQueue = 0;
            base.DoStart();
        }
    }

    protected override void ProcessMonitorTask()
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        lock (_consumersMonitor)
        {
            long reduce = 0;
            foreach (var c in _consumers)
            {
                if (_whenUsed.TryGetValue(c, out var howlong)
                    && !_inUseConsumerChannels.Values.Contains(c)
                    && howlong < now - IdleEventInterval)
                {
                    reduce++;
                }
            }

            if (reduce > 0)
            {
                _logger?.LogDebug("Reducing idle consumes by {reduce}", reduce);
                _consumerCount = (int)Math.Max(0, _consumerCount - reduce);
                base.ConsumersPerQueue = _consumerCount;
            }
        }
    }

    protected override int FindIdleConsumer()
    {
        for (var i = 0; i < _consumers.Count; i++)
        {
            if (!_inUseConsumerChannels.Values.Contains(_consumers[i]))
            {
                return i;
            }
        }

        return -1;
    }

    protected override void ConsumerRemoved(SimpleConsumer consumer)
    {
        _inUseConsumerChannels.Remove(consumer.Model, out _);
        _whenUsed.Remove(consumer, out _);
    }

    public class ChannelHolder
    {
        public ChannelHolder(RC.IModel channel, int consumerEpoch)
        {
            Channel = channel;
            ConsumerEpoch = consumerEpoch;
        }

        public RC.IModel Channel { get; }

        public int ConsumerEpoch { get; }

        public override string ToString()
        {
            return $"ChannelHolder [channel={Channel}, consumerEpoch={ConsumerEpoch}]";
        }
    }

    private sealed class ChannelAwareMessageListener : IChannelAwareMessageListener
    {
        private readonly DirectReplyToMessageListenerContainer _container;

        private readonly IMessageListener _listener;

        public ChannelAwareMessageListener(DirectReplyToMessageListenerContainer container, IMessageListener listener)
        {
            _container = container;
            _listener = listener;
        }

        public AcknowledgeMode ContainerAckMode
        {
            get
            {
                return AcknowledgeMode.NONE;
            }

            set
            {
                // Do nothing
            }
        }

        public void OnMessage(IMessage message, RC.IModel channel)
        {
            if (_listener is IChannelAwareMessageListener chanAwareListener)
            {
                try
                {
                    chanAwareListener.OnMessage(message, channel);
                }
                finally
                {
                    _container._inUseConsumerChannels.Remove(channel, out _);
                }
            }
            else
            {
                try
                {
                    _listener.OnMessage(message);
                }
                finally
                {
                    _container._inUseConsumerChannels.Remove(channel, out _);
                }
            }
        }

        public void OnMessage(IMessage message)
        {
            throw new InvalidOperationException("Should never be called for a ChannelAwareMessageListener");
        }

        public void OnMessageBatch(List<IMessage> messages, RC.IModel channel)
        {
            if (_listener is IChannelAwareMessageListener chanAwareListener)
            {
                try
                {
                    chanAwareListener.OnMessageBatch(messages, channel);
                }
                finally
                {
                    _container._inUseConsumerChannels.Remove(channel, out _);
                }
            }
            else
            {
                try
                {
                    _listener.OnMessageBatch(messages);
                }
                finally
                {
                    _container._inUseConsumerChannels.Remove(channel, out _);
                }
            }
        }

        public void OnMessageBatch(List<IMessage> messages)
        {
            throw new InvalidOperationException("Should never be called for a ChannelAwareMessageListener");
        }
    }
}
