// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Endpoint;
using Steeltoe.Integration.Rabbit.Support;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.Support;
using RabbitConverter = Steeltoe.Messaging.RabbitMQ.Support.Converter;

namespace Steeltoe.Integration.Rabbit.Inbound;

public class RabbitInboundChannelAdapter : MessageProducerSupportEndpoint
{
    private static readonly AsyncLocal<IAttributeAccessor> AttributesHolder = new();
    private readonly ILogger _logger;

    private AbstractMessageListenerContainer MessageListenerContainer { get; }

    public ISmartMessageConverter MessageConverter { get; set; } = new RabbitConverter.SimpleMessageConverter();

    public RetryTemplate RetryTemplate { get; set; }

    public IRecoveryCallback RecoveryCallback { get; set; }

    public IBatchingStrategy BatchingStrategy { get; set; } = new SimpleBatchingStrategy(0, 0, 0L);

    public bool BindSourceMessage { get; set; }

    public RabbitInboundChannelAdapter(IApplicationContext context, AbstractMessageListenerContainer listenerContainer, ILogger logger = null)
        : base(context, logger)
    {
        if (listenerContainer == null)
        {
            throw new ArgumentNullException(nameof(listenerContainer));
        }

        if (listenerContainer.MessageListener != null)
        {
            throw new ArgumentException("The listenerContainer provided to a RabbitMQ inbound Channel Adapter " +
                "must not have a MessageListener configured since the adapter " + "configures its own listener implementation.");
        }

        _logger = logger;
        listenerContainer.IsAutoStartup = true;
        MessageListenerContainer = listenerContainer;
        ErrorMessageStrategy = new RabbitMessageHeaderErrorMessageStrategy();
        var messageListener = new Listener(this, logger);
        MessageListenerContainer.MessageListener = messageListener;
        MessageListenerContainer.Initialize();
    }

    protected override Task DoStart()
    {
        return MessageListenerContainer.Start();
    }

    protected override Task DoStop()
    {
        return MessageListenerContainer.Stop();
    }

    protected override IAttributeAccessor GetErrorMessageAttributes(IMessage message)
    {
        IAttributeAccessor attributes = AttributesHolder.Value;

        if (attributes == null)
        {
            return base.GetErrorMessageAttributes(message);
        }

        return attributes;
    }

    private void SetAttributesIfNecessary(IMessage original, IMessage endMessage)
    {
        bool needHolder = ErrorChannel != null && RetryTemplate == null;
        bool needAttributes = needHolder || RetryTemplate != null;

        if (needHolder)
        {
            AttributesHolder.Value = ErrorMessageUtils.GetAttributeAccessor(null, null);
        }

        if (needAttributes)
        {
            IAttributeAccessor attributes = RetryTemplate != null ? RetrySynchronizationManager.GetContext() : AttributesHolder.Value;

            if (attributes != null)
            {
                attributes.SetAttribute(ErrorMessageUtils.InputMessageContextKey, endMessage);
                attributes.SetAttribute(RabbitMessageHeaderErrorMessageStrategy.AmqpRawMessage, original);
            }
        }
    }

    protected class Listener : IChannelAwareMessageListener
    {
        private readonly RabbitInboundChannelAdapter _adapter;
        private readonly ILogger _logger;

        private bool IsManualAck => _adapter.MessageListenerContainer.AcknowledgeMode == AcknowledgeMode.Manual;

        public AcknowledgeMode ContainerAckMode { get; set; }

        public Listener(RabbitInboundChannelAdapter adapter, ILogger logger)
        {
            _adapter = adapter;
            _logger = logger;
        }

        public void OnMessage(IMessage message, IModel channel)
        {
            bool retryDisabled = _adapter.RetryTemplate == null;

            try
            {
                if (retryDisabled)
                {
                    CreateAndSend(message, channel);
                }
                else
                {
                    IMessage toSend = CreateMessage(message, channel);

                    _adapter.RetryTemplate.Execute(context =>
                    {
                        try
                        {
                            _logger?.LogTrace($"RabbitInboundChannelAdapter::OnMessage Context: {context}");
                            var deliveryAttempts = message.Headers.Get<AtomicInteger>(IntegrationMessageHeaderAccessor.DeliveryAttempt);
                            deliveryAttempts?.IncrementAndGet();
                            _adapter.SetAttributesIfNecessary(message, toSend);
                            _adapter.SendMessage(message);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, ex.Message, context);
                            throw;
                        }
                    }, _adapter.RecoveryCallback);
                }
            }
            catch (MessageConversionException e)
            {
                if (_adapter.ErrorChannel != null)
                {
                    _adapter.SetAttributesIfNecessary(message, null);
                    ErrorMessage errorMessage = _adapter.BuildErrorMessage(null, EndpointUtils.CreateErrorMessagePayload(message, channel, IsManualAck, e));
                    _adapter.MessagingTemplate.Send(_adapter.ErrorChannel, errorMessage);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (retryDisabled)
                {
                    AttributesHolder.Value = null;
                }
            }
        }

        public void OnMessage(IMessage message)
        {
            throw new InvalidOperationException("Should never be called for a ChannelAwareMessageListener");
        }

        public void OnMessageBatch(List<IMessage> messages, IModel channel)
        {
            throw new NotSupportedException("This listener does not support message batches");
        }

        public void OnMessageBatch(List<IMessage> messages)
        {
            throw new NotSupportedException("This listener does not support message batches");
        }

        private void CreateAndSend(IMessage message, IModel channel)
        {
            IMessage toSend = CreateMessage(message, channel);
            _adapter.SetAttributesIfNecessary(message, toSend);
            _adapter.SendMessage(toSend);
        }

        private IMessage CreateMessage(IMessage message, IModel channel)
        {
            object payload;

            if (_adapter.BatchingStrategy.CanDebatch(message.Headers))
            {
                var payloads = new List<object>();
                _adapter.BatchingStrategy.DeBatch(message, fragment => payloads.Add(_adapter.MessageConverter.FromMessage(fragment, null)));
                payload = payloads;
            }
            else
            {
                payload = _adapter.MessageConverter.FromMessage(message, null);
            }

            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);

            if (IsManualAck)
            {
                accessor.SetHeader(RabbitMessageHeaders.DeliveryTag, message.Headers.DeliveryTag());
                accessor.SetHeader(RabbitMessageHeaders.Channel, channel);
            }

            if (_adapter.RetryTemplate != null)
            {
                accessor.SetHeader(IntegrationMessageHeaderAccessor.DeliveryAttempt, new AtomicInteger());
            }

            if (_adapter.BindSourceMessage)
            {
                accessor.SetHeader(IntegrationMessageHeaderAccessor.SourceData, message);
            }

            IMessage messagingMessage = _adapter.IntegrationServices.MessageBuilderFactory.WithPayload(payload).CopyHeaders(accessor.MessageHeaders).Build();

            return messagingMessage;
        }
    }
}
