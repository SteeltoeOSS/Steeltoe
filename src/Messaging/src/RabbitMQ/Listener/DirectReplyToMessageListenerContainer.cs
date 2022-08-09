// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class DirectReplyToMessageListenerContainer : DirectMessageListenerContainer
{
    private const int DefaultIdle = 60000;
    internal readonly ConcurrentDictionary<RC.IModel, SimpleConsumer> InUseConsumerChannels = new();
    internal readonly ConcurrentDictionary<SimpleConsumer, long> WhenUsed = new();
    private int _consumerCount;

    public override int ConsumersPerQueue
    {
#pragma warning disable S4275 // Getters and setters should access the expected fields
        get
#pragma warning restore S4275 // Getters and setters should access the expected fields
        {
            return base.ConsumersPerQueue;
        }

        set
        {
            throw new NotSupportedException();
        }
    }

    public override IMessageListener MessageListener
    {
        get => base.MessageListener;

        set => base.MessageListener = new ChannelAwareMessageListener(this, value);
    }

    public DirectReplyToMessageListenerContainer(string name = null, ILoggerFactory loggerFactory = null)
        : this(null, null, name, loggerFactory)
    {
    }

    public DirectReplyToMessageListenerContainer(IApplicationContext applicationContext, string name = null, ILoggerFactory loggerFactory = null)
        : this(applicationContext, null, name, loggerFactory)
    {
    }

    public DirectReplyToMessageListenerContainer(IApplicationContext applicationContext, IConnectionFactory connectionFactory, string name = null,
        ILoggerFactory loggerFactory = null)
        : base(applicationContext, connectionFactory, name, loggerFactory)
    {
        base.SetQueueNames(Address.AmqRabbitMQReplyTo);
        AcknowledgeMode = AcknowledgeMode.None;
        base.ConsumersPerQueue = 0;
        IdleEventInterval = DefaultIdle;
    }

    public override void SetQueueNames(params string[] queueNames)
    {
        throw new NotSupportedException();
    }

    public override void AddQueueNames(params string[] queueNames)
    {
        throw new NotSupportedException();
    }

    public override bool RemoveQueueNames(params string[] queueNames)
    {
        throw new NotSupportedException();
    }

    public ChannelHolder GetChannelHolder()
    {
        lock (ConsumersMonitor)
        {
            ChannelHolder channelHolder = null;

            while (channelHolder == null)
            {
                if (!IsRunning)
                {
                    throw new InvalidOperationException("Direct reply-to container is not running");
                }

                foreach (SimpleConsumer consumer in Consumers)
                {
                    RC.IModel candidate = consumer.Model;

                    if (candidate.IsOpen && InUseConsumerChannels.TryAdd(candidate, consumer))
                    {
                        channelHolder = new ChannelHolder(candidate, consumer.IncrementAndGetEpoch());
                        WhenUsed[consumer] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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
        lock (ConsumersMonitor)
        {
            InUseConsumerChannels.TryGetValue(channelHolder.Channel, out SimpleConsumer consumer);

            if (consumer != null && consumer.Epoch == channelHolder.ConsumerEpoch)
            {
                InUseConsumerChannels.Remove(channelHolder.Channel, out _);

                if (cancelConsumer)
                {
                    if (message == null)
                    {
                        throw new InvalidOperationException("A 'message' is required when 'cancelConsumer' is 'true'.");
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
        long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        lock (ConsumersMonitor)
        {
            long reduce = 0;

            foreach (SimpleConsumer c in Consumers)
            {
                if (WhenUsed.TryGetValue(c, out long howLong) && !InUseConsumerChannels.Values.Contains(c) && howLong < now - IdleEventInterval)
                {
                    reduce++;
                }
            }

            if (reduce > 0)
            {
                Logger?.LogDebug("Reducing idle consumes by {reduce}", reduce);
                _consumerCount = (int)Math.Max(0, _consumerCount - reduce);
                base.ConsumersPerQueue = _consumerCount;
            }
        }
    }

    protected override int FindIdleConsumer()
    {
        for (int i = 0; i < Consumers.Count; i++)
        {
            if (!InUseConsumerChannels.Values.Contains(Consumers[i]))
            {
                return i;
            }
        }

        return -1;
    }

    protected override void ConsumerRemoved(SimpleConsumer consumer)
    {
        InUseConsumerChannels.Remove(consumer.Model, out _);
        WhenUsed.Remove(consumer, out _);
    }

    public class ChannelHolder
    {
        public RC.IModel Channel { get; }

        public int ConsumerEpoch { get; }

        public ChannelHolder(RC.IModel channel, int consumerEpoch)
        {
            Channel = channel;
            ConsumerEpoch = consumerEpoch;
        }

        public override string ToString()
        {
            return $"ChannelHolder [channel={Channel}, consumerEpoch={ConsumerEpoch}]";
        }
    }

    private sealed class ChannelAwareMessageListener : IChannelAwareMessageListener
    {
        private readonly DirectReplyToMessageListenerContainer _container;

        private readonly IMessageListener _listener;

        public AcknowledgeMode ContainerAckMode
        {
            get => AcknowledgeMode.None;

            set
            {
                // Do nothing
            }
        }

        public ChannelAwareMessageListener(DirectReplyToMessageListenerContainer container, IMessageListener listener)
        {
            _container = container;
            _listener = listener;
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
                    _container.InUseConsumerChannels.Remove(channel, out _);
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
                    _container.InUseConsumerChannels.Remove(channel, out _);
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
                    _container.InUseConsumerChannels.Remove(channel, out _);
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
                    _container.InUseConsumerChannels.Remove(channel, out _);
                }
            }
        }

        public void OnMessageBatch(List<IMessage> messages)
        {
            throw new InvalidOperationException("Should never be called for a ChannelAwareMessageListener");
        }
    }
}
