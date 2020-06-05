// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Steeltoe.Common.Expression;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Listener;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Rabbit.Support.Converter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class RabbitTemplate : RabbitAccessor, IRabbitOperations, IMessageListener, IListenerContainerAware, IPublisherCallbackChannel.IListener
    {
        public const string DEFAULT_RABBIT_TEMPLATE_SERVICE_NAME = "rabbitTemplate";

        private const string RETURN_CORRELATION_KEY = "spring_request_return_correlation";
        private const string DEFAULT_EXCHANGE = "";
        private const string DEFAULT_ROUTING_KEY = "";
        private const int DEFAULT_REPLY_TIMEOUT = 5000;
        private const int DEFAULT_CONSUME_TIMEOUT = 10000;

        private readonly object _lock = new object();
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<IModel, RabbitTemplate> _publisherConfirmChannels = new ConcurrentDictionary<IModel, RabbitTemplate>();
        private readonly ConcurrentDictionary<string, PendingReply> _replyHolder = new ConcurrentDictionary<string, PendingReply>();
        private readonly Dictionary<Connection.IConnectionFactory, DirectReplyToMessageListenerContainer> _directReplyToContainers = new Dictionary<Connection.IConnectionFactory, DirectReplyToMessageListenerContainer>();
        private readonly AsyncLocal<IModel> _dedicatedChannels = new AsyncLocal<IModel>();
        private readonly IOptionsMonitor<RabbitOptions> _optionsMonitor;

        private int _activeTemplateCallbacks;
        private int _messageTagProvider;
        private int _containerInstance;
        private bool _isListener = false;
        private volatile bool _usingFastReplyTo;
        private volatile bool _evaluatedFastReplyTo;
        private bool? _confirmsOrReturnsCapable;
        private bool _publisherConfirms;
        private string _replyAddress;

        public RabbitTemplate(IOptionsMonitor<RabbitOptions> optionsMonitor, Connection.IConnectionFactory connectionFactory, Support.Converter.IMessageConverter messageConverter, ILogger logger = null)
            : base(connectionFactory)
        {
            _optionsMonitor = optionsMonitor;
            ConnectionFactory = connectionFactory;
            MessageConverter = messageConverter;
            _logger = logger;
            Configure(Options);
        }

        public RabbitTemplate(IOptionsMonitor<RabbitOptions> optionsMonitor, Connection.IConnectionFactory connectionFactory, ILogger logger = null)
            : base(connectionFactory)
        {
            _optionsMonitor = optionsMonitor;
            ConnectionFactory = connectionFactory;
            MessageConverter = new SimpleMessageConverter();
            _logger = logger;
            Configure(Options);
        }

        public RabbitTemplate(Connection.IConnectionFactory connectionFactory, ILogger logger = null)
            : base(connectionFactory)
        {
            ConnectionFactory = connectionFactory;
            MessageConverter = new SimpleMessageConverter();
            _logger = logger;
        }

        #region Properties

        public AcknowledgeMode ContainerAckMode { get; set; }

        public Support.Converter.IMessageConverter MessageConverter { get; set; }

        public string Exchange { get; set; } = DEFAULT_EXCHANGE;

        public string RoutingKey { get; set; } = DEFAULT_ROUTING_KEY;

        public string DefaultReceiveQueue { get; set; }

        public Encoding Encoding { get; set; } = EncodingUtils.Utf8;

        public string ReplyAddress
        {
            get
            {
                return _replyAddress;
            }

            set
            {
                _evaluatedFastReplyTo = false;
                _replyAddress = value;
            }
        }

        public int ReceiveTimeout { get; set; } = 0;

        public int ReplyTimeout { get; set; } = DEFAULT_REPLY_TIMEOUT;

        public IMessagePropertiesConverter MessagePropertiesConverter { get; set; } = new DefaultMessagePropertiesConverter();

        public IConfirmCallback ConfirmCallback { get; set; }

        public IReturnCallback ReturnCallback { get; set; }

        public bool Mandatory { get; set; }

        public IExpression MandatoryExpression { get; set; } = new ValueExpression<bool>(false);

        public string MandatoryExpressionString { get; set; }

        public IExpression SendConnectionFactorySelectorExpression { get; set; }

        public IExpression ReceiveConnectionFactorySelectorExpression { get; set; }

        public string CorrelationKey { get; set; }

        public IEvaluationContext EvaluationContext { get; set; } // TODO  = new StandardEvaluationContext();

        public IRetryOperation RetryTemplate { get; set; }

        public IRecoveryCallback RecoveryCallback { get; set; }

        // public void setBeanFactory(BeanFactory beanFactory) throws BeansException
        //   {
        // this.evaluationContext.setBeanResolver(new BeanFactoryResolver(beanFactory));
        // this.evaluationContext.addPropertyAccessor(new MapAccessor());
        // }
        public IList<IMessagePostProcessor> BeforePublishPostProcessors { get; internal set; }

        public IList<IMessagePostProcessor> AfterReceivePostProcessors { get; internal set; }

        public ICorrelationDataPostProcessor CorrelationDataPostProcessor { get; set; }

        public bool UseTemporaryReplyQueues { get; set; }

        public bool UseDirectReplyToContainer { get; set; } = true;

        public IExpression UserIdExpression { get; set; }

        public string UserIdExpressionString { get; set; }

        public string Name { get; set; } = DEFAULT_RABBIT_TEMPLATE_SERVICE_NAME;

        public bool UseCorrelationId { get; set; }

        public bool UsePublisherConnection { get; set; }

        public bool NoLocalReplyConsumer { get; set; }

        public IErrorHandler ReplyErrorHandler { get; set; }

        public bool IsRunning
        {
            get
            {
                lock (_directReplyToContainers)
                {
                    return _directReplyToContainers.Values
                            .Any((c) => c.IsRunning);
                }
            }
        }

        public string UUID { get; } = Guid.NewGuid().ToString();

        public bool IsConfirmListener => ConfirmCallback != null;

        public bool IsReturnListener => true;

        protected internal RabbitOptions Options
        {
            get
            {
                if (_optionsMonitor != null)
                {
                    return _optionsMonitor.CurrentValue;
                }

                return null;
            }
        }

        #endregion

        #region Public
        public void SetBeforePublishPostProcessors(params IMessagePostProcessor[] beforePublishPostProcessors)
        {
            if (beforePublishPostProcessors == null)
            {
                throw new ArgumentNullException(nameof(beforePublishPostProcessors));
            }

            Array.ForEach(beforePublishPostProcessors, (e) =>
            {
                if (e == null)
                {
                    throw new ArgumentException("'beforePublishPostProcessors' cannot have null elements");
                }
            });

            var newList = new List<IMessagePostProcessor>(beforePublishPostProcessors);
            MessagePostProcessorUtils.Sort(newList);
            BeforePublishPostProcessors = newList;
        }

        public void AddBeforePublishPostProcessors(params IMessagePostProcessor[] beforePublishPostProcessors)
        {
            if (beforePublishPostProcessors == null)
            {
                throw new ArgumentNullException(nameof(beforePublishPostProcessors));
            }

            var existing = BeforePublishPostProcessors;
            var newList = new List<IMessagePostProcessor>(beforePublishPostProcessors);
            if (existing != null)
            {
                newList.AddRange(existing);
            }

            MessagePostProcessorUtils.Sort(newList);
            BeforePublishPostProcessors = newList;
        }

        public bool RemoveBeforePublishPostProcessor(IMessagePostProcessor beforePublishPostProcessor)
        {
            if (beforePublishPostProcessor == null)
            {
                throw new ArgumentNullException(nameof(beforePublishPostProcessor));
            }

            var existing = BeforePublishPostProcessors;
            if (existing != null && existing.Contains(beforePublishPostProcessor))
            {
                var copy = new List<IMessagePostProcessor>(existing);
                var result = copy.Remove(beforePublishPostProcessor);
                BeforePublishPostProcessors = copy;
                return result;
            }

            return false;
        }

        public void SetAfterReceivePostProcessors(params IMessagePostProcessor[] afterReceivePostProcessors)
        {
            if (AfterReceivePostProcessors == null)
            {
                throw new ArgumentNullException(nameof(afterReceivePostProcessors));
            }

            Array.ForEach(afterReceivePostProcessors, (e) =>
            {
                if (e == null)
                {
                    throw new ArgumentException("'afterReceivePostProcessors' cannot have null elements");
                }
            });

            var newList = new List<IMessagePostProcessor>(afterReceivePostProcessors);
            MessagePostProcessorUtils.Sort(newList);
            AfterReceivePostProcessors = newList;
        }

        public void AddAfterReceivePostProcessors(params IMessagePostProcessor[] afterReceivePostProcessors)
        {
            if (afterReceivePostProcessors == null)
            {
                throw new ArgumentNullException(nameof(afterReceivePostProcessors));
            }

            var existing = AfterReceivePostProcessors;
            var newList = new List<IMessagePostProcessor>(afterReceivePostProcessors);
            if (existing != null)
            {
                newList.AddRange(existing);
            }

            MessagePostProcessorUtils.Sort(newList);
            AfterReceivePostProcessors = newList;
        }

        public bool RemoveAfterReceivePostProcessor(IMessagePostProcessor afterReceivePostProcessor)
        {
            if (afterReceivePostProcessor == null)
            {
                throw new ArgumentNullException(nameof(afterReceivePostProcessor));
            }

            var existing = AfterReceivePostProcessors;
            if (existing != null && existing.Contains(afterReceivePostProcessor))
            {
                var copy = new List<IMessagePostProcessor>(existing);
                var result = copy.Remove(afterReceivePostProcessor);
                BeforePublishPostProcessors = copy;
                return result;
            }

            return false;
        }

        public List<string> GetExpectedQueueNames()
        {
            _isListener = true;
            List<string> replyQueue = null;
            if (ReplyAddress == null || ReplyAddress == Address.AMQ_RABBITMQ_REPLY_TO)
            {
                throw new InvalidOperationException("A listener container must not be provided when using direct reply-to");
            }
            else
            {
                var address = new Address(ReplyAddress);
                if (address.ExchangeName == string.Empty)
                {
                    replyQueue = new List<string>() { address.RoutingKey };
                }
                else
                {
                    _logger?.LogInformation("Cannot verify reply queue because 'replyAddress' is not a simple queue name: " + ReplyAddress);
                }
            }

            return replyQueue;
        }

        public ICollection<CorrelationData> GetUnconfirmed(long age)
        {
            var unconfirmed = new HashSet<CorrelationData>();
            var cutoffTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - age;
            foreach (var channel in _publisherConfirmChannels.Keys)
            {
                if (channel is IPublisherCallbackChannel pubCallbackChan)
                {
                    var confirms = pubCallbackChan.Expire(this, cutoffTime);
                    foreach (var confirm in confirms)
                    {
                        unconfirmed.Add(confirm.CorrelationInfo);
                    }
                }
            }

            return unconfirmed.Count > 0 ? unconfirmed : null;
        }

        public int GetUnconfirmedCount()
        {
            return _publisherConfirmChannels.Keys
                    .Select((m) =>
                    {
                        if (m is IPublisherCallbackChannel pubCallbackChan)
                        {
                            return pubCallbackChan.GetPendingConfirmsCount(this);
                        }

                        return 0;
                    })
                    .Sum();
        }

        public async Task Start()
        {
            await DoStart();
        }

        public async Task Stop()
        {
            lock (_directReplyToContainers)
            {
                foreach (var c in _directReplyToContainers.Values)
                {
                    if (c.IsRunning)
                    {
                        c.Stop();
                    }
                }

                _directReplyToContainers.Clear();
            }

            await DoStop();
        }

        public async Task Destroy() => await Stop();

        public void Send(Message message)
        {
            Send(Exchange, RoutingKey, message);
        }

        public void Send(string routingKey, Message message)
        {
            Send(Exchange, routingKey, message);
        }

        public void Send(string exchange, string routingKey, Message message)
        {
            Send(exchange, routingKey, message, null);
        }

        public void Send(string exchange, string routingKey, Message message, CorrelationData correlationData)
        {
            var mandatory = (ReturnCallback != null || (correlationData != null && !string.IsNullOrEmpty(correlationData.Id)))
                            && MandatoryExpression.GetValue<bool>(EvaluationContext, message);
            Execute<object>(
                channel =>
            {
                DoSend(channel, exchange, routingKey, message, mandatory, correlationData);
                return null;
            }, ObtainTargetConnectionFactory(SendConnectionFactorySelectorExpression, message));
        }

        public void ConvertAndSend(object message)
        {
            ConvertAndSend(Exchange, RoutingKey, message, (CorrelationData)null);
        }

        public void ConvertAndSend(string routingKey, object message)
        {
            ConvertAndSend(Exchange, routingKey, message, (CorrelationData)null);
        }

        public void ConvertAndSend(string routingKey, object message, CorrelationData correlationData)
        {
            ConvertAndSend(Exchange, routingKey, message, correlationData);
        }

        public void ConvertAndSend(string exchange, string routingKey, object message)
        {
            ConvertAndSend(exchange, routingKey, message, (CorrelationData)null);
        }

        public void ConvertAndSend(string exchange, string routingKey, object message, CorrelationData correlationData)
        {
            Send(exchange, routingKey, ConvertMessageIfNecessary(message), correlationData);
        }

        public void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor)
        {
            ConvertAndSend(Exchange, RoutingKey, message, messagePostProcessor);
        }

        public void ConvertAndSend(string routingKey, object message, IMessagePostProcessor messagePostProcessor)
        {
            ConvertAndSend(Exchange, routingKey, message, messagePostProcessor, null);
        }

        public void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            ConvertAndSend(Exchange, RoutingKey, message, messagePostProcessor, correlationData);
        }

        public void ConvertAndSend(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            ConvertAndSend(Exchange, routingKey, message, messagePostProcessor, correlationData);
        }

        public void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor)
        {
            ConvertAndSend(exchange, routingKey, message, messagePostProcessor, null);
        }

        public void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            var messageToSend = ConvertMessageIfNecessary(message);
            messageToSend = messagePostProcessor.PostProcessMessage(messageToSend, correlationData);
            Send(exchange, routingKey, messageToSend, correlationData);
        }

        public void CorrelationConvertAndSend(object message, CorrelationData correlationData)
        {
            ConvertAndSend(Exchange, RoutingKey, message, correlationData);
        }

        public Message Receive()
        {
            return Receive(GetRequiredQueue());
        }

        public Message Receive(string queueName)
        {
            if (ReceiveTimeout == 0)
            {
                return DoReceiveNoWait(queueName);
            }
            else
            {
                return Receive(queueName, ReceiveTimeout);
            }
        }

        public Message Receive(int timeoutMillis)
        {
            var queue = GetRequiredQueue();
            if (timeoutMillis == 0)
            {
                return DoReceiveNoWait(queue);
            }
            else
            {
                return Receive(queue, timeoutMillis);
            }
        }

        public Message Receive(string queueName, int timeoutMillis)
        {
            var message = Execute(
                channel =>
                {
                    var delivery = ConsumeDelivery(channel, queueName, timeoutMillis);
                    if (delivery == null)
                    {
                        return null;
                    }
                    else
                    {
                        if (IsChannelLocallyTransacted(channel))
                        {
                            channel.BasicAck(delivery.Envelope.DeliveryTag, false);
                            channel.TxCommit();
                        }
                        else if (IsChannelTransacted)
                        {
                            ConnectionFactoryUtils.RegisterDeliveryTag(ConnectionFactory, channel, delivery.Envelope.DeliveryTag);
                        }
                        else
                        {
                            channel.BasicAck(delivery.Envelope.DeliveryTag, false);
                        }

                        return BuildMessageFromDelivery(delivery);
                    }
                });

            LogReceived(message);
            return message;
        }

        public object ReceiveAndConvert()
        {
            return ReceiveAndConvert(GetRequiredQueue());
        }

        public object ReceiveAndConvert(string queueName)
        {
            return ReceiveAndConvert(queueName, ReceiveTimeout);
        }

        public object ReceiveAndConvert(int timeoutMillis)
        {
            return ReceiveAndConvert(GetRequiredQueue(), timeoutMillis);
        }

        public object ReceiveAndConvert(string queueName, int timeoutMillis)
        {
            var response = timeoutMillis == 0 ? DoReceiveNoWait(queueName) : Receive(queueName, timeoutMillis);
            if (response != null)
            {
                return GetRequiredMessageConverter().FromMessage(response);
            }

            return null;
        }

        public T ReceiveAndConvert<T>(Type type)
        {
            return ReceiveAndConvert<T>(GetRequiredQueue(), type);
        }

        public T ReceiveAndConvert<T>(string queueName, Type type)
        {
            return ReceiveAndConvert<T>(queueName, ReceiveTimeout, type);
        }

        public T ReceiveAndConvert<T>(int timeoutMillis, Type type)
        {
            return ReceiveAndConvert<T>(GetRequiredQueue(), timeoutMillis, type);
        }

        public T ReceiveAndConvert<T>(string queueName, int timeoutMillis, Type type)
        {
            var response = timeoutMillis == 0 ? DoReceiveNoWait(queueName) : Receive(queueName, timeoutMillis);
            if (response != null)
            {
                return (T)GetRequiredSmartMessageConverter().FromMessage(response, type);
            }

            return default;
        }

        public bool ReceiveAndReply<R, S>(Func<R, S> callback)
            where R : class
            where S : class
        {
            return ReceiveAndReply(GetRequiredQueue(), callback);
        }

        public bool ReceiveAndReply<R, S>(string queueName, Func<R, S> callback)
            where R : class
            where S : class
        {
            return ReceiveAndReply(queueName, callback, (request, replyto) => GetReplyToAddress(request));
        }

        public bool ReceiveAndReply<R, S>(Func<R, S> callback, string exchange, string routingKey)
            where R : class
            where S : class
        {
            return ReceiveAndReply(GetRequiredQueue(), callback, exchange, routingKey);
        }

        public bool ReceiveAndReply<R, S>(string queueName, Func<R, S> callback, string replyExchange, string replyRoutingKey)
            where R : class
            where S : class
        {
            return ReceiveAndReply(queueName, callback, (request, reply) => new Address(replyExchange, replyRoutingKey));
        }

        public bool ReceiveAndReply<R, S>(Func<R, S> callback, Func<Message, S, Address> replyToAddressCallback)
            where R : class
            where S : class
        {
            return ReceiveAndReply(GetRequiredQueue(), callback, replyToAddressCallback);
        }

        public bool ReceiveAndReply<R, S>(string queueName, Func<R, S> callback, Func<Message, S, Address> replyToAddressCallback)
            where R : class
            where S : class
        {
            return DoReceiveAndReply(queueName, callback, replyToAddressCallback);
        }

        public Message SendAndReceive(Message message)
        {
            return SendAndReceive(message, null);
        }

        public Message SendAndReceive(Message message, CorrelationData correlationData)
        {
            return DoSendAndReceive(Exchange, RoutingKey, message, correlationData);
        }

        public Message SendAndReceive(string routingKey, Message message)
        {
            return SendAndReceive(routingKey, message, null);
        }

        public Message SendAndReceive(string routingKey, Message message, CorrelationData correlationData)
        {
            return DoSendAndReceive(Exchange, routingKey, message, correlationData);
        }

        public Message SendAndReceive(string exchange, string routingKey, Message message)
        {
            return SendAndReceive(exchange, routingKey, message, null);
        }

        public Message SendAndReceive(string exchange, string routingKey, Message message, CorrelationData correlationData)
        {
            return DoSendAndReceive(exchange, routingKey, message, correlationData);
        }

        public object ConvertSendAndReceive(object message)
        {
            return ConvertSendAndReceive(message, (CorrelationData)null);
        }

        public object ConvertSendAndReceive(object message, CorrelationData correlationData)
        {
            return ConvertSendAndReceive(Exchange, RoutingKey, message, null, correlationData);
        }

        public object ConvertSendAndReceive(string routingKey, object message)
        {
            return ConvertSendAndReceive(routingKey, message, (CorrelationData)null);
        }

        public object ConvertSendAndReceive(string routingKey, object message, CorrelationData correlationData)
        {
            return ConvertSendAndReceive(Exchange, routingKey, message, null, correlationData);
        }

        public object ConvertSendAndReceive(string exchange, string routingKey, object message)
        {
            return ConvertSendAndReceive(exchange, routingKey, message, (CorrelationData)null);
        }

        public object ConvertSendAndReceive(string exchange, string routingKey, object message, CorrelationData correlationData)
        {
            return ConvertSendAndReceive(exchange, routingKey, message, null, correlationData);
        }

        public object ConvertSendAndReceive(object message, IMessagePostProcessor messagePostProcessor)
        {
            return ConvertSendAndReceive(message, messagePostProcessor, null);
        }

        public object ConvertSendAndReceive(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            return ConvertSendAndReceive(Exchange, RoutingKey, message, messagePostProcessor, correlationData);
        }

        public object ConvertSendAndReceive(string routingKey, object message, IMessagePostProcessor messagePostProcessor)
        {
            return ConvertSendAndReceive(routingKey, message, messagePostProcessor, null);
        }

        public object ConvertSendAndReceive(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            return ConvertSendAndReceive(Exchange, routingKey, message, messagePostProcessor, correlationData);
        }

        public object ConvertSendAndReceive(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor)
        {
            return ConvertSendAndReceive(exchange, routingKey, message, messagePostProcessor, null);
        }

        public object ConvertSendAndReceive(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            var replyMessage = ConvertSendAndReceiveRaw(exchange, routingKey, message, messagePostProcessor, correlationData);
            if (replyMessage == null)
            {
                return null;
            }

            return GetRequiredMessageConverter().FromMessage(replyMessage);
        }

        public T ConvertSendAndReceiveAsType<T>(object message, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(message, (CorrelationData)null, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(object message, CorrelationData correlationData, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(Exchange, RoutingKey, message, null, correlationData, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(string routingKey, object message, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(routingKey, message, (CorrelationData)null, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(string routingKey, object message, CorrelationData correlationData, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(Exchange, routingKey, message, null, correlationData, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(string exchange, string routingKey, object message, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(exchange, routingKey, message, (CorrelationData)null, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(object message, IMessagePostProcessor messagePostProcessor, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(message, messagePostProcessor, null, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(Exchange, RoutingKey, message, messagePostProcessor, correlationData, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(string routingKey, object message, IMessagePostProcessor messagePostProcessor, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(routingKey, message, messagePostProcessor, null, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(Exchange, routingKey, message, messagePostProcessor, correlationData, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(exchange, routingKey, message, messagePostProcessor, null, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(string exchange, string routingKey, object message, CorrelationData correlationData, Type responseType)
        {
            return ConvertSendAndReceiveAsType<T>(exchange, routingKey, message, null, correlationData, responseType);
        }

        public T ConvertSendAndReceiveAsType<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type responseType)
        {
            var replyMessage = ConvertSendAndReceiveRaw(exchange, routingKey, message, messagePostProcessor, correlationData);
            if (replyMessage == null)
            {
                return default;
            }

            return (T)GetRequiredSmartMessageConverter().FromMessage(replyMessage, responseType);
        }

        public bool IsMandatoryFor(Message message)
        {
            return MandatoryExpression.GetValue<bool>(EvaluationContext, message);
        }

        public T Invoke<T>(Func<IRabbitOperations, T> rabbitOperations, Action<object, BasicAckEventArgs> acks, Action<object, BasicNackEventArgs> nacks)
        {
            var currentChannel = _dedicatedChannels.Value;
            if (currentChannel != null)
            {
                throw new InvalidOperationException("Nested invoke() calls are not supported; channel '" + currentChannel + "' is already associated with this thread");
            }

            Interlocked.Increment(ref _activeTemplateCallbacks);
            RabbitResourceHolder resourceHolder = null;
            Connection.IConnection connection = null;
            IModel channel;
            var connectionFactory = ConnectionFactory;
            if (IsChannelTransacted)
            {
                resourceHolder = ConnectionFactoryUtils.GetTransactionalResourceHolder(connectionFactory, true, UsePublisherConnection);
                channel = resourceHolder.GetChannel();
                if (channel == null)
                {
                    ConnectionFactoryUtils.ReleaseResources(resourceHolder);
                    throw new InvalidOperationException("Resource holder returned a null channel");
                }
            }
            else
            {
                if (UsePublisherConnection && connectionFactory.PublisherConnectionFactory != null)
                {
                    connectionFactory = connectionFactory.PublisherConnectionFactory;
                }

                connection = connectionFactory.CreateConnection();
                if (connection == null)
                {
                    throw new InvalidOperationException("Connection factory returned a null connection");
                }

                try
                {
                    channel = connection.CreateChannel(false);
                    if (channel == null)
                    {
                        throw new InvalidOperationException("Connection returned a null channel");
                    }

                    if (!connectionFactory.IsPublisherConfirms)
                    {
                        RabbitUtils.SetPhysicalCloseRequired(channel, true);
                    }

                    _dedicatedChannels.Value = channel;
                }
                catch (Exception e)
                {
                    _logger?.LogError("Exception thrown while creating channel", e);
                    RabbitUtils.CloseConnection(connection);
                    throw;
                }
            }

            var listener = AddConfirmListener(acks, nacks, channel);
            try
            {
                return rabbitOperations(this);
            }
            finally
            {
                CleanUpAfterAction(resourceHolder, connection, channel, listener);
            }
        }

        public bool WaitForConfirms(int timeoutInMilliseconds)
        {
            var channel = _dedicatedChannels.Value;
            if (channel == null)
            {
                throw new InvalidOperationException("This operation is only available within the scope of an invoke operation");
            }

            try
            {
                return channel.WaitForConfirms(TimeSpan.FromMilliseconds(timeoutInMilliseconds));
            }
            catch (Exception e)
            {
                _logger?.LogError("Exception thrown during WaitForConfirms", e);
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
        }

        public void WaitForConfirmsOrDie(int timeoutInMilliseconds)
        {
            var channel = _dedicatedChannels.Value;
            if (channel == null)
            {
                throw new InvalidOperationException("This operation is only available within the scope of an invoke operation");
            }

            try
            {
                channel.WaitForConfirmsOrDie(TimeSpan.FromMilliseconds(timeoutInMilliseconds));
            }
            catch (Exception e)
            {
                _logger?.LogError("Exception thrown during WaitForConfirmsOrDie", e);
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
        }

        public void DetermineConfirmsReturnsCapability(Connection.IConnectionFactory connectionFactory)
        {
            _publisherConfirms = connectionFactory.IsPublisherConfirms;
            _confirmsOrReturnsCapable = _publisherConfirms || connectionFactory.IsPublisherReturns;
        }

        public void DoSend(IModel channel, string exchangeArg, string routingKeyArg, Message message, bool mandatory, CorrelationData correlationData)
        {
            var exch = exchangeArg;
            var rKey = routingKeyArg;
            if (exch == null)
            {
                exch = Exchange;
            }

            if (rKey == null)
            {
                rKey = RoutingKey;
            }

            _logger?.LogTrace("Original message to publish: " + message);

            var messageToUse = message;
            var messageProperties = messageToUse.MessageProperties;
            if (mandatory)
            {
                messageProperties.Headers[PublisherCallbackChannel.RETURN_LISTENER_CORRELATION_KEY] = UUID;
            }

            if (BeforePublishPostProcessors != null)
            {
                var processors = BeforePublishPostProcessors;
                foreach (var processor in processors)
                {
                    messageToUse = processor.PostProcessMessage(messageToUse, correlationData);
                }
            }

            SetupConfirm(channel, messageToUse, correlationData);
            if (UserIdExpression != null && messageProperties.UserId == null)
            {
                var userId = UserIdExpression.GetValue<string>(EvaluationContext, messageToUse);
                if (userId != null)
                {
                    messageProperties.UserId = userId;
                }
            }

            _logger?.LogDebug("Publishing message [" + messageToUse + "] on exchange [" + exch + "], routingKey = [" + rKey + "]");
            SendToRabbit(channel, exch, rKey, mandatory, messageToUse);

            // Check if commit needed
            if (IsChannelLocallyTransacted(channel))
            {
                // Transacted channel created by this template -> commit.
                RabbitUtils.CommitIfNecessary(channel);
            }
        }

        public void AddListener(IModel channel)
        {
            if (channel is IPublisherCallbackChannel publisherCallbackChannel)
            {
                var key = channel is IChannelProxy ? ((IChannelProxy)channel).TargetChannel : channel;
                if (_publisherConfirmChannels.TryAdd(key, this))
                {
                    publisherCallbackChannel.AddListener(this);
                    _logger?.LogDebug("Added publisher confirm channel: " + channel + " to map, size now " + _publisherConfirmChannels.Count);
                }
            }
            else
            {
                throw new InvalidOperationException("Channel does not support confirms or returns; is the connection factory configured for confirms or returns?");
            }
        }

        public void HandleConfirm(PendingConfirm pendingConfirm, bool ack)
        {
            if (ConfirmCallback != null)
            {
                ConfirmCallback.Confirm(pendingConfirm.CorrelationInfo, ack, pendingConfirm.Cause);
            }
        }

        public void HandleReturn(int replyCode, string replyText, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            var callback = ReturnCallback;
            if (callback == null)
            {
                if (properties.Headers.Remove(RETURN_CORRELATION_KEY, out var messageTagHeader))
                {
                    var messageTag = messageTagHeader.ToString();
                    if (_replyHolder.TryGetValue(messageTag, out var pendingReply))
                    {
                        callback = new PendingReplyReturn(pendingReply);
                    }
                    else
                    {
                        _logger?.LogWarning("Returned request message but caller has timed out");
                    }
                }
                else
                {
                    _logger?.LogWarning("Returned message but no callback available");
                }
            }

            if (callback != null)
            {
                properties.Headers.Remove(PublisherCallbackChannel.RETURN_LISTENER_CORRELATION_KEY);
                var messageProperties = MessagePropertiesConverter.ToMessageProperties(properties, null, Encoding);
                var returnedMessage = new Message(body, messageProperties);
                callback.ReturnedMessage(returnedMessage, replyCode, replyText, exchange, routingKey);
            }
        }

        public void Revoke(IModel channel)
        {
            _publisherConfirmChannels.Remove(channel, out _);
            _logger?.LogDebug("Removed publisher confirm channel: " + channel + " from map, size now " + _publisherConfirmChannels.Count);
        }

        public void OnMessageBatch(List<Message> messages)
        {
            throw new NotSupportedException("This listener does not support message batches");
        }

        public void OnMessage(Message message)
        {
            _logger?.LogTrace("Message received " + message);
            object messageTag;
            if (CorrelationKey == null)
            {
                // using standard correlationId property
                messageTag = message.MessageProperties.CorrelationId;
            }
            else
            {
                message.MessageProperties.Headers.TryGetValue(CorrelationKey, out messageTag);
            }

            if (messageTag == null)
            {
                throw new AmqpRejectAndDontRequeueException("No correlation header in reply");
            }

            if (!_replyHolder.TryGetValue((string)messageTag, out var pendingReply))
            {
                _logger?.LogWarning("Reply received after timeout for " + messageTag);
                throw new AmqpRejectAndDontRequeueException("Reply received after timeout");
            }
            else
            {
                RestoreProperties(message, pendingReply);
                pendingReply.Reply(message);
            }
        }

        public T Execute<T>(Func<IModel, T> action)
        {
            return Execute(action, ConnectionFactory);
        }

        #endregion

        #region Protected

        protected Message ConvertSendAndReceiveRaw(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            var requestMessage = ConvertMessageIfNecessary(message);
            if (messagePostProcessor != null)
            {
                requestMessage = messagePostProcessor.PostProcessMessage(requestMessage, correlationData);
            }

            return DoSendAndReceive(exchange, routingKey, requestMessage, correlationData);
        }

        protected Message ConvertMessageIfNecessary(object message)
        {
            return message is Message ? (Message)message : GetRequiredMessageConverter().ToMessage(message, new MessageProperties());
        }

        protected Message DoSendAndReceive(string exchange, string routingKey, Message message, CorrelationData correlationData)
        {
            if (!_evaluatedFastReplyTo)
            {
                lock (_lock)
                {
                    if (!_evaluatedFastReplyTo)
                    {
                        EvaluateFastReplyTo();
                    }
                }
            }

            if (_usingFastReplyTo && UseDirectReplyToContainer)
            {
                return DoSendAndReceiveWithDirect(exchange, routingKey, message, correlationData);
            }
            else if (ReplyAddress == null || _usingFastReplyTo)
            {
                return DoSendAndReceiveWithTemporary(exchange, routingKey, message, correlationData);
            }
            else
            {
                return DoSendAndReceiveWithFixed(exchange, routingKey, message, correlationData);
            }
        }

        protected Message DoSendAndReceiveWithTemporary(string exchange, string routingKey, Message message, CorrelationData correlationData)
        {
            return Execute(
                channel =>
                {
                    if (message.MessageProperties.ReplyTo == null)
                    {
                        throw new ArgumentException("Send-and-receive methods can only be used  if the Message does not already have a replyTo property.");
                    }

                    var pendingReply = new PendingReply();
                    var messageTag = Interlocked.Increment(ref _messageTagProvider).ToString();
                    _replyHolder.TryAdd(messageTag, pendingReply);
                    string replyTo;
                    if (_usingFastReplyTo)
                    {
                        replyTo = Address.AMQ_RABBITMQ_REPLY_TO;
                    }
                    else
                    {
                        var queueDeclaration = channel.QueueDeclare();
                        replyTo = queueDeclaration.QueueName;
                    }

                    message.MessageProperties.ReplyTo = replyTo;

                    var consumerTag = Guid.NewGuid().ToString();

                    var consumer = new DoSendAndReceiveTemplateConsumer(this, channel, pendingReply);

                    channel.BasicConsume(replyTo, true, consumerTag, NoLocalReplyConsumer, true, null, consumer);
                    Message reply = null;
                    try
                    {
                        reply = ExchangeMessages(exchange, routingKey, message, correlationData, channel, pendingReply, messageTag);
                    }
                    finally
                    {
                        _replyHolder.TryRemove(messageTag, out _);
                        if (channel.IsOpen)
                        {
                            CancelConsumerQuietly(channel, consumer);
                        }
                    }

                    return reply;
                }, ObtainTargetConnectionFactory(SendConnectionFactorySelectorExpression, message));
        }

        protected Message DoSendAndReceiveWithFixed(string exchange, string routingKey, Message message, CorrelationData correlationData)
        {
            if (!_isListener)
            {
                throw new InvalidOperationException("RabbitTemplate is not configured as MessageListener - cannot use a 'replyAddress': " + ReplyAddress);
            }

            return Execute(
                channel =>
                {
                    return DoSendAndReceiveAsListener(exchange, routingKey, message, correlationData, channel);
                }, ObtainTargetConnectionFactory(SendConnectionFactorySelectorExpression, message));
        }

        protected virtual void ReplyTimedOut(string correlationId)
        {
        }

        protected Message DoReceiveNoWait(string queueName)
        {
            var message = Execute(
                channel =>
                {
                    var response = channel.BasicGet(queueName, !IsChannelTransacted);

                    // Response can be null is the case that there is no message on the queue.
                    if (response != null)
                    {
                        var deliveryTag = response.DeliveryTag;
                        if (IsChannelLocallyTransacted(channel))
                        {
                            channel.BasicAck(deliveryTag, false);
                            channel.TxCommit();
                        }
                        else if (IsChannelTransacted)
                        {
                            // Not locally transacted but it is transacted so it
                            // could be synchronized with an external transaction
                            ConnectionFactoryUtils.RegisterDeliveryTag(ConnectionFactory, channel, deliveryTag);
                        }

                        return BuildMessageFromResponse(response);
                    }

                    return null;
                }, ObtainTargetConnectionFactory(ReceiveConnectionFactorySelectorExpression, queueName));

            LogReceived(message);
            return message;
        }

        protected virtual Task DoStart()
        {
            return Task.CompletedTask;
        }

        protected virtual Task DoStop()
        {
            return Task.CompletedTask;
        }

        protected void InitDefaultStrategies()
        {
            MessageConverter = new Support.Converter.SimpleMessageConverter();
        }

        protected virtual bool UseDirectReplyTo()
        {
            if (UseTemporaryReplyQueues)
            {
                if (ReplyAddress != null)
                {
                    _logger?.LogWarning("'useTemporaryReplyQueues' is ignored when a 'replyAddress' is provided");
                }
                else
                {
                    return false;
                }
            }

            if (ReplyAddress == null || ReplyAddress == Address.AMQ_RABBITMQ_REPLY_TO)
            {
                try
                {
                    return Execute(channel =>
                    {
                        channel.QueueDeclarePassive(Address.AMQ_RABBITMQ_REPLY_TO);
                        return true;
                    });
                }
                catch (AmqpException ex) when (ex is AmqpConnectException || ex is AmqpIOException)
                {
                    if (ShouldRethrow(ex))
                    {
                        throw;
                    }
                }
            }

            return false;
        }

        protected bool IsChannelLocallyTransacted(IModel channel)
        {
            return IsChannelTransacted && !ConnectionFactoryUtils.IsChannelTransactional(channel, ConnectionFactory);
        }

        protected void SendToRabbit(IModel channel, string exchange, string routingKey, bool mandatory, Message message)
        {
            var convertedMessageProperties = channel.CreateBasicProperties();
            MessagePropertiesConverter.FromMessageProperties(message.MessageProperties, convertedMessageProperties, Encoding);
            channel.BasicPublish(exchange, routingKey, mandatory, convertedMessageProperties, message.Body);
        }

        #endregion

        #region Private
        private void Configure(RabbitOptions options)
        {
            var templateOptions = options.Template;
            if (templateOptions.Mandatory)
            {
                Mandatory = true;
            }
            else
            {
                Mandatory = options.PublisherReturns;
            }

            if (templateOptions.Retry.Enabled)
            {
                RetryTemplate = new PollyRetryTemplate(
                    new Dictionary<Type, bool>(),
                    templateOptions.Retry.MaxAttempts,
                    true,
                    (int)templateOptions.Retry.InitialInterval.TotalMilliseconds,
                    (int)templateOptions.Retry.MaxInterval.TotalMilliseconds,
                    templateOptions.Retry.Multiplier);
            }

            if (templateOptions.ReceiveTimeout.HasValue)
            {
                var asMillis = (int)templateOptions.ReceiveTimeout.Value.TotalMilliseconds;
                ReceiveTimeout = asMillis;
            }

            if (templateOptions.ReplyTimeout.HasValue)
            {
                var asMillis = (int)templateOptions.ReplyTimeout.Value.TotalMilliseconds;
                ReplyTimeout = asMillis;
            }

            Exchange = templateOptions.Exchange;
            RoutingKey = templateOptions.RoutingKey;

            if (templateOptions.DefaultReceiveQueue != null)
            {
                DefaultReceiveQueue = templateOptions.DefaultReceiveQueue;
            }
        }

        private void RestoreProperties(Message message, PendingReply pendingReply)
        {
            if (!UseCorrelationId)
            {
                // Restore the inbound correlation data
                var savedCorrelation = pendingReply.SavedCorrelation;
                if (CorrelationKey == null)
                {
                    message.MessageProperties.CorrelationId = savedCorrelation;
                }
                else
                {
                    if (savedCorrelation != null)
                    {
                        message.MessageProperties.SetHeader(CorrelationKey, savedCorrelation);
                    }
                    else
                    {
                        message.MessageProperties.Headers.Remove(CorrelationKey);
                    }
                }
            }

            // Restore any inbound replyTo
            var savedReplyTo = pendingReply.SavedReplyTo;
            message.MessageProperties.ReplyTo = savedReplyTo;
            if (savedReplyTo != null)
            {
                _logger?.LogDebug("Restored replyTo to " + savedReplyTo);
            }
        }

        private Message BuildMessageFromDelivery(Delivery delivery)
        {
            return BuildMessage(delivery.Envelope, delivery.Properties, delivery.Body, null);
        }

        private Message BuildMessageFromResponse(BasicGetResult response)
        {
            return BuildMessage(new Envelope(response.DeliveryTag, response.Redelivered, response.Exchange, response.RoutingKey), response.BasicProperties, response.Body, response.MessageCount);
        }

        private Message BuildMessage(Envelope envelope, IBasicProperties properties, byte[] body, uint? msgCount)
        {
            var messageProps = MessagePropertiesConverter.ToMessageProperties(properties, envelope, Encoding);
            if (msgCount.HasValue)
            {
                messageProps.MessageCount = msgCount.Value;
            }

            var message = new Message(body, messageProps);
            if (AfterReceivePostProcessors != null)
            {
                var processors = AfterReceivePostProcessors;
                foreach (var processor in processors)
                {
                    message = processor.PostProcessMessage(message);
                }
            }

            return message;
        }

        private Support.Converter.IMessageConverter GetRequiredMessageConverter()
        {
            var converter = MessageConverter;
            if (converter == null)
            {
                throw new AmqpIllegalStateException("No 'messageConverter' specified. Check configuration of RabbitTemplate.");
            }

            return converter;
        }

        private Support.Converter.ISmartMessageConverter GetRequiredSmartMessageConverter()
        {
            if (!(GetRequiredMessageConverter() is Support.Converter.ISmartMessageConverter converter))
            {
                throw new AmqpIllegalStateException("template's message converter must be a SmartMessageConverter");
            }

            return converter;
        }

        private string GetRequiredQueue()
        {
            var name = DefaultReceiveQueue;
            if (name == null)
            {
                throw new AmqpIllegalStateException("No 'queue' specified. Check configuration of RabbitTemplate.");
            }

            return name;
        }

        private Address GetReplyToAddress(Message request)
        {
            var replyTo = request.MessageProperties.ReplyToAddress;
            if (replyTo == null)
            {
                if (Exchange == null)
                {
                    throw new AmqpException("Cannot determine ReplyTo message property value: Request message does not contain reply-to property, and no default Exchange was set.");
                }

                replyTo = new Address(Exchange, RoutingKey);
            }

            return replyTo;
        }

        private void SetupConfirm(IModel channel, Message message, CorrelationData correlationDataArg)
        {
            if ((_publisherConfirms || ConfirmCallback != null) && channel is IPublisherCallbackChannel)
            {
                var publisherCallbackChannel = (IPublisherCallbackChannel)channel;
                var correlationData = CorrelationDataPostProcessor != null ? CorrelationDataPostProcessor.PostProcess(message, correlationDataArg) : correlationDataArg;
                var nextPublishSeqNo = channel.NextPublishSeqNo;
                message.MessageProperties.PublishSequenceNumber = nextPublishSeqNo;
                publisherCallbackChannel.AddPendingConfirm(this, nextPublishSeqNo, new PendingConfirm(correlationData, DateTimeOffset.Now.ToUnixTimeMilliseconds()));
                if (correlationData != null && !string.IsNullOrEmpty(correlationData.Id))
                {
                    message.MessageProperties.SetHeader(PublisherCallbackChannel.RETURNED_MESSAGE_CORRELATION_KEY, correlationData.Id);
                }
            }
            else if (channel is IChannelProxy && ((IChannelProxy)channel).IsConfirmSelected)
            {
                var nextPublishSeqNo = channel.NextPublishSeqNo;
                message.MessageProperties.PublishSequenceNumber = nextPublishSeqNo;
            }
        }

        private Message DoSendAndReceiveWithDirect(string exchange, string routingKey, Message message, CorrelationData correlationData)
        {
            var connectionFactory = ObtainTargetConnectionFactory(SendConnectionFactorySelectorExpression, message);
            if (UsePublisherConnection && connectionFactory.PublisherConnectionFactory != null)
            {
                connectionFactory = connectionFactory.PublisherConnectionFactory;
            }

            if (!_directReplyToContainers.TryGetValue(connectionFactory, out var container))
            {
                lock (_directReplyToContainers)
                {
                    if (!_directReplyToContainers.TryGetValue(connectionFactory, out container))
                    {
                        container = new DirectReplyToMessageListenerContainer(null, connectionFactory);
                        container.MessageListener = this;
                        container.Name = Name + "#" + Interlocked.Increment(ref _containerInstance);

                        // if (this.taskExecutor != null)
                        // {
                        //    container.setTaskExecutor(this.taskExecutor);
                        // }
                        if (AfterReceivePostProcessors != null)
                        {
                            container.SetAfterReceivePostProcessors(AfterReceivePostProcessors.ToArray());
                        }

                        container.NoLocal = NoLocalReplyConsumer;
                        if (ReplyErrorHandler != null)
                        {
                            container.ErrorHandler = ReplyErrorHandler;
                        }

                        container.Start();
                        _directReplyToContainers.TryAdd(connectionFactory, container);
                        ReplyAddress = Address.AMQ_RABBITMQ_REPLY_TO;
                    }
                }
            }

            var channelHolder = container.GetChannelHolder();
            try
            {
                var channel = channelHolder.Channel;
                if (_confirmsOrReturnsCapable.HasValue && _confirmsOrReturnsCapable.Value)
                {
                    AddListener(channel);
                }

                return DoSendAndReceiveAsListener(exchange, routingKey, message, correlationData, channel);
            }
            catch (Exception e)
            {
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
            finally
            {
                container.ReleaseConsumerFor(channelHolder, false, null);
            }
        }

        private Message DoSendAndReceiveAsListener(string exchange, string routingKey, Message message, CorrelationData correlationData, IModel channel)
        {
            var pendingReply = new PendingReply();
            var messageTag = Interlocked.Increment(ref _messageTagProvider).ToString();
            if (UseCorrelationId)
            {
                object correlationId;
                if (CorrelationKey != null)
                {
                    message.MessageProperties.Headers.TryGetValue(CorrelationKey, out correlationId);
                }
                else
                {
                    correlationId = message.MessageProperties.CorrelationId;
                }

                if (correlationId == null)
                {
                    _replyHolder[messageTag] = pendingReply;
                }
                else
                {
                    _replyHolder[(string)correlationId] = pendingReply;
                }
            }
            else
            {
                _replyHolder[messageTag] = pendingReply;
            }

            SaveAndSetProperties(message, pendingReply, messageTag);

            _logger?.LogDebug("Sending message with tag " + messageTag);
            Message reply = null;
            try
            {
                reply = ExchangeMessages(exchange, routingKey, message, correlationData, channel, pendingReply, messageTag);
                if (reply != null && AfterReceivePostProcessors != null)
                {
                    var processors = AfterReceivePostProcessors;
                    foreach (var processor in processors)
                    {
                        reply = processor.PostProcessMessage(reply);
                    }
                }
            }
            finally
            {
                _replyHolder.TryRemove(messageTag, out _);
            }

            return reply;
        }

        private void SaveAndSetProperties(Message message, PendingReply pendingReply, string messageTag)
        {
            // Save any existing replyTo and correlation data
            var savedReplyTo = message.MessageProperties.ReplyTo;
            pendingReply.SavedReplyTo = savedReplyTo;
            if (!string.IsNullOrEmpty(savedReplyTo))
            {
                _logger?.LogDebug("Replacing replyTo header: " + savedReplyTo + " in favor of template's configured reply-queue: " + ReplyAddress);
            }

            message.MessageProperties.ReplyTo = ReplyAddress;
            if (!UseCorrelationId)
            {
                object savedCorrelation = null;
                if (CorrelationKey == null)
                {
                    // using standard correlationId property
                    var correlationId = message.MessageProperties.CorrelationId;
                    if (correlationId != null)
                    {
                        savedCorrelation = correlationId;
                    }
                }
                else
                {
                    message.MessageProperties.Headers.TryGetValue(CorrelationKey, out savedCorrelation);
                }

                pendingReply.SavedCorrelation = (string)savedCorrelation;
                if (CorrelationKey == null)
                {
                    // using standard correlationId property
                    message.MessageProperties.CorrelationId = messageTag;
                }
                else
                {
                    message.MessageProperties.SetHeader(CorrelationKey, messageTag);
                }
            }
        }

        private Message ExchangeMessages(string exchange, string routingKey, Message message, CorrelationData correlationData, IModel channel, PendingReply pendingReply, string messageTag)
        {
            Message reply;
            var mandatory = IsMandatoryFor(message);
            if (mandatory && ReturnCallback == null)
            {
                message.MessageProperties.Headers[RETURN_CORRELATION_KEY] = messageTag;
            }

            DoSend(channel, exchange, routingKey, message, mandatory, correlationData);

            reply = ReplyTimeout < 0 ? pendingReply.Get() : pendingReply.Get(ReplyTimeout);
            _logger?.LogDebug("Reply: " + reply);
            if (reply == null)
            {
                ReplyTimedOut(message.MessageProperties.CorrelationId);
            }

            return reply;
        }

        private void CancelConsumerQuietly(IModel channel, DefaultBasicConsumer consumer)
        {
            RabbitUtils.Cancel(channel, consumer.ConsumerTag);
        }

        private bool DoReceiveAndReply<R, S>(string queueName, Func<R, S> callback, Func<Message, S, Address> replyToAddressCallback)
            where R : class
            where S : class
        {
            var result = Execute(
                channel =>
                {
                    var receiveMessage = ReceiveForReply(queueName, channel);
                    if (receiveMessage != null)
                    {
                        return SendReply(callback, replyToAddressCallback, channel, receiveMessage);
                    }

                    return false;
                }, ObtainTargetConnectionFactory(ReceiveConnectionFactorySelectorExpression, queueName));
            return result;
        }

        private Message ReceiveForReply(string queueName, IModel channel)
        {
            var channelTransacted = IsChannelTransacted;
            var channelLocallyTransacted = IsChannelLocallyTransacted(channel);
            Message receiveMessage = null;
            if (ReceiveTimeout == 0)
            {
                var response = channel.BasicGet(queueName, !channelTransacted);

                // Response can be null in the case that there is no message on the queue.
                if (response != null)
                {
                    var deliveryTag1 = response.DeliveryTag;

                    if (channelLocallyTransacted)
                    {
                        channel.BasicAck(deliveryTag1, false);
                    }
                    else if (channelTransacted)
                    {
                        // Not locally transacted but it is transacted so it could be
                        // synchronized with an external transaction
                        ConnectionFactoryUtils.RegisterDeliveryTag(ConnectionFactory, channel, deliveryTag1);
                    }

                    receiveMessage = BuildMessageFromResponse(response);
                }
            }
            else
            {
                var delivery = ConsumeDelivery(channel, queueName, ReceiveTimeout);
                if (delivery != null)
                {
                    var deliveryTag2 = delivery.Envelope.DeliveryTag;
                    if (channelTransacted && !channelLocallyTransacted)
                    {
                        // Not locally transacted but it is transacted so it could be
                        // synchronized with an external transaction
                        ConnectionFactoryUtils.RegisterDeliveryTag(ConnectionFactory, channel, deliveryTag2);
                    }
                    else
                    {
                        channel.BasicAck(deliveryTag2, false);
                    }

                    receiveMessage = BuildMessageFromDelivery(delivery);
                }
            }

            LogReceived(receiveMessage);
            return receiveMessage;
        }

        private Delivery ConsumeDelivery(IModel channel, string queueName, int timeoutMillis)
        {
            Delivery delivery = null;
            Exception exception = null;
            var future = new TaskCompletionSource<Delivery>();

            DefaultBasicConsumer consumer = null;
            try
            {
                consumer = CreateConsumer(queueName, channel, future, timeoutMillis < 0 ? DEFAULT_CONSUME_TIMEOUT : timeoutMillis);
                if (timeoutMillis < 0)
                {
                    delivery = future.Task.Result;
                }
                else
                {
                    if (future.Task.Wait(TimeSpan.FromMilliseconds(timeoutMillis)))
                    {
                        delivery = future.Task.Result;
                    }
                    else
                    {
                        RabbitUtils.SetPhysicalCloseRequired(channel, true);
                    }
                }
            }
            catch (AggregateException e)
            {
                var cause = e.InnerExceptions.FirstOrDefault();

                _logger?.LogError("Consumer failed to receive message: " + consumer, cause);
                exception = RabbitExceptionTranslator.ConvertRabbitAccessException(cause);
                throw exception;
            }
            finally
            {
                if (consumer != null && !(exception is ConsumerCancelledException) && channel.IsOpen)
                {
                    CancelConsumerQuietly(channel, consumer);
                }
            }

            return delivery;
        }

        private void LogReceived(Message message)
        {
            if (message == null)
            {
                _logger?.LogDebug("Received no message");
            }
            else
            {
                _logger?.LogDebug("Received: " + message);
            }
        }

        private bool SendReply<R, S>(Func<R, S> receiveAndReplyCallback, Func<Message, S, Address> replyToAddressCallback, IModel channel, Message receiveMessage)
            where R : class
            where S : class
        {
            var receive = receiveMessage;

            // TODO: What is this doing?
            // if (!(ReceiveAndReplyMessageCallback.class.isAssignableFrom(callback.getClass()))) {
            // receive = getRequiredMessageConverter().fromMessage(receiveMessage);
            //  }
            if (!(receive is R messageAsR))
            {
                throw new ArgumentException("'callback' can't handle received object '" + receive + "'");
            }

            var reply = receiveAndReplyCallback(messageAsR);

            if (reply != null)
            {
                DoSendReply(replyToAddressCallback, channel, receiveMessage, reply);
            }
            else if (IsChannelLocallyTransacted(channel))
            {
                channel.TxCommit();
            }

            return true;
        }

        private void DoSendReply<S>(Func<Message, S, Address> replyToAddressCallback, IModel channel, Message receiveMessage, S reply)
            where S : class
        {
            var replyTo = replyToAddressCallback(receiveMessage, reply);

            var replyMessage = ConvertMessageIfNecessary(reply);

            var receiveMessageProperties = receiveMessage.MessageProperties;
            var replyMessageProperties = replyMessage.MessageProperties;

            object correlation;
            if (CorrelationKey == null)
            {
                correlation = receiveMessageProperties.CorrelationId;
            }
            else
            {
                receiveMessageProperties.Headers.TryGetValue(CorrelationKey, out correlation);
            }

            if (CorrelationKey == null || correlation == null)
            {
                // using standard correlationId property
                if (correlation == null)
                {
                    var messageId = receiveMessageProperties.MessageId;
                    if (messageId != null)
                    {
                        correlation = messageId;
                    }
                }

                replyMessageProperties.CorrelationId = (string)correlation;
            }
            else
            {
                replyMessageProperties.SetHeader(CorrelationKey, correlation);
            }

            // 'doSend()' takes care of 'channel.txCommit()'.
            DoSend(channel, replyTo.ExchangeName, replyTo.RoutingKey, replyMessage, ReturnCallback != null && IsMandatoryFor(replyMessage), null);
        }

        private DefaultBasicConsumer CreateConsumer(string queueName, IModel channel, TaskCompletionSource<Delivery> future, int timeoutMillis)
        {
            channel.BasicQos(0, 1, false); // TODO: Verify
            var latch = new CountdownEvent(1);
            var consumer = new DefaultTemplateConsumer(channel, latch, future, queueName);

            var consumeResult = channel.BasicConsume(queueName, false, consumer); // Verify autoack false
            if (!latch.Wait(TimeSpan.FromMilliseconds(timeoutMillis)))
            {
                if (channel is IChannelProxy asProxy)
                {
                    asProxy.TargetChannel.Close();
                }

                future.TrySetException(new ConsumeOkNotReceivedException("Blocking receive, consumer failed to consume within " + timeoutMillis + " ms: " + consumer));
            }

            return consumer;
        }

        private Connection.IConnectionFactory ObtainTargetConnectionFactory(IExpression expression, object rootObject)
        {
            if (expression != null && ConnectionFactory is AbstractRoutingConnectionFactory)
            {
                var routingConnectionFactory = (AbstractRoutingConnectionFactory)ConnectionFactory;
                object lookupKey;
                if (rootObject != null)
                {
                    lookupKey = SendConnectionFactorySelectorExpression.GetValue(EvaluationContext, rootObject);
                }
                else
                {
                    lookupKey = SendConnectionFactorySelectorExpression.GetValue(EvaluationContext);
                }

                if (lookupKey != null)
                {
                    var connectionFactory = routingConnectionFactory.GetTargetConnectionFactory(lookupKey);
                    if (connectionFactory != null)
                    {
                        return connectionFactory;
                    }
                    else if (!routingConnectionFactory.LenientFallback)
                    {
                        throw new InvalidOperationException("Cannot determine target ConnectionFactory for lookup key [" + lookupKey + "]");
                    }
                }
            }

            return ConnectionFactory;
        }

        private T Execute<T>(Func<IModel, T> action, Connection.IConnectionFactory connectionFactory)
        {
            if (RetryTemplate != null)
            {
                try
                {
                    return RetryTemplate.Execute(
                            context => DoExecute(action, connectionFactory),
                            (IRecoveryCallback<T>)RecoveryCallback);
                }
                catch (Exception e)
                {
                    _logger?.LogError("Exception executing DoExecute in retry", e);
                    throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
                }
            }
            else
            {
                return DoExecute(action, connectionFactory);
            }
        }

        private T DoExecute<T>(Func<IModel, T> channelCallback, Connection.IConnectionFactory connectionFactory)
        {
            // NOSONAR complexity
            if (channelCallback == null)
            {
                throw new ArgumentNullException(nameof(channelCallback));
            }

            IModel channel = null;
            var invokeScope = false;

            // No need to check the thread local if we know that no invokes are in process
            if (_activeTemplateCallbacks > 0)
            {
                channel = _dedicatedChannels.Value;
            }

            RabbitResourceHolder resourceHolder = null;
            Connection.IConnection connection = null;
            if (channel == null)
            {
                if (IsChannelTransacted)
                {
                    resourceHolder = ConnectionFactoryUtils.GetTransactionalResourceHolder(connectionFactory, true, UsePublisherConnection);
                    channel = resourceHolder.GetChannel();
                    if (channel == null)
                    {
                        ConnectionFactoryUtils.ReleaseResources(resourceHolder);
                        throw new InvalidOperationException("Resource holder returned a null channel");
                    }
                }
                else
                {
                    connection = ConnectionFactoryUtils.CreateConnection(connectionFactory, UsePublisherConnection);
                    if (connection == null)
                    {
                        throw new InvalidOperationException("Connection factory returned a null connection");
                    }

                    try
                    {
                        channel = connection.CreateChannel(false);
                        if (channel == null)
                        {
                            throw new InvalidOperationException("Connection returned a null channel");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError("Exception while creating channel", e);
                        RabbitUtils.CloseConnection(connection);
                        throw;
                    }
                }
            }
            else
            {
                invokeScope = true;
            }

            try
            {
                return InvokeAction(channelCallback, connectionFactory, channel);
            }
            catch (Exception ex)
            {
                if (IsChannelLocallyTransacted(channel) && resourceHolder != null)
                {
                    resourceHolder.RollbackAll();
                }

                throw ConvertRabbitAccessException(ex);
            }
            finally
            {
                CleanUpAfterAction(channel, invokeScope, resourceHolder, connection);
            }
        }

        private T InvokeAction<T>(Func<IModel, T> channelCallback, Connection.IConnectionFactory connectionFactory, IModel channel)
        {
            if (!_confirmsOrReturnsCapable.HasValue)
            {
                DetermineConfirmsReturnsCapability(connectionFactory);
            }

            if (_confirmsOrReturnsCapable.Value)
            {
                AddListener(channel);
            }

            _logger?.LogDebug("Executing callback on RabbitMQ Channel: " + channel);
            return channelCallback(channel);
        }

        private ConfirmListener AddConfirmListener(Action<object, BasicAckEventArgs> acks, Action<object, BasicNackEventArgs> nacks, IModel channel)
        {
            if (acks != null && nacks != null && channel is IChannelProxy && ((IChannelProxy)channel).IsConfirmSelected)
            {
                return new ConfirmListener(acks, nacks, channel);
            }

            return null;
        }

        private void CleanUpAfterAction(IModel channel, bool invokeScope, RabbitResourceHolder resourceHolder, Connection.IConnection connection)
        {
            if (!invokeScope)
            {
                if (resourceHolder != null)
                {
                    ConnectionFactoryUtils.ReleaseResources(resourceHolder);
                }
                else
                {
                    RabbitUtils.CloseChannel(channel);
                    RabbitUtils.CloseConnection(connection);
                }
            }
        }

        private void CleanUpAfterAction(RabbitResourceHolder resourceHolder, Connection.IConnection connection, IModel channel, ConfirmListener listener)
        {
            if (listener != null)
            {
                listener.Remove();
            }

            Interlocked.Decrement(ref _activeTemplateCallbacks);
            _dedicatedChannels.Value = null;

            if (resourceHolder != null)
            {
                ConnectionFactoryUtils.ReleaseResources(resourceHolder);
            }
            else
            {
                RabbitUtils.CloseChannel(channel);
                RabbitUtils.CloseConnection(connection);
            }
        }

        private bool ShouldRethrow(AmqpException ex)
        {
            Exception cause = ex;
            while (cause != null && !(cause is ShutdownSignalException))
            {
                cause = cause.InnerException;
            }

            if (cause != null && RabbitUtils.IsPassiveDeclarationChannelClose((ShutdownSignalException)cause))
            {
                _logger?.LogWarning("Broker does not support fast replies via 'amq.rabbitmq.reply-to', temporary " + "queues will be used: " + cause.Message + ".");
                ReplyAddress = null;
                return false;
            }

            if (ex != null)
            {
                _logger?.LogDebug("IO error, deferring directReplyTo detection: " + ex.ToString());
            }

            return true;
        }

        private void EvaluateFastReplyTo()
        {
            _usingFastReplyTo = UseDirectReplyTo();
            _evaluatedFastReplyTo = true;
        }
        #endregion

        #region Nested Types
        protected class DoSendAndReceiveTemplateConsumer : AbstractTemplateConsumer
        {
            private readonly RabbitTemplate _template;
            private readonly PendingReply _pendingReply;

            public DoSendAndReceiveTemplateConsumer(RabbitTemplate template, IModel channel, PendingReply pendingReply)
                : base(channel)
            {
                _template = template;
                _pendingReply = pendingReply;
            }

            public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
            {
                var messageProperties = _template
                    .MessagePropertiesConverter
                    .ToMessageProperties(properties, new Envelope(deliveryTag, redelivered, exchange, routingKey), _template.Encoding);
                var reply = new Message(body, messageProperties);
                _template._logger?.LogTrace("Message received " + reply);
                if (_template.AfterReceivePostProcessors != null)
                {
                    var processors = _template.AfterReceivePostProcessors;
                    foreach (var processor in processors)
                    {
                        reply = processor.PostProcessMessage(reply);
                    }
                }

                _pendingReply.Reply(reply);
            }

            public override void HandleModelShutdown(object model, ShutdownEventArgs reason)
            {
                base.HandleModelShutdown(model, reason);
                if (!RabbitUtils.IsNormalChannelClose(reason))
                {
                    _pendingReply.CompleteExceptionally(new ShutdownSignalException(reason));
                }
                else
                {
                    _pendingReply.Reply(null);
                }
            }
        }

        protected class DefaultTemplateConsumer : AbstractTemplateConsumer
        {
            private readonly CountdownEvent _latch;
            private readonly TaskCompletionSource<Delivery> _completionSource;
            private readonly string _queueName;

            public DefaultTemplateConsumer(IModel channel, CountdownEvent latch, TaskCompletionSource<Delivery> completionSource, string queueName)
                : base(channel)
            {
                _latch = latch;
                _completionSource = completionSource;
                _queueName = queueName;
            }

            public override void HandleBasicCancel(string consumerTag)
            {
                _completionSource.TrySetException(new ConsumerCancelledException());
                Signal();
            }

            public override void HandleBasicConsumeOk(string consumerTag)
            {
                base.HandleBasicConsumeOk(consumerTag);
                Signal();
            }

            public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
            {
                base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
                _completionSource.SetResult(new Delivery(consumerTag, new Envelope(deliveryTag, redelivered, exchange, routingKey), properties, body, _queueName));
                Signal();
            }

            public override void HandleModelShutdown(object model, ShutdownEventArgs reason)
            {
                base.HandleModelShutdown(model, reason);
                if (!RabbitUtils.IsNormalChannelClose(reason))
                {
                    _completionSource.TrySetException(new ShutdownSignalException(reason));
                }

                Signal();
            }

            private void Signal()
            {
                try
                {
                    _latch.Signal();
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
        }

        protected abstract class AbstractTemplateConsumer : DefaultBasicConsumer
        {
            protected AbstractTemplateConsumer(IModel channel)
                : base(channel)
            {
            }

            public override string ToString()
            {
                return "TemplateConsumer [channel=" + Model + ", consumerTag=" + ConsumerTag + "]";
            }
        }

        protected class PendingReply
        {
            private readonly TaskCompletionSource<Message> _future = new TaskCompletionSource<Message>();

            public string SavedReplyTo { get; set; }

            public string SavedCorrelation { get; set; }

            public Message Get()
            {
                try
                {
                    return _future.Task.Result;
                }
                catch (Exception e)
                {
                    throw RabbitExceptionTranslator.ConvertRabbitAccessException(e.InnerException);
                }
            }

            public Message Get(int timeout)
            {
                try
                {
                    if (_future.Task.Wait(TimeSpan.FromMilliseconds(timeout)))
                    {
                        return _future.Task.Result;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception e)
                {
                    throw RabbitExceptionTranslator.ConvertRabbitAccessException(e.InnerException);
                }
            }

            public void Reply(Message reply)
            {
                _future.TrySetResult(reply);
            }

            public void Returned(AmqpMessageReturnedException e)
            {
                CompleteExceptionally(e);
            }

            public void CompleteExceptionally(Exception exception)
            {
                _future.TrySetException(exception);
            }
        }

        protected class ConfirmListener
        {
            private Action<object, BasicAckEventArgs> _acks;
            private Action<object, BasicNackEventArgs> _nacks;
            private IModel _channel;

            public ConfirmListener(Action<object, BasicAckEventArgs> acks, Action<object, BasicNackEventArgs> nacks, IModel channel)
            {
                _channel = channel;
                _acks = acks;
                _nacks = nacks;

                _channel.BasicAcks += Channel_BasicAcks;
                _channel.BasicNacks += Channel_BasicNacks;
            }

            public void Remove()
            {
                _channel.BasicAcks -= Channel_BasicAcks;
                _channel.BasicNacks -= Channel_BasicNacks;
            }

            private void Channel_BasicNacks(object sender, BasicNackEventArgs args)
            {
                _nacks(sender, args);
            }

            private void Channel_BasicAcks(object sender, BasicAckEventArgs args)
            {
                _acks(sender, args);
            }
        }

        public interface IConfirmCallback
        {
            void Confirm(CorrelationData correlationData, bool ack, string cause);
        }

        private class PendingReplyReturn : IReturnCallback
        {
            private PendingReply _pendingReply;

            public PendingReplyReturn(PendingReply pendingReply)
            {
                _pendingReply = pendingReply;
            }

            public void ReturnedMessage(Message message, int replyCode, string replyText, string exchange, string routingKey)
            {
                _pendingReply.Returned(new AmqpMessageReturnedException("Message returned", message, replyCode, replyText, exchange, routingKey));
            }
        }

        public interface IReturnCallback
        {
            void ReturnedMessage(Message message, int replyCode, string replyText, string exchange, string routingKey);
        }
        #endregion
    }
}
