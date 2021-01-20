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
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitConverter = Steeltoe.Messaging.RabbitMQ.Support.Converter;

namespace Steeltoe.Integration.Rabbit.Inbound
{
    public class RabbitInboundChannelAdapter : MessageProducerSupportEndpoint // ,IOrderlyShutdownCapable
    {
        private static readonly AsyncLocal<IAttributeAccessor> _attributesHolder = new AsyncLocal<IAttributeAccessor>();
        private readonly ILogger _logger;

        public RabbitInboundChannelAdapter(IApplicationContext context, AbstractMessageListenerContainer listenerContainer, ILogger logger = null)
            : base(context, logger)
        {
            if (listenerContainer == null)
            {
                throw new ArgumentNullException(nameof(listenerContainer));
            }

            if (listenerContainer.MessageListener != null)
            {
                throw new ArgumentException("The listenerContainer provided to an RabbitMQ inbound Channel Adapter " +
                            "must not have a MessageListener configured since the adapter " +
                            "configure its own listener implementation.");
            }

            _logger = logger;
            listenerContainer.IsAutoStartup = true;
            MessageListenerContainer = listenerContainer;
            ErrorMessageStrategy = new RabbitMessageHeaderErrorMessageStrategy();
            var messageListener = new Listener(this, logger);
            MessageListenerContainer.MessageListener = messageListener;
            MessageListenerContainer.Initialize();
        }

        public ISmartMessageConverter MessageConverter { get; set; } = new RabbitConverter.SimpleMessageConverter();

        public RetryTemplate RetryTemplate { get; set; }

        public IRecoveryCallback RecoveryCallback { get; set; }

        public IBatchingStrategy BatchingStrategy { get; set; } = new SimpleBatchingStrategy(0, 0, 0L);

        public bool BindSourceMessage { get; set; }

        //Todo: Do we need this? 
        // private volatile RabbitHeaderMapper HeaderMapper = DefaultAmqpHeaderMapper.inboundMapper();
        private AbstractMessageListenerContainer MessageListenerContainer { get; }

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
            var attributes = _attributesHolder.Value;
            if (attributes == null)
            {
                return base.GetErrorMessageAttributes(message);
            }
            else
            {
                return attributes;
            }
        }

        // public int beforeShutdown()
        // {
        //    this.stop();
        //    return 0;
        // }

        // public int afterShutdown()
        // {
        //    return 0;
        // }
        private void SetAttributesIfNecessary(IMessage original, IMessage endMessage)
        {
            bool needHolder = ErrorChannel != null && RetryTemplate == null;
            bool needAttributes = needHolder || RetryTemplate != null;
            if (needHolder)
            {
                _attributesHolder.Value = ErrorMessageUtils.GetAttributeAccessor(null, null);
            }

            if (needAttributes)
            {
                var attributes = RetryTemplate != null ? RetrySynchronizationManager.GetContext() : _attributesHolder.Value;
                if (attributes != null)
                {
                    attributes.SetAttribute(ErrorMessageUtils.INPUT_MESSAGE_CONTEXT_KEY, endMessage);
                    attributes.SetAttribute(RabbitMessageHeaderErrorMessageStrategy.AMQP_RAW_MESSAGE, original);
                }
            }
        }

        protected class Listener : IChannelAwareMessageListener
        {
            private readonly RabbitInboundChannelAdapter _adapter;
            private readonly ILogger _logger;

            public Listener(RabbitInboundChannelAdapter adapter, ILogger logger)
            {
                _adapter = adapter;
                _logger = logger;
            }

            public AcknowledgeMode ContainerAckMode { get; set; }

            public void OnMessage(IMessage message, IModel channel)
            {
                var retryDisabled = _adapter.RetryTemplate == null;
                try
                {
                    if (retryDisabled)
                    {
                        CreateAndSend(message, channel);
                    }
                    else
                    {
                        var toSend = CreateMessage(message, channel);
                        _adapter.RetryTemplate.Execute(
                            context =>
                           {
                               try
                               {
                                   _logger.LogTrace($"RabbitInboundChannelAdapter::OnMessage Context: {context}");
                                   var deliveryAttempts = message.Headers.Get<AtomicInteger>(IntegrationMessageHeaderAccessor.DELIVERY_ATTEMPT);
                                   deliveryAttempts?.IncrementAndGet();
                                   _adapter.SetAttributesIfNecessary(message, toSend);
                                   _adapter.SendMessage(message);
                               }
                               catch(Exception ex)
                               {
                                   _logger.LogError(ex, ex.Message, context);
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
                        var errorMessage = _adapter.BuildErrorMessage(null, EndpointUtils.CreateErrorMessagePayload(message, channel, IsManualAck, e));
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
                        _attributesHolder.Value = null;
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

            private bool IsManualAck
            {
                get
                {
                    return _adapter.MessageListenerContainer.AcknowledgeMode == AcknowledgeMode.MANUAL;
                }
            }

            private void CreateAndSend(IMessage message, IModel channel)
            {
                var toSend = CreateMessage(message, channel);
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

                // Dictionary<string, object> headers = _adapter.HeaderMapper.toHeadersFromRequest(message.getMessageProperties());
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                if (IsManualAck)
                {
                    accessor.SetHeader(RabbitMessageHeaders.CHANNEL, channel);

                    // headers[AmqpHeaders.DELIVERY_TAG] = message.Headers.DeliveryTag();
                    // headers[AmqpHeaders.CHANNEL] = channel;
                }

                if (_adapter.RetryTemplate != null)
                {
                    accessor.SetHeader(IntegrationMessageHeaderAccessor.DELIVERY_ATTEMPT, new AtomicInteger());
                }

                if (_adapter.BindSourceMessage)
                {
                    accessor.SetHeader(IntegrationMessageHeaderAccessor.SOURCE_DATA, message);
                }

                var messagingMessage = _adapter.IntegrationServices.MessageBuilderFactory
                        .WithPayload(payload)
                        .CopyHeaders(accessor.MessageHeaders)
                        .Build();

                return messagingMessage;
            }
        }
    }
}
