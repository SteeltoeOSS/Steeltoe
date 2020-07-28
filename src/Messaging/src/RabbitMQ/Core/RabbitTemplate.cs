// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Impl;
using Steeltoe.Common.Expression;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Services;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Extensions;
using Steeltoe.Messaging.Rabbit.Listener;
using Steeltoe.Messaging.Rabbit.Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Core
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class RabbitTemplate : AbstractMessagingTemplate<RabbitDestination>, IRabbitTemplate, IMessageListener, IListenerContainerAware, IPublisherCallbackChannel.IListener, IServiceNameAware, IDisposable
    {
        public const string DEFAULT_SERVICE_NAME = "rabbitTemplate";

        internal readonly object _lock = new object();
        internal readonly ConcurrentDictionary<IModel, RabbitTemplate> _publisherConfirmChannels = new ConcurrentDictionary<IModel, RabbitTemplate>();
        internal readonly ConcurrentDictionary<string, PendingReply> _replyHolder = new ConcurrentDictionary<string, PendingReply>();
        internal readonly Dictionary<Connection.IConnectionFactory, DirectReplyToMessageListenerContainer> _directReplyToContainers = new Dictionary<Connection.IConnectionFactory, DirectReplyToMessageListenerContainer>();
        internal readonly AsyncLocal<IModel> _dedicatedChannels = new AsyncLocal<IModel>();
        internal readonly IOptionsMonitor<RabbitOptions> _optionsMonitor;
        internal bool _evaluatedFastReplyTo;
        internal bool _usingFastReplyTo;

        protected readonly ILogger _logger;

        private const string RETURN_CORRELATION_KEY = "spring_request_return_correlation";
        private const string DEFAULT_EXCHANGE = "";
        private const string DEFAULT_ROUTING_KEY = "";
        private const int DEFAULT_REPLY_TIMEOUT = 5000;
        private const int DEFAULT_CONSUME_TIMEOUT = 10000;

        private RabbitOptions _options;
        private int _activeTemplateCallbacks;
        private int _messageTagProvider;
        private int _containerInstance;
        private bool _isListener = false;
        private bool? _confirmsOrReturnsCapable;
        private bool _publisherConfirms;
        private string _replyAddress;

        [ActivatorUtilitiesConstructor]
        public RabbitTemplate(IOptionsMonitor<RabbitOptions> optionsMonitor, Connection.IConnectionFactory connectionFactory, ISmartMessageConverter messageConverter, ILogger logger = null)
            : base()
        {
            _optionsMonitor = optionsMonitor;
            ConnectionFactory = connectionFactory;
            MessageConverter = messageConverter ?? new Support.Converter.SimpleMessageConverter();
            _logger = logger;
            Configure(Options);
        }

        public RabbitTemplate(RabbitOptions options, Connection.IConnectionFactory connectionFactory, ISmartMessageConverter messageConverter, ILogger logger = null)
            : base()
        {
            _options = options;
            ConnectionFactory = connectionFactory;
            MessageConverter = messageConverter ?? new Support.Converter.SimpleMessageConverter();
            _logger = logger;
            Configure(Options);
        }

        public RabbitTemplate(IOptionsMonitor<RabbitOptions> optionsMonitor, Connection.IConnectionFactory connectionFactory, ILogger logger = null)
            : base()
        {
            _optionsMonitor = optionsMonitor;
            ConnectionFactory = connectionFactory;
            MessageConverter = new Support.Converter.SimpleMessageConverter();
            _logger = logger;
            Configure(Options);
        }

        public RabbitTemplate(RabbitOptions options, Connection.IConnectionFactory connectionFactory, ILogger logger = null)
            : base()
        {
            _options = options;
            ConnectionFactory = connectionFactory;
            MessageConverter = new Support.Converter.SimpleMessageConverter();
            _logger = logger;
            Configure(Options);
        }

        public RabbitTemplate(Connection.IConnectionFactory connectionFactory, ILogger logger = null)
            : base()
        {
            ConnectionFactory = connectionFactory;
            MessageConverter = new Support.Converter.SimpleMessageConverter();
            DefaultSendDestination = string.Empty + "/" + string.Empty;
            DefaultReceiveDestination = null;
            _logger = logger;
        }

        public RabbitTemplate(ILogger logger = null)
            : base()
        {
            MessageConverter = new Support.Converter.SimpleMessageConverter();
            DefaultSendDestination = string.Empty + "/" + string.Empty;
            DefaultReceiveDestination = null;
            _logger = logger;
        }

        public virtual Connection.IConnectionFactory ConnectionFactory { get; set; }

        public virtual bool IsChannelTransacted { get; set; }

        #region Properties

        public virtual string RoutingKey
        {
            get => DefaultSendDestination.RoutingKey;
            set => DefaultSendDestination = new RabbitDestination(DefaultSendDestination.ExchangeName, value);
        }

        public virtual string Exchange
        {
            get => DefaultSendDestination.ExchangeName;
            set => DefaultSendDestination = new RabbitDestination(value, DefaultSendDestination.RoutingKey);
        }

        public virtual string DefaultReceiveQueue
        {
            get => DefaultReceiveDestination?.QueueName;
            set => DefaultReceiveDestination = new RabbitDestination(value);
        }

        public virtual AcknowledgeMode ContainerAckMode { get; set; }

        public virtual Encoding Encoding { get; set; } = EncodingUtils.Utf8;

        public virtual string ReplyAddress
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

        public virtual int ReceiveTimeout { get; set; } = 0;

        public virtual int ReplyTimeout { get; set; } = DEFAULT_REPLY_TIMEOUT;

        public virtual IMessageHeadersConverter MessagePropertiesConverter { get; set; } = new DefaultMessageHeadersConverter();

        public virtual IConfirmCallback ConfirmCallback { get; set; }

        public virtual IReturnCallback ReturnCallback { get; set; }

        public virtual bool Mandatory
        {
            get
            {
                return MandatoryExpression.GetValue<bool>();
            }

            set
            {
                MandatoryExpression = new ValueExpression<bool>(value);
            }
        }

        public virtual IExpression MandatoryExpression { get; set; } = new ValueExpression<bool>(false);

        public virtual string MandatoryExpressionString { get; set; }

        public virtual IExpression SendConnectionFactorySelectorExpression { get; set; }

        public virtual IExpression ReceiveConnectionFactorySelectorExpression { get; set; }

        public virtual string CorrelationKey { get; set; }

        public virtual IEvaluationContext EvaluationContext { get; set; } // TODO  = new StandardEvaluationContext();

        public virtual IRetryOperation RetryTemplate { get; set; }

        public virtual IRecoveryCallback RecoveryCallback { get; set; }

        // public virtual void setBeanFactory(BeanFactory beanFactory) throws BeansException
        //   {
        // this.evaluationContext.setBeanResolver(new BeanFactoryResolver(beanFactory));
        // this.evaluationContext.addPropertyAccessor(new MapAccessor());
        // }
        public virtual IList<IMessagePostProcessor> BeforePublishPostProcessors { get; internal set; }

        public virtual IList<IMessagePostProcessor> AfterReceivePostProcessors { get; internal set; }

        public virtual ICorrelationDataPostProcessor CorrelationDataPostProcessor { get; set; }

        public virtual bool UseTemporaryReplyQueues { get; set; }

        public virtual bool UseDirectReplyToContainer { get; set; } = true;

        public virtual IExpression UserIdExpression { get; set; }

        public virtual string UserIdExpressionString { get; set; }

        public virtual string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        public virtual bool UseCorrelationId { get; set; }

        public virtual bool UsePublisherConnection { get; set; }

        public virtual bool NoLocalReplyConsumer { get; set; }

        public virtual IErrorHandler ReplyErrorHandler { get; set; }

        public virtual bool IsRunning
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

        public virtual string UUID { get; } = Guid.NewGuid().ToString();

        public virtual bool IsConfirmListener => ConfirmCallback != null;

        public virtual bool IsReturnListener => true;

        protected internal RabbitOptions Options
        {
            get
            {
                if (_optionsMonitor != null)
                {
                    return _optionsMonitor.CurrentValue;
                }

                return _options;
            }
        }
        #endregion Properties

        #region Public

        #region PostProcessors
        public virtual void SetBeforePublishPostProcessors(params IMessagePostProcessor[] beforePublishPostProcessors)
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

        public virtual void AddBeforePublishPostProcessors(params IMessagePostProcessor[] beforePublishPostProcessors)
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

        public virtual bool RemoveBeforePublishPostProcessor(IMessagePostProcessor beforePublishPostProcessor)
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

        public virtual void SetAfterReceivePostProcessors(params IMessagePostProcessor[] afterReceivePostProcessors)
        {
            if (afterReceivePostProcessors == null)
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

        public virtual void AddAfterReceivePostProcessors(params IMessagePostProcessor[] afterReceivePostProcessors)
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

        public virtual bool RemoveAfterReceivePostProcessor(IMessagePostProcessor afterReceivePostProcessor)
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
                AfterReceivePostProcessors = copy;
                return result;
            }

            return false;
        }
        #endregion PostProcessors

        #region IPublisherCallbackChannel.IListener

        public virtual void HandleConfirm(PendingConfirm pendingConfirm, bool ack)
        {
            if (ConfirmCallback != null)
            {
                ConfirmCallback.Confirm(pendingConfirm.CorrelationInfo, ack, pendingConfirm.Cause);
            }
        }

        public virtual void HandleReturn(int replyCode, string replyText, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            var callback = ReturnCallback;
            if (callback == null)
            {
                var messageProperties = MessagePropertiesConverter.ToMessageHeaders(properties, null, Encoding);
                var messageTagHeader = messageProperties.Get<string>(RETURN_CORRELATION_KEY);
                if (messageTagHeader != null)
                {
                    var messageTag = messageTagHeader;
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
                var messageProperties = MessagePropertiesConverter.ToMessageHeaders(properties, null, Encoding);
                var returnedMessage = Message.Create(body, messageProperties);
                callback.ReturnedMessage(returnedMessage, replyCode, replyText, exchange, routingKey);
            }
        }

        public virtual void Revoke(IModel channel)
        {
            _publisherConfirmChannels.Remove(channel, out _);
            _logger?.LogDebug("Removed publisher confirm channel: {channel} from map, size now {size}", channel, _publisherConfirmChannels.Count);
        }
        #endregion IPublisherCallbackChannel.IListener

        #region IMessageListener

        public virtual void OnMessageBatch(List<IMessage> messages)
        {
            throw new NotSupportedException("This listener does not support message batches");
        }

        public virtual void OnMessage(IMessage message)
        {
            _logger?.LogTrace("Message received {message}", message);
            object messageTag;
            if (CorrelationKey == null)
            {
                // using standard correlationId property
                messageTag = message.Headers.CorrelationId();
            }
            else
            {
                messageTag = message.Headers.Get<object>(CorrelationKey);
            }

            if (messageTag == null)
            {
                throw new RabbitRejectAndDontRequeueException("No correlation header in reply");
            }

            if (!_replyHolder.TryGetValue((string)messageTag, out var pendingReply))
            {
                _logger?.LogWarning("Reply received after timeout for " + messageTag);
                throw new RabbitRejectAndDontRequeueException("Reply received after timeout");
            }
            else
            {
                RestoreProperties(message, pendingReply);
                pendingReply.Reply(message);
            }
        }
        #endregion IMessageListener

        #region IListenerContainerAware
        public virtual List<string> GetExpectedQueueNames()
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
                    _logger?.LogInformation("Cannot verify reply queue because 'replyAddress' is not a simple queue name: {replyAddres}", ReplyAddress);
                }
            }

            return replyQueue;
        }

        #endregion IListenerContainerAware

        #region RabbitSend
        public virtual void Send(string routingKey, IMessage message)
        {
            Send(GetDefaultExchange(), routingKey, message);
        }

        public virtual void Send(string exchange, string routingKey, IMessage message)
        {
            Send(exchange, routingKey, message, null);
        }

        public virtual void Send(string exchange, string routingKey, IMessage message, CorrelationData correlationData)
        {
            var mandatory = (ReturnCallback != null || (correlationData != null && !string.IsNullOrEmpty(correlationData.Id)))
                            && MandatoryExpression.GetValue<bool>(EvaluationContext, message);
            Execute<object>(
                channel =>
                {
                    DoSend(channel, exchange, routingKey, message, mandatory, correlationData, default);
                    return null;
                }, ObtainTargetConnectionFactory(SendConnectionFactorySelectorExpression, message));
        }

        public virtual Task SendAsync(string routingKey, IMessage message, CancellationToken cancellationToken = default)
        {
            return SendAsync(GetDefaultExchange(), routingKey, message, cancellationToken);
        }

        public virtual Task SendAsync(string exchange, string routingKey, IMessage message, CancellationToken cancellationToken = default)
        {
            return SendAsync(exchange, routingKey, message, null, cancellationToken);
        }

        public virtual Task SendAsync(string exchange, string routingKey, IMessage message, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            var mandatory = (ReturnCallback != null || (correlationData != null && !string.IsNullOrEmpty(correlationData.Id)))
                            && MandatoryExpression.GetValue<bool>(EvaluationContext, message);
            return Task.Run(
                () => Execute<object>(
                channel =>
                {
                    DoSend(channel, exchange, routingKey, message, mandatory, correlationData, cancellationToken);
                    return null;
                }, ObtainTargetConnectionFactory(SendConnectionFactorySelectorExpression, message)), cancellationToken);
        }

        #endregion RabbitSend

        #region RabbitConvertAndSend

        public virtual void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor)
        {
            ConvertAndSend(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, null);
        }

        public virtual void ConvertAndSend(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            ConvertAndSend(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, correlationData);
        }

        public virtual void ConvertAndSend(string routingKey, object message)
        {
            ConvertAndSend(GetDefaultExchange(), routingKey, message, null, null);
        }

        public virtual void ConvertAndSend(string routingKey, object message, CorrelationData correlationData)
        {
            ConvertAndSend(GetDefaultExchange(), routingKey, message, null, correlationData);
        }

        public virtual void ConvertAndSend(string routingKey, object message, IMessagePostProcessor messagePostProcessor)
        {
            ConvertAndSend(GetDefaultExchange(), routingKey, message, messagePostProcessor, null);
        }

        public virtual void ConvertAndSend(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            ConvertAndSend(GetDefaultExchange(), routingKey, message, messagePostProcessor, correlationData);
        }

        public virtual void ConvertAndSend(string exchange, string routingKey, object message)
        {
            ConvertAndSend(exchange, routingKey, message, (CorrelationData)null);
        }

        public virtual void ConvertAndSend(string exchange, string routingKey, object message, CorrelationData correlationData)
        {
            ConvertAndSend(exchange, routingKey, message, null, correlationData);
        }

        public virtual void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor)
        {
            ConvertAndSend(exchange, routingKey, message, messagePostProcessor, null);
        }

        public virtual void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            var messageToSend = ConvertMessageIfNecessary(message);
            if (messagePostProcessor != null)
            {
                messageToSend = messagePostProcessor.PostProcessMessage(messageToSend, correlationData);
            }

            Send(exchange, routingKey, messageToSend, correlationData);
        }

        public virtual Task ConvertAndSendAsync(object message, IMessagePostProcessor messagePostProcessor, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, null, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, correlationData, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(string routingKey, object message, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(GetDefaultExchange(), routingKey, message, null, null, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(string routingKey, object message, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(GetDefaultExchange(), routingKey, message, null, correlationData, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(GetDefaultExchange(), routingKey, message, messagePostProcessor, null, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(GetDefaultExchange(), routingKey, message, messagePostProcessor, correlationData, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(string exchange, string routingKey, object message, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(exchange, routingKey, message, null, null, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(string exchange, string routingKey, object message, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(exchange, routingKey, message, null, correlationData, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(exchange, routingKey, message, messagePostProcessor, null, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            var messageToSend = ConvertMessageIfNecessary(message);
            if (messagePostProcessor != null)
            {
                messageToSend = messagePostProcessor.PostProcessMessage(messageToSend, correlationData);
            }

            return SendAsync(exchange, routingKey, messageToSend, correlationData, cancellationToken);
        }

        #endregion RabbitConvertAndSend

        #region RabbitReceive
        public virtual Task<IMessage> ReceiveAsync(string queueName, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => DoReceive(queueName, ReceiveTimeout, cancellationToken), cancellationToken);
        }

        public virtual Task<IMessage> ReceiveAsync(int timeoutMillis, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => DoReceive(GetRequiredQueue(), timeoutMillis, cancellationToken), cancellationToken);
        }

        public virtual Task<IMessage> ReceiveAsync(string queueName, int timeoutMillis, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => DoReceive(queueName, timeoutMillis, cancellationToken), cancellationToken);
        }

        public virtual IMessage Receive(int timeoutMillis)
        {
            return Receive(GetRequiredQueue(), timeoutMillis);
        }

        public virtual IMessage Receive(string queueName)
        {
            return Receive(queueName, ReceiveTimeout);
        }

        public virtual IMessage Receive(string queueName, int timeoutMillis)
        {
            if (timeoutMillis == 0)
            {
                return DoReceiveNoWait(queueName);
            }
            else
            {
                return DoReceive(queueName, timeoutMillis, default);
            }
        }

        #endregion RabbitReceive

        #region RabbitReceiveAndConvert

        public virtual T ReceiveAndConvert<T>(int timeoutMillis)
        {
            return (T)ReceiveAndConvert(GetRequiredQueue(), timeoutMillis, typeof(T));
        }

        public virtual T ReceiveAndConvert<T>(string queueName)
        {
            return (T)ReceiveAndConvert(queueName, ReceiveTimeout, typeof(T));
        }

        public virtual T ReceiveAndConvert<T>(string queueName, int timeoutMillis)
        {
            return (T)ReceiveAndConvert(queueName, timeoutMillis, typeof(T));
        }

        public virtual object ReceiveAndConvert(Type type)
        {
            return ReceiveAndConvert(GetRequiredQueue(), ReceiveTimeout, type);
        }

        public virtual object ReceiveAndConvert(string queueName, Type type)
        {
            return ReceiveAndConvert(queueName, ReceiveTimeout, type);
        }

        public virtual object ReceiveAndConvert(int timeoutMillis, Type type)
        {
            return ReceiveAndConvert(GetRequiredQueue(), timeoutMillis, type);
        }

        public virtual object ReceiveAndConvert(string queueName, int timeoutMillis, Type type)
        {
            return DoReceiveAndConvert(queueName, timeoutMillis, type, default);
        }

        public virtual Task<T> ReceiveAndConvertAsync<T>(int timeoutMillis, CancellationToken cancellationToken = default)
        {
            return ReceiveAndConvertAsync<T>(GetRequiredQueue(), timeoutMillis, cancellationToken);
        }

        public virtual Task<T> ReceiveAndConvertAsync<T>(string queueName, CancellationToken cancellationToken = default)
        {
            return ReceiveAndConvertAsync<T>(queueName, ReceiveTimeout, cancellationToken);
        }

        public virtual Task<T> ReceiveAndConvertAsync<T>(string queueName, int timeoutMillis, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                return (T)DoReceiveAndConvert(queueName, timeoutMillis, typeof(T), cancellationToken);
            });
        }

        public virtual Task<object> ReceiveAndConvertAsync(Type type, CancellationToken cancellation = default)
        {
            return ReceiveAndConvertAsync(GetRequiredQueue(), ReceiveTimeout, type, cancellation);
        }

        public virtual Task<object> ReceiveAndConvertAsync(string queueName, Type type, CancellationToken cancellationToken = default)
        {
            return ReceiveAndConvertAsync(queueName, ReceiveTimeout, type, cancellationToken);
        }

        public virtual Task<object> ReceiveAndConvertAsync(int timeoutMillis, Type type, CancellationToken cancellationToken = default)
        {
            return ReceiveAndConvertAsync(GetRequiredQueue(), timeoutMillis, type, cancellationToken);
        }

        public virtual Task<object> ReceiveAndConvertAsync(string queueName, int timeoutMillis, Type type, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                return DoReceiveAndConvert(queueName, timeoutMillis, type, cancellationToken);
            });
        }

        #endregion RabbitReceiveAndConvert

        #region RabbitReceiveAndReply
        public virtual bool ReceiveAndReply<R, S>(Func<R, S> callback)
        {
            return ReceiveAndReply(GetRequiredQueue(), callback);
        }

        public virtual bool ReceiveAndReply<R, S>(string queueName, Func<R, S> callback)
        {
            return ReceiveAndReply(queueName, callback, (request, replyto) => GetReplyToAddress(request));
        }

        public virtual bool ReceiveAndReply<R, S>(Func<R, S> callback, string exchange, string routingKey)
        {
            return ReceiveAndReply(GetRequiredQueue(), callback, exchange, routingKey);
        }

        public virtual bool ReceiveAndReply<R, S>(string queueName, Func<R, S> callback, string replyExchange, string replyRoutingKey)
        {
            return ReceiveAndReply(queueName, callback, (request, reply) => new Address(replyExchange, replyRoutingKey));
        }

        public virtual bool ReceiveAndReply<R, S>(Func<R, S> callback, Func<IMessage, S, Address> replyToAddressCallback)
        {
            return ReceiveAndReply(GetRequiredQueue(), callback, replyToAddressCallback);
        }

        public virtual bool ReceiveAndReply<R, S>(string queueName, Func<R, S> callback, Func<IMessage, S, Address> replyToAddressCallback)
        {
            return DoReceiveAndReply(queueName, callback, replyToAddressCallback);
        }
        #endregion RabbitReceiveAndReply

        #region RabbitSendAndReceive
        public virtual IMessage SendAndReceive(IMessage message, CorrelationData correlationData)
        {
            return DoSendAndReceive(GetDefaultExchange(), GetDefaultRoutingKey(), message, correlationData, default);
        }

        public virtual IMessage SendAndReceive(string routingKey, IMessage message)
        {
            return DoSendAndReceive(GetDefaultExchange(), routingKey, message, null, default);
        }

        public virtual IMessage SendAndReceive(string routingKey, IMessage message, CorrelationData correlationData)
        {
            return DoSendAndReceive(GetDefaultExchange(), routingKey, message, correlationData, default);
        }

        public virtual IMessage SendAndReceive(string exchange, string routingKey, IMessage message)
        {
            return DoSendAndReceive(exchange, routingKey, message, null, default);
        }

        public virtual IMessage SendAndReceive(string exchange, string routingKey, IMessage message, CorrelationData correlationData)
        {
            return DoSendAndReceive(exchange, routingKey, message, correlationData, default);
        }

        public virtual Task<IMessage> SendAndReceiveAsync(IMessage message, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return SendAndReceiveAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, correlationData, cancellationToken);
        }

        public virtual Task<IMessage> SendAndReceiveAsync(string routingKey, IMessage message, CancellationToken cancellationToken = default)
        {
            return SendAndReceiveAsync(GetDefaultExchange(), routingKey, message, null, cancellationToken);
        }

        public virtual Task<IMessage> SendAndReceiveAsync(string routingKey, IMessage message, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return SendAndReceiveAsync(GetDefaultExchange(), routingKey, message, null, cancellationToken);
        }

        public virtual Task<IMessage> SendAndReceiveAsync(string exchange, string routingKey, IMessage message, CancellationToken cancellationToken = default)
        {
            return SendAndReceiveAsync(exchange, routingKey, message, null, cancellationToken);
        }

        public virtual Task<IMessage> SendAndReceiveAsync(string exchange, string routingKey, IMessage message, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => DoSendAndReceive(exchange, routingKey, message, correlationData, cancellationToken), cancellationToken);
        }

        #endregion RabbitSendAndReceive

        #region RabbitConvertSendAndReceive

        public virtual T ConvertSendAndReceive<T>(object message, CorrelationData correlationData)
        {
            return (T)ConvertSendAndReceiveAsType(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, correlationData, typeof(T));
        }

        public virtual T ConvertSendAndReceive<T>(object message, IMessagePostProcessor messagePostProcessor)
        {
            return (T)ConvertSendAndReceiveAsType(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, null, typeof(T));
        }

        public virtual T ConvertSendAndReceive<T>(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            return (T)ConvertSendAndReceiveAsType(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, correlationData, typeof(T));
        }

        public virtual T ConvertSendAndReceive<T>(string routingKey, object message)
        {
            return (T)ConvertSendAndReceiveAsType(GetDefaultExchange(), routingKey, message, null, null, typeof(T));
        }

        public virtual T ConvertSendAndReceive<T>(string routingKey, object message, CorrelationData correlationData)
        {
            return (T)ConvertSendAndReceiveAsType(GetDefaultExchange(), routingKey, message, null, correlationData, typeof(T));
        }

        public virtual T ConvertSendAndReceive<T>(string routingKey, object message, IMessagePostProcessor messagePostProcessor)
        {
            return (T)ConvertSendAndReceiveAsType(GetDefaultExchange(), routingKey, message, messagePostProcessor, null, typeof(T));
        }

        public virtual T ConvertSendAndReceive<T>(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            return (T)ConvertSendAndReceiveAsType(GetDefaultExchange(), routingKey, message, messagePostProcessor, correlationData, typeof(T));
        }

        public virtual T ConvertSendAndReceive<T>(string exchange, string routingKey, object message)
        {
            return (T)ConvertSendAndReceiveAsType(exchange, routingKey, message, null, null, typeof(T));
        }

        public virtual T ConvertSendAndReceive<T>(string exchange, string routingKey, object message, CorrelationData correlationData)
        {
            return (T)ConvertSendAndReceiveAsType(exchange, routingKey, message, null, correlationData, typeof(T));
        }

        public virtual T ConvertSendAndReceive<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor)
        {
            return (T)ConvertSendAndReceiveAsType(exchange, routingKey, message, messagePostProcessor, null, typeof(T));
        }

        public virtual T ConvertSendAndReceive<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            return (T)ConvertSendAndReceiveAsType(exchange, routingKey, message, messagePostProcessor, correlationData, typeof(T));
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(object message, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, correlationData, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(object message, IMessagePostProcessor messagePostProcessor, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, null, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, correlationData, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(string routingKey, object message, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), routingKey, message, null, null, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(string routingKey, object message, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), routingKey, message, null, correlationData, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), routingKey, message, messagePostProcessor, null, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), routingKey, message, messagePostProcessor, correlationData, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(string exchange, string routingKey, object message, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(exchange, routingKey, message, null, null, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(string exchange, string routingKey, object message, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(exchange, routingKey, message, null, correlationData, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(exchange, routingKey, message, messagePostProcessor, null, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, CancellationToken cancellationToken = default)
        {
            return Task.Run(
            () =>
            {
                return (T)ConvertSendAndReceiveAsType(exchange, routingKey, message, messagePostProcessor, correlationData, typeof(T));
            }, cancellationToken);
        }

        public virtual object ConvertSendAndReceiveAsType(object message, Type type)
        {
            return ConvertSendAndReceiveAsType(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, null, type);
        }

        public virtual object ConvertSendAndReceiveAsType(object message, CorrelationData correlationData, Type type)
        {
            return ConvertSendAndReceiveAsType(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, correlationData, type);
        }

        public virtual object ConvertSendAndReceiveAsType(object message, IMessagePostProcessor messagePostProcessor, Type type)
        {
            return ConvertSendAndReceiveAsType(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, null, type);
        }

        public virtual object ConvertSendAndReceiveAsType(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type type)
        {
            return ConvertSendAndReceiveAsType(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, correlationData, type);
        }

        public virtual object ConvertSendAndReceiveAsType(string routingKey, object message, Type type)
        {
            return ConvertSendAndReceiveAsType(GetDefaultExchange(), routingKey, message, null, null, type);
        }

        public virtual object ConvertSendAndReceiveAsType(string routingKey, object message, CorrelationData correlationData, Type type)
        {
            return ConvertSendAndReceiveAsType(GetDefaultExchange(), routingKey, message, null, correlationData, type);
        }

        public virtual object ConvertSendAndReceiveAsType(string routingKey, object message, IMessagePostProcessor messagePostProcessor, Type type)
        {
            return ConvertSendAndReceiveAsType(GetDefaultExchange(), routingKey, message, messagePostProcessor, null, type);
        }

        public virtual object ConvertSendAndReceiveAsType(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type type)
        {
            return ConvertSendAndReceiveAsType(GetDefaultExchange(), routingKey, message, messagePostProcessor, correlationData, type);
        }

        public virtual object ConvertSendAndReceiveAsType(string exchange, string routingKey, object message, Type type)
        {
            return ConvertSendAndReceiveAsType(exchange, routingKey, message, null, null, type);
        }

        public virtual object ConvertSendAndReceiveAsType(string exchange, string routingKey, object message, CorrelationData correlationData, Type type)
        {
            return ConvertSendAndReceiveAsType(exchange, routingKey, message, null, correlationData, type);
        }

        public virtual object ConvertSendAndReceiveAsType(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, Type type)
        {
            return ConvertSendAndReceiveAsType(exchange, routingKey, message, messagePostProcessor, null, type);
        }

        public virtual object ConvertSendAndReceiveAsType(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type type)
        {
            var replyMessage = ConvertSendAndReceiveRaw(exchange, routingKey, message, messagePostProcessor, correlationData);
            if (replyMessage == null)
            {
                return default;
            }

            var value = GetRequiredSmartMessageConverter().FromMessage(replyMessage, type);

            if (value is Exception && ThrowReceivedExceptions)
            {
                throw (Exception)value;
            }

            return value;
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(object message, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, null, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(object message, CorrelationData correlationData, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, correlationData, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(object message, IMessagePostProcessor messagePostProcessor, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, null, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, correlationData, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string routingKey, object message, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), routingKey, message, null, null, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string routingKey, object message, CorrelationData correlationData, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), routingKey, message, null, correlationData, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string routingKey, object message, IMessagePostProcessor messagePostProcessor, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), routingKey, message, messagePostProcessor, null, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), routingKey, message, messagePostProcessor, correlationData, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string exchange, string routingKey, object message, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(exchange, routingKey, message, null, null, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string exchange, string routingKey, object message, CorrelationData correlationData, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(exchange, routingKey, message, null, correlationData, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, Type type, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsTypeAsync(exchange, routingKey, message, messagePostProcessor, null, type, cancellationToken);
        }

        public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData, Type type, CancellationToken cancellationToken = default)
        {
            return Task.Run(
            () =>
            {
                var replyMessage = ConvertSendAndReceiveRaw(exchange, routingKey, message, messagePostProcessor, correlationData);
                if (replyMessage == null)
                {
                    return default;
                }

                var value = GetRequiredSmartMessageConverter().FromMessage(replyMessage, type);

                if (value is Exception && ThrowReceivedExceptions)
                {
                    throw (Exception)value;
                }

                return value;
            }, cancellationToken);
        }

        #endregion RabbitConvertSendAndReceive

        #region General

        public virtual void CorrelationConvertAndSend(object message, CorrelationData correlationData)
        {
            ConvertAndSend(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, correlationData);
        }

        public virtual ICollection<CorrelationData> GetUnconfirmed(long age)
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

        public virtual int GetUnconfirmedCount()
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

        public virtual void Execute(Action<IModel> action)
        {
            _ = Execute<object>(
                (channel) =>
            {
                action(channel);
                return null;
            }, ConnectionFactory);
        }

        public virtual T Execute<T>(Func<IModel, T> action)
        {
            return Execute(action, ConnectionFactory);
        }

        public virtual void AddListener(IModel channel)
        {
            if (channel is IPublisherCallbackChannel publisherCallbackChannel)
            {
                var key = channel is IChannelProxy ? ((IChannelProxy)channel).TargetChannel : channel;
                if (_publisherConfirmChannels.TryAdd(key, this))
                {
                    publisherCallbackChannel.AddListener(this);
                    _logger?.LogDebug("Added publisher confirm channel: {channel} to map, size now {size}", channel, _publisherConfirmChannels.Count);
                }
            }
            else
            {
                throw new InvalidOperationException("Channel does not support confirms or returns; is the connection factory configured for confirms or returns?");
            }
        }

        public virtual T Invoke<T>(Func<IRabbitTemplate, T> rabbitOperations)
        {
            return Invoke<T>(rabbitOperations, null, null);
        }

        public virtual T Invoke<T>(Func<IRabbitTemplate, T> rabbitOperations, Action<object, BasicAckEventArgs> acks, Action<object, BasicNackEventArgs> nacks)
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
                    _logger?.LogError(e, "Exception thrown while creating channel");
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

        public virtual bool WaitForConfirms(int timeoutInMilliseconds)
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
                _logger?.LogError(e, "Exception thrown during WaitForConfirms");
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
        }

        public virtual void WaitForConfirmsOrDie(int timeoutInMilliseconds)
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
                _logger?.LogError(e, "Exception thrown during WaitForConfirmsOrDie");
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
        }

        public virtual void DetermineConfirmsReturnsCapability(Connection.IConnectionFactory connectionFactory)
        {
            _publisherConfirms = connectionFactory.IsPublisherConfirms;
            _confirmsOrReturnsCapable = _publisherConfirms || connectionFactory.IsPublisherReturns;
        }

        public virtual bool IsMandatoryFor(IMessage message)
        {
            return MandatoryExpression.GetValue<bool>(EvaluationContext, message);
        }

        public virtual void Dispose()
        {
            Stop().Wait();
        }

        public virtual async Task Start()
        {
            await DoStart();
        }

        public virtual async Task Stop()
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

        #endregion General

        #endregion Public

        #region Protected
        protected internal virtual IMessage ConvertMessageIfNecessary(object message)
        {
            if (message is IMessage<byte[]>)
            {
                return (IMessage)message;
            }

            return GetRequiredMessageConverter().ToMessage(message, new MessageHeaders());
        }

        protected internal virtual IMessage DoSendAndReceiveWithTemporary(string exchange, string routingKey, IMessage message, CorrelationData correlationData, CancellationToken cancellationToken)
        {
            return Execute(
                channel =>
                {
                    if (message.Headers.ReplyTo() != null)
                    {
                        throw new ArgumentException("Send-and-receive methods can only be used if the Message does not already have a replyTo property.");
                    }

                    cancellationToken.ThrowIfCancellationRequested();

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

                    var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                    accessor.ReplyTo = replyTo;

                    var consumerTag = Guid.NewGuid().ToString();

                    var consumer = new DoSendAndReceiveTemplateConsumer(this, channel, pendingReply);

                    channel.ModelShutdown += (sender, args) =>
                    {
                        if (!RabbitUtils.IsNormalChannelClose(args))
                        {
                            var exception = new ShutdownSignalException(args);
                            pendingReply.CompleteExceptionally(exception);
                        }
                    };

                    channel.BasicConsume(replyTo, true, consumerTag, NoLocalReplyConsumer, true, null, consumer);
                    IMessage reply = null;
                    try
                    {
                        reply = ExchangeMessages(exchange, routingKey, message, correlationData, channel, pendingReply, messageTag, cancellationToken);
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

        protected virtual object DoReceiveAndConvert(string queueName, int timeoutMillis, Type type, CancellationToken cancellationToken = default)
        {
            var response = timeoutMillis == 0 ? DoReceiveNoWait(queueName) : DoReceive(queueName, timeoutMillis, cancellationToken);
            if (response != null)
            {
                return GetRequiredSmartMessageConverter().FromMessage(response, type);
            }

            return default;
        }

        protected virtual IMessage DoReceive(string queueName, int timeoutMillis, CancellationToken cancellationToken)
        {
            var message = Execute(
                channel =>
                {
                    var delivery = ConsumeDelivery(channel, queueName, timeoutMillis, cancellationToken);
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

        protected override IMessage DoReceive(RabbitDestination destination)
        {
            if (ReceiveTimeout == 0)
            {
                return DoReceiveNoWait(destination.QueueName);
            }
            else
            {
                return DoReceive(destination.QueueName, ReceiveTimeout, default);
            }
        }

        protected override Task<IMessage> DoReceiveAsync(RabbitDestination destination, CancellationToken cancellationToken)
        {
            return ReceiveAsync(destination.QueueName, cancellationToken);
        }

        protected override Task DoSendAsync(RabbitDestination destination, IMessage message, CancellationToken cancellationToken)
        {
            return SendAsync(destination.ExchangeName, destination.RoutingKey, message, cancellationToken);
        }

        protected override Task<IMessage> DoSendAndReceiveAsync(RabbitDestination destination, IMessage requestMessage, CancellationToken cancellationToken = default)
        {
            return SendAndReceiveAsync(destination.ExchangeName, destination.RoutingKey, requestMessage, cancellationToken);
        }

        protected override IMessage DoSendAndReceive(RabbitDestination destination, IMessage requestMessage)
        {
            return DoSendAndReceive(destination.ExchangeName, destination.RoutingKey, requestMessage, null, default);
        }

        protected virtual IMessage DoSendAndReceive(string exchange, string routingKey, IMessage message, CorrelationData correlationData, CancellationToken cancellationToken)
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
                return DoSendAndReceiveWithDirect(exchange, routingKey, message, correlationData, cancellationToken);
            }
            else if (ReplyAddress == null || _usingFastReplyTo)
            {
                return DoSendAndReceiveWithTemporary(exchange, routingKey, message, correlationData, cancellationToken);
            }
            else
            {
                return DoSendAndReceiveWithFixed(exchange, routingKey, message, correlationData, cancellationToken);
            }
        }

        protected virtual IMessage DoSendAndReceiveWithFixed(string exchange, string routingKey, IMessage message, CorrelationData correlationData, CancellationToken cancellationToken)
        {
            if (!_isListener)
            {
                throw new InvalidOperationException("RabbitTemplate is not configured as MessageListener - cannot use a 'replyAddress': " + ReplyAddress);
            }

            return Execute(
                channel =>
                {
                    return DoSendAndReceiveAsListener(exchange, routingKey, message, correlationData, channel, cancellationToken);
                }, ObtainTargetConnectionFactory(SendConnectionFactorySelectorExpression, message));
        }

        protected virtual IMessage DoSendAndReceiveWithDirect(string exchange, string routingKey, IMessage message, CorrelationData correlationData, CancellationToken cancellationToken = default)
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
                        container.ServiceName = ServiceName + "#" + Interlocked.Increment(ref _containerInstance);

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
                        _replyAddress = Address.AMQ_RABBITMQ_REPLY_TO;
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

                return DoSendAndReceiveAsListener(exchange, routingKey, message, correlationData, channel, cancellationToken);
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

        protected virtual IMessage DoReceiveNoWait(string queueName, CancellationToken cancellationToken = default)
        {
            var message = Execute(
                channel =>
                {
                    if (!cancellationToken.IsCancellationRequested)
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
                    }

                    return null;
                }, ObtainTargetConnectionFactory(ReceiveConnectionFactorySelectorExpression, queueName));

            LogReceived(message);
            return message;
        }

        protected virtual void DoSend(IModel channel, string exchangeArg, string routingKeyArg, IMessage message, bool mandatory, CorrelationData correlationData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var exch = exchangeArg;
            var rKey = routingKeyArg;
            if (exch == null)
            {
                exch = GetDefaultExchange();
            }

            if (rKey == null)
            {
                rKey = GetDefaultRoutingKey();
            }

            _logger?.LogTrace("Original message to publish: {message}", message);

            var messageToUse = message;
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(messageToUse);
            if (mandatory)
            {
                accessor.SetHeader(PublisherCallbackChannel.RETURN_LISTENER_CORRELATION_KEY, UUID);
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
            if (UserIdExpression != null && accessor.UserId == null)
            {
                var userId = UserIdExpression.GetValue<string>(EvaluationContext, messageToUse);
                if (userId != null)
                {
                    accessor.UserId = userId;
                }
            }

            _logger?.LogDebug("Publishing message [{message}] on exchange [{exchange}], routingKey = [{routingKey}]", messageToUse, exch, rKey);
            SendToRabbit(channel, exch, rKey, mandatory, messageToUse);

            // Check if commit needed
            if (IsChannelLocallyTransacted(channel))
            {
                // Transacted channel created by this template -> commit.
                RabbitUtils.CommitIfNecessary(channel);
            }
        }

        protected override void DoSend(RabbitDestination destination, IMessage message)
        {
            Send(destination.ExchangeName, destination.RoutingKey, message, null);
        }

        protected virtual void SendToRabbit(IModel channel, string exchange, string routingKey, bool mandatory, IMessage message)
        {
            byte[] body = message.Payload as byte[];
            if (body == null)
            {
                // TODO: If content type is byte but payload is string .. do conversion?
                throw new InvalidOperationException("Unable to publish IMessage, payload must be a byte[]");
            }

            var convertedMessageProperties = channel.CreateBasicProperties();
            MessagePropertiesConverter.FromMessageHeaders(message.Headers, convertedMessageProperties, Encoding);
            channel.BasicPublish(exchange, routingKey, mandatory, convertedMessageProperties, body);
        }

        protected virtual bool IsChannelLocallyTransacted(IModel channel)
        {
            return IsChannelTransacted && !ConnectionFactoryUtils.IsChannelTransactional(channel, ConnectionFactory);
        }

        protected virtual Connection.IConnection CreateConnection()
        {
            return ConnectionFactory.CreateConnection();
        }

        protected virtual RabbitResourceHolder GetTransactionalResourceHolder()
        {
            return ConnectionFactoryUtils.GetTransactionalResourceHolder(ConnectionFactory, IsChannelTransacted);
        }

        protected virtual Exception ConvertRabbitAccessException(Exception ex)
        {
            return RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
        }

        protected IMessage ConvertSendAndReceiveRaw(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData)
        {
            var requestMessage = ConvertMessageIfNecessary(message);
            if (messagePostProcessor != null)
            {
                requestMessage = messagePostProcessor.PostProcessMessage(requestMessage, correlationData);
            }

            return DoSendAndReceive(exchange, routingKey, requestMessage, correlationData, default);
        }

        protected virtual string GetDefaultExchange()
        {
            if (DefaultSendDestination != null)
            {
                return DefaultSendDestination.ExchangeName;
            }

            return DEFAULT_EXCHANGE;
        }

        protected virtual string GetDefaultRoutingKey()
        {
            if (DefaultSendDestination != null)
            {
                return DefaultSendDestination.RoutingKey;

                // var dest = ParseDestination(DefaultSendDestination);
                // return dest.Item2;
            }

            return DEFAULT_ROUTING_KEY;
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
                catch (RabbitException ex) when (ex is RabbitConnectException || ex is RabbitIOException)
                {
                    if (ShouldRethrow(ex))
                    {
                        throw;
                    }
                }
            }

            return false;
        }

        protected virtual void ReplyTimedOut(string correlationId)
        {
        }

        protected virtual Task DoStart()
        {
            return Task.CompletedTask;
        }

        protected virtual Task DoStop()
        {
            return Task.CompletedTask;
        }

        #endregion Protected

        #region Private
        private void Configure(RabbitOptions options)
        {
            if (options == null)
            {
                return;
            }

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

            DefaultSendDestination = templateOptions.Exchange + "/" + templateOptions.RoutingKey;
            DefaultReceiveDestination = templateOptions.DefaultReceiveQueue;
        }

        private void RestoreProperties(IMessage message, PendingReply pendingReply)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            if (!UseCorrelationId)
            {
                // Restore the inbound correlation data
                var savedCorrelation = pendingReply.SavedCorrelation;
                if (CorrelationKey == null)
                {
                    accessor.CorrelationId = savedCorrelation;
                }
                else
                {
                    if (savedCorrelation != null)
                    {
                        accessor.SetHeader(CorrelationKey, savedCorrelation);
                    }
                    else
                    {
                        accessor.RemoveHeader(CorrelationKey);
                    }
                }
            }

            // Restore any inbound replyTo
            var savedReplyTo = pendingReply.SavedReplyTo;
            accessor.ReplyTo = savedReplyTo;
            if (savedReplyTo != null)
            {
                _logger?.LogDebug("Restored replyTo to: {replyTo} ", savedReplyTo);
            }
        }

        private IMessage BuildMessageFromDelivery(Delivery delivery)
        {
            return BuildMessage(delivery.Envelope, delivery.Properties, delivery.Body, null);
        }

        private IMessage BuildMessageFromResponse(BasicGetResult response)
        {
            return BuildMessage(new Envelope(response.DeliveryTag, response.Redelivered, response.Exchange, response.RoutingKey), response.BasicProperties, response.Body, response.MessageCount);
        }

        private IMessage BuildMessage(Envelope envelope, IBasicProperties properties, byte[] body, uint? msgCount)
        {
            var messageProps = MessagePropertiesConverter.ToMessageHeaders(properties, envelope, Encoding);
            if (msgCount.HasValue)
            {
                var accessor = RabbitHeaderAccessor.GetAccessor<RabbitHeaderAccessor>(messageProps);
                accessor.MessageCount = msgCount.Value;
            }

            IMessage message = Message.Create(body, messageProps);
            if (AfterReceivePostProcessors != null)
            {
                var processors = AfterReceivePostProcessors;
                var postProcessed = message;
                foreach (var processor in processors)
                {
                    postProcessed = processor.PostProcessMessage(postProcessed);
                }

                message = postProcessed;
            }

            return message;
        }

        private IMessageConverter GetRequiredMessageConverter()
        {
            var converter = MessageConverter;
            if (converter == null)
            {
                throw new RabbitIllegalStateException("No 'messageConverter' specified. Check configuration of RabbitTemplate.");
            }

            return converter;
        }

        private ISmartMessageConverter GetRequiredSmartMessageConverter()
        {
            if (!(GetRequiredMessageConverter() is ISmartMessageConverter converter))
            {
                throw new RabbitIllegalStateException("template's message converter must be a SmartMessageConverter");
            }

            return converter;
        }

        private string GetRequiredQueue()
        {
            var name = DefaultReceiveDestination;
            if (name == null)
            {
                throw new RabbitIllegalStateException("No 'queue' specified. Check configuration of RabbitTemplate.");
            }

            return name;
        }

        private Address GetReplyToAddress(IMessage request)
        {
            var replyTo = request.Headers.ReplyToAddress();
            if (replyTo == null)
            {
                var exchange = GetDefaultExchange();
                var routingKey = GetDefaultRoutingKey();
                if (exchange == null)
                {
                    throw new RabbitException("Cannot determine ReplyTo message property value: Request message does not contain reply-to property, and no default Exchange was set.");
                }

                replyTo = new Address(exchange, routingKey);
            }

            return replyTo;
        }

        private void SetupConfirm(IModel channel, IMessage message, CorrelationData correlationDataArg)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            if ((_publisherConfirms || ConfirmCallback != null) && channel is IPublisherCallbackChannel)
            {
                var publisherCallbackChannel = (IPublisherCallbackChannel)channel;
                var correlationData = CorrelationDataPostProcessor != null ? CorrelationDataPostProcessor.PostProcess(message, correlationDataArg) : correlationDataArg;
                var nextPublishSeqNo = channel.NextPublishSeqNo;
                accessor.PublishSequenceNumber = nextPublishSeqNo;
                publisherCallbackChannel.AddPendingConfirm(this, nextPublishSeqNo, new PendingConfirm(correlationData, DateTimeOffset.Now.ToUnixTimeMilliseconds()));
                if (correlationData != null && !string.IsNullOrEmpty(correlationData.Id))
                {
                    accessor.SetHeader(PublisherCallbackChannel.RETURNED_MESSAGE_CORRELATION_KEY, correlationData.Id);
                }
            }
            else if (channel is IChannelProxy && ((IChannelProxy)channel).IsConfirmSelected)
            {
                var nextPublishSeqNo = channel.NextPublishSeqNo;
                accessor.PublishSequenceNumber = nextPublishSeqNo;
            }
        }

        private IMessage DoSendAndReceiveAsListener(string exchange, string routingKey, IMessage message, CorrelationData correlationData, IModel channel, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pendingReply = new PendingReply();
            var messageTag = Interlocked.Increment(ref _messageTagProvider).ToString();
            if (UseCorrelationId)
            {
                object correlationId;
                if (CorrelationKey != null)
                {
                    correlationId = message.Headers.Get<object>(CorrelationKey);
                }
                else
                {
                    correlationId = message.Headers.CorrelationId();
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

            _logger?.LogDebug("Sending message with tag {tag}", messageTag);
            IMessage reply = null;
            try
            {
                reply = ExchangeMessages(exchange, routingKey, message, correlationData, channel, pendingReply, messageTag, cancellationToken);
                if (reply != null && AfterReceivePostProcessors != null)
                {
                    var processors = AfterReceivePostProcessors;
                    var postProcessed = reply;
                    foreach (var processor in processors)
                    {
                        postProcessed = processor.PostProcessMessage(postProcessed);
                    }

                    reply = postProcessed;
                }
            }
            finally
            {
                _replyHolder.TryRemove(messageTag, out _);
            }

            return reply;
        }

        private void SaveAndSetProperties(IMessage message, PendingReply pendingReply, string messageTag)
        {
            // Save any existing replyTo and correlation data
            var savedReplyTo = message.Headers.ReplyTo();
            pendingReply.SavedReplyTo = savedReplyTo;
            if (!string.IsNullOrEmpty(savedReplyTo))
            {
                _logger?.LogDebug("Replacing replyTo header: {savedReplyTo} in favor of template's configured reply-queue: {replyAddress}", savedReplyTo, ReplyAddress);
            }

            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ReplyTo = ReplyAddress;
            if (!UseCorrelationId)
            {
                object savedCorrelation = null;
                if (CorrelationKey == null)
                {
                    // using standard correlationId property
                    var correlationId = accessor.CorrelationId;
                    if (correlationId != null)
                    {
                        savedCorrelation = correlationId;
                    }
                }
                else
                {
                    savedCorrelation = accessor.GetHeader(CorrelationKey);
                }

                pendingReply.SavedCorrelation = (string)savedCorrelation;
                if (CorrelationKey == null)
                {
                    // using standard correlationId property
                    accessor.CorrelationId = messageTag;
                }
                else
                {
                    accessor.SetHeader(CorrelationKey, messageTag);
                }
            }
        }

        private IMessage ExchangeMessages(string exchange, string routingKey, IMessage message, CorrelationData correlationData, IModel channel, PendingReply pendingReply, string messageTag, CancellationToken cancellationToken)
        {
            IMessage reply;
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            var mandatory = IsMandatoryFor(message);
            if (mandatory && ReturnCallback == null)
            {
                accessor.SetHeader(RETURN_CORRELATION_KEY, messageTag);
            }

            DoSend(channel, exchange, routingKey, message, mandatory, correlationData, cancellationToken);

            reply = ReplyTimeout < 0 ? pendingReply.Get() : pendingReply.Get(ReplyTimeout);
            _logger?.LogDebug("Reply: {reply} ", reply);
            if (reply == null)
            {
                ReplyTimedOut(accessor.CorrelationId);
            }

            return reply;
        }

        private void CancelConsumerQuietly(IModel channel, DefaultBasicConsumer consumer)
        {
            RabbitUtils.Cancel(channel, consumer.ConsumerTag);
        }

        private bool DoReceiveAndReply<R, S>(string queueName, Func<R, S> callback, Func<IMessage, S, Address> replyToAddressCallback)
        {
            var result = Execute(
                channel =>
                {
                    var receiveMessage = ReceiveForReply(queueName, channel, default);
                    if (receiveMessage != null)
                    {
                        return SendReply(callback, replyToAddressCallback, channel, receiveMessage);
                    }

                    return false;
                }, ObtainTargetConnectionFactory(ReceiveConnectionFactorySelectorExpression, queueName));
            return result;
        }

        private IMessage ReceiveForReply(string queueName, IModel channel, CancellationToken cancellationToken)
        {
            var channelTransacted = IsChannelTransacted;
            var channelLocallyTransacted = IsChannelLocallyTransacted(channel);
            IMessage receiveMessage = null;
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
                var delivery = ConsumeDelivery(channel, queueName, ReceiveTimeout, cancellationToken);
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

        private Delivery ConsumeDelivery(IModel channel, string queueName, int timeoutMillis, CancellationToken cancellationToken)
        {
            Delivery delivery = null;
            Exception exception = null;
            var future = new TaskCompletionSource<Delivery>();

            DefaultBasicConsumer consumer = null;
            try
            {
                var consumeTimeout = timeoutMillis < 0 ? DEFAULT_CONSUME_TIMEOUT : timeoutMillis;
                consumer = CreateConsumer(queueName, channel, future, consumeTimeout, cancellationToken);

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

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (AggregateException e)
            {
                var cause = e.InnerExceptions.FirstOrDefault();

                _logger?.LogError(cause, "Consumer {consumer} failed to receive message", consumer);
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

        private void LogReceived(IMessage message)
        {
            if (message == null)
            {
                _logger?.LogDebug("Received no message");
            }
            else
            {
                _logger?.LogDebug("Received: {message}", message);
            }
        }

        private bool SendReply<R, S>(Func<R, S> receiveAndReplyCallback, Func<IMessage, S, Address> replyToAddressCallback, IModel channel, IMessage receiveMessage)
        {
            object receive = receiveMessage;
            if (!typeof(R).IsAssignableFrom(receive.GetType()))
            {
                receive = GetRequiredMessageConverter().FromMessage(receiveMessage, typeof(R));
            }

            if (!(receive is R messageAsR))
            {
                throw new ArgumentException("'receiveAndReplyCallback' can't handle received object '" + receive.GetType() + "'");
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

        private void DoSendReply<S>(Func<IMessage, S, Address> replyToAddressCallback, IModel channel, IMessage receiveMessage, S reply)
        {
            var replyTo = replyToAddressCallback(receiveMessage, reply);

            var replyMessage = ConvertMessageIfNecessary(reply);

            var receiveMessageAccessor = RabbitHeaderAccessor.GetMutableAccessor(receiveMessage);
            var replyMessageAccessor = RabbitHeaderAccessor.GetMutableAccessor(replyMessage);

            object correlation;
            if (CorrelationKey == null)
            {
                correlation = receiveMessageAccessor.CorrelationId;
            }
            else
            {
                correlation = receiveMessageAccessor.GetHeader(CorrelationKey);
            }

            if (CorrelationKey == null || correlation == null)
            {
                // using standard correlationId property
                if (correlation == null)
                {
                    var messageId = receiveMessageAccessor.MessageId;
                    if (messageId != null)
                    {
                        correlation = messageId;
                    }
                }

                replyMessageAccessor.CorrelationId = (string)correlation;
            }
            else
            {
                replyMessageAccessor.SetHeader(CorrelationKey, correlation);
            }

            // 'doSend()' takes care of 'channel.txCommit()'.
            DoSend(channel, replyTo.ExchangeName, replyTo.RoutingKey, replyMessage, ReturnCallback != null && IsMandatoryFor(replyMessage), null, default);
        }

        private DefaultBasicConsumer CreateConsumer(string queueName, IModel channel, TaskCompletionSource<Delivery> future, int timeoutMillis, CancellationToken cancelationToken)
        {
            // TODO: Verify
            channel.BasicQos(0, 1, false);
            var latch = new CountdownEvent(1);
            var consumer = new DefaultTemplateConsumer(channel, latch, future, queueName, cancelationToken);

            // TODO: Verify autoack false
            var consumeResult = channel.BasicConsume(queueName, false, consumer);

            // Waiting for consumeOK, if latch hasn't signaled, then consumeOK response never hit
            if (!latch.Wait(TimeSpan.FromMilliseconds(timeoutMillis)))
            {
                if (channel is IChannelProxy asProxy)
                {
                    asProxy.TargetChannel.Close();
                }

                future.TrySetException(new ConsumeOkNotReceivedException("Blocking receive, consumer failed to consume within  ms: " + timeoutMillis + " for consumer " + consumer));
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
                    _logger?.LogError(e, "Exception executing DoExecute in retry");
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
                        _logger?.LogError(e, "Exception while creating channel");
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

            _logger?.LogDebug("Executing callback on RabbitMQ Channel: {channel}", channel);
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

        private bool ShouldRethrow(RabbitException ex)
        {
            Exception cause = ex;
            while (cause != null && !(cause is ShutdownSignalException) && !(cause is ProtocolException))
            {
                cause = cause.InnerException;
            }

            if (cause != null && RabbitUtils.IsPassiveDeclarationChannelClose(cause))
            {
                _logger?.LogWarning("Broker does not support fast replies via 'amq.rabbitmq.reply-to', temporary " + "queues will be used: " + cause.Message + ".");
                _replyAddress = null;
                return false;
            }

            if (ex != null)
            {
                _logger?.LogDebug(ex, "IO error, deferring directReplyTo detection");
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
        protected internal class PendingReply
        {
            private readonly TaskCompletionSource<IMessage> _future = new TaskCompletionSource<IMessage>();

            public virtual string SavedReplyTo { get; set; }

            public virtual string SavedCorrelation { get; set; }

            public virtual IMessage Get()
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

            public virtual IMessage Get(int timeout)
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

            public virtual void Reply(IMessage reply)
            {
                _future.TrySetResult(reply);
            }

            public virtual void Returned(RabbitMessageReturnedException e)
            {
                CompleteExceptionally(e);
            }

            public virtual void CompleteExceptionally(Exception exception)
            {
                _future.TrySetException(exception);
            }
        }

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
                    .ToMessageHeaders(properties, new Envelope(deliveryTag, redelivered, exchange, routingKey), _template.Encoding);
                var reply = Message.Create(body, messageProperties);
                _template._logger?.LogTrace("Message received {reply}", reply);
                if (_template.AfterReceivePostProcessors != null)
                {
                    var processors = _template.AfterReceivePostProcessors;
                    IMessage postProcessed = reply;
                    foreach (var processor in processors)
                    {
                        postProcessed = processor.PostProcessMessage(postProcessed);
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
            private readonly CancellationToken _cancellationToken;

            public DefaultTemplateConsumer(IModel channel, CountdownEvent latch, TaskCompletionSource<Delivery> completionSource, string queueName, CancellationToken cancelationToken)
                : base(channel)
            {
                _latch = latch;
                _completionSource = completionSource;
                _queueName = queueName;
                _cancellationToken = cancelationToken;
                _cancellationToken.Register(() =>
                {
                    Signal();
                    _completionSource.TrySetCanceled();
                    channel.BasicCancel(ConsumerTag);
                });
            }

            public override void HandleBasicCancel(string consumerTag)
            {
                _completionSource.TrySetException(new ConsumerCancelledException());
                Signal();
            }

            public override void HandleBasicConsumeOk(string consumerTag)
            {
                Signal();
                base.HandleBasicConsumeOk(consumerTag);
            }

            public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, byte[] body)
            {
                base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
                _completionSource.TrySetResult(new Delivery(consumerTag, new Envelope(deliveryTag, redelivered, exchange, routingKey), properties, body, _queueName));
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
                if (!_latch.IsSet)
                {
                    _latch.Signal();
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

            public virtual void Remove()
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

            public virtual void ReturnedMessage(IMessage<byte[]> message, int replyCode, string replyText, string exchange, string routingKey)
            {
                _pendingReply.Returned(new RabbitMessageReturnedException("Message returned", message, replyCode, replyText, exchange, routingKey));
            }
        }

        public interface IReturnCallback
        {
            void ReturnedMessage(IMessage<byte[]> message, int replyCode, string replyText, string exchange, string routingKey);
        }
        #endregion
    }
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
}
