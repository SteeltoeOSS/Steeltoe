// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Impl;
using Steeltoe.Common;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Retry;
using Steeltoe.Common.RetryPolly;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.Support;
using RC = RabbitMQ.Client;
using SimpleMessageConverter = Steeltoe.Messaging.RabbitMQ.Support.Converter.SimpleMessageConverter;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class RabbitTemplate
    : AbstractMessagingTemplate<RabbitDestination>, IRabbitTemplate, IMessageListener, IListenerContainerAware, IPublisherCallbackChannel.IListener, IDisposable
{
    private const string ReturnCorrelationKey = "spring_request_return_correlation";
    private const string DefaultExchange = "";
    private const string DefaultRoutingKey = "";
    private const int DefaultReplyTimeout = 5000;
    private const int DefaultConsumeTimeout = 10000;
    public const string DefaultServiceName = "rabbitTemplate";

    private static readonly SpelExpressionParser Parser = new();

    private readonly RabbitOptions _options;

    internal readonly object Lock = new();
    internal readonly ConcurrentDictionary<RC.IModel, RabbitTemplate> PublisherConfirmChannels = new();
    internal readonly ConcurrentDictionary<string, PendingReply> ReplyHolder = new();
    internal readonly Dictionary<IConnectionFactory, DirectReplyToMessageListenerContainer> DirectReplyToContainers = new();
    internal readonly AsyncLocal<RC.IModel> DedicatedChannels = new();
    internal readonly IOptionsMonitor<RabbitOptions> OptionsMonitor;

    protected readonly ILogger Logger;
    private int _activeTemplateCallbacks;
    private int _messageTagProvider;
    private int _containerInstance;
    private bool _isListener;
    private bool? _confirmsOrReturnsCapable;
    private bool _publisherConfirms;
    private string _replyAddress;
    internal bool EvaluatedFastReplyTo;
    internal bool UsingFastReplyTo;

    protected internal RabbitOptions Options
    {
        get
        {
            if (OptionsMonitor != null)
            {
                return OptionsMonitor.CurrentValue;
            }

            return _options;
        }
    }

    public virtual IConnectionFactory ConnectionFactory { get; set; }

    public virtual bool IsChannelTransacted { get; set; }

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
        get => _replyAddress;
        set
        {
            EvaluatedFastReplyTo = false;
            _replyAddress = value;
        }
    }

    public virtual int ReceiveTimeout { get; set; }

    public virtual int ReplyTimeout { get; set; } = DefaultReplyTimeout;

    public virtual IMessageHeadersConverter MessagePropertiesConverter { get; set; } = new DefaultMessageHeadersConverter();

    public virtual IConfirmCallback ConfirmCallback { get; set; }

    public virtual IReturnCallback ReturnCallback { get; set; }

    public virtual bool Mandatory
    {
        get => MandatoryExpression.GetValue<bool>();
        set => MandatoryExpression = new ValueExpression<bool>(value);
    }

    public virtual IExpression MandatoryExpression { get; set; } = new ValueExpression<bool>(false);

    public virtual string MandatoryExpressionString
    {
        get => MandatoryExpression?.ToString();
        set
        {
            ArgumentGuard.NotNull(value);

            MandatoryExpression = Parser.ParseExpression(value);
        }
    }

    public virtual IExpression SendConnectionFactorySelectorExpression { get; set; }

    public virtual IExpression ReceiveConnectionFactorySelectorExpression { get; set; }

    public virtual string CorrelationKey { get; set; }

    public virtual IEvaluationContext EvaluationContext { get; set; } = new StandardEvaluationContext();

    public virtual IRetryOperation RetryTemplate { get; set; }

    public virtual IRecoveryCallback RecoveryCallback { get; set; }

    public virtual IList<IMessagePostProcessor> BeforePublishPostProcessors { get; internal set; }

    public virtual IList<IMessagePostProcessor> AfterReceivePostProcessors { get; internal set; }

    public virtual ICorrelationDataPostProcessor CorrelationDataPostProcessor { get; set; }

    public virtual bool UseTemporaryReplyQueues { get; set; }

    public virtual bool UseDirectReplyToContainer { get; set; } = true;

    public virtual IExpression UserIdExpression { get; set; }

    public virtual string UserIdExpressionString
    {
        get => UserIdExpression?.ToString();
        set => UserIdExpression = Parser.ParseExpression(value);
    }

    public virtual string ServiceName { get; set; } = DefaultServiceName;

    public virtual bool UserCorrelationId { get; set; }

    public virtual bool UsePublisherConnection { get; set; }

    public virtual bool NoLocalReplyConsumer { get; set; }

    public virtual IErrorHandler ReplyErrorHandler { get; set; }

    public virtual bool IsRunning
    {
        get
        {
            lock (DirectReplyToContainers)
            {
                return DirectReplyToContainers.Values.Any(c => c.IsRunning);
            }
        }
    }

    public virtual string Uuid { get; } = Guid.NewGuid().ToString();

    public virtual bool IsConfirmListener => ConfirmCallback != null;

    public virtual bool IsReturnListener => true;

    [ActivatorUtilitiesConstructor]
    public RabbitTemplate(IOptionsMonitor<RabbitOptions> optionsMonitor, IConnectionFactory connectionFactory, ISmartMessageConverter messageConverter,
        ILogger logger = null)
    {
        OptionsMonitor = optionsMonitor;
        ConnectionFactory = connectionFactory;
        MessageConverter = messageConverter ?? new SimpleMessageConverter();
        Logger = logger;
        Configure(Options);
    }

    public RabbitTemplate(RabbitOptions options, IConnectionFactory connectionFactory, ISmartMessageConverter messageConverter, ILogger logger = null)
    {
        _options = options;
        ConnectionFactory = connectionFactory;
        MessageConverter = messageConverter ?? new SimpleMessageConverter();
        Logger = logger;
        Configure(Options);
    }

    public RabbitTemplate(IOptionsMonitor<RabbitOptions> optionsMonitor, IConnectionFactory connectionFactory, ILogger logger = null)
    {
        OptionsMonitor = optionsMonitor;
        ConnectionFactory = connectionFactory;
        MessageConverter = new SimpleMessageConverter();
        Logger = logger;
        Configure(Options);
    }

    public RabbitTemplate(RabbitOptions options, IConnectionFactory connectionFactory, ILogger logger = null)
    {
        _options = options;
        ConnectionFactory = connectionFactory;
        MessageConverter = new SimpleMessageConverter();
        Logger = logger;
        Configure(Options);
    }

    public RabbitTemplate(IConnectionFactory connectionFactory, ILogger logger = null)
    {
        ConnectionFactory = connectionFactory;
        MessageConverter = new SimpleMessageConverter();
        DefaultSendDestination = $"{string.Empty}/{string.Empty}";
        DefaultReceiveDestination = null;
        Logger = logger;
    }

    public RabbitTemplate(ILogger logger = null)
    {
        MessageConverter = new SimpleMessageConverter();
        DefaultSendDestination = $"{string.Empty}/{string.Empty}";
        DefaultReceiveDestination = null;
        Logger = logger;
    }

    public virtual void SetBeforePublishPostProcessors(params IMessagePostProcessor[] beforePublishPostProcessors)
    {
        ArgumentGuard.NotNull(beforePublishPostProcessors);
        ArgumentGuard.ElementsNotNull(beforePublishPostProcessors);

        var newList = new List<IMessagePostProcessor>(beforePublishPostProcessors);
        MessagePostProcessorUtils.Sort(newList);
        BeforePublishPostProcessors = newList;
    }

    public virtual void AddBeforePublishPostProcessors(params IMessagePostProcessor[] beforePublishPostProcessors)
    {
        ArgumentGuard.NotNull(beforePublishPostProcessors);

        IList<IMessagePostProcessor> existing = BeforePublishPostProcessors;
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
        ArgumentGuard.NotNull(beforePublishPostProcessor);

        IList<IMessagePostProcessor> existing = BeforePublishPostProcessors;

        if (existing != null && existing.Contains(beforePublishPostProcessor))
        {
            var copy = new List<IMessagePostProcessor>(existing);
            bool result = copy.Remove(beforePublishPostProcessor);
            BeforePublishPostProcessors = copy;
            return result;
        }

        return false;
    }

    public virtual void SetAfterReceivePostProcessors(params IMessagePostProcessor[] afterReceivePostProcessors)
    {
        ArgumentGuard.NotNull(afterReceivePostProcessors);
        ArgumentGuard.ElementsNotNull(afterReceivePostProcessors);

        var newList = new List<IMessagePostProcessor>(afterReceivePostProcessors);
        MessagePostProcessorUtils.Sort(newList);
        AfterReceivePostProcessors = newList;
    }

    public virtual void AddAfterReceivePostProcessors(params IMessagePostProcessor[] afterReceivePostProcessors)
    {
        ArgumentGuard.NotNull(afterReceivePostProcessors);

        IList<IMessagePostProcessor> existing = AfterReceivePostProcessors;
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
        ArgumentGuard.NotNull(afterReceivePostProcessor);

        IList<IMessagePostProcessor> existing = AfterReceivePostProcessors;

        if (existing != null && existing.Contains(afterReceivePostProcessor))
        {
            var copy = new List<IMessagePostProcessor>(existing);
            bool result = copy.Remove(afterReceivePostProcessor);
            AfterReceivePostProcessors = copy;
            return result;
        }

        return false;
    }

    public virtual void HandleConfirm(PendingConfirm pendingConfirm, bool ack)
    {
        if (ConfirmCallback != null)
        {
            ConfirmCallback.Confirm(pendingConfirm.CorrelationInfo, ack, pendingConfirm.Cause);
        }
    }

    public virtual void HandleReturn(int replyCode, string replyText, string exchange, string routingKey, RC.IBasicProperties properties, byte[] body)
    {
        IReturnCallback callback = ReturnCallback;

        if (callback == null)
        {
            IMessageHeaders messageProperties = MessagePropertiesConverter.ToMessageHeaders(properties, null, Encoding);
            string messageTagHeader = messageProperties.Get<string>(ReturnCorrelationKey);

            if (messageTagHeader != null)
            {
                string messageTag = messageTagHeader;

                if (ReplyHolder.TryGetValue(messageTag, out PendingReply pendingReply))
                {
                    callback = new PendingReplyReturn(pendingReply);
                }
                else
                {
                    Logger?.LogWarning("Returned request message but caller has timed out");
                }
            }
            else
            {
                Logger?.LogWarning("Returned message but no callback available");
            }
        }

        if (callback != null)
        {
            properties.Headers.Remove(PublisherCallbackChannel.ReturnListenerCorrelationKey);
            IMessageHeaders messageProperties = MessagePropertiesConverter.ToMessageHeaders(properties, null, Encoding);
            IMessage<byte[]> returnedMessage = Message.Create(body, messageProperties);
            callback.ReturnedMessage(returnedMessage, replyCode, replyText, exchange, routingKey);
        }
    }

    public virtual void Revoke(RC.IModel channel)
    {
        PublisherConfirmChannels.Remove(channel, out _);
        Logger?.LogDebug("Removed publisher confirm channel: {channel} from map, size now {size}", channel, PublisherConfirmChannels.Count);
    }

    public virtual void OnMessageBatch(List<IMessage> messages)
    {
        throw new NotSupportedException("This listener does not support message batches");
    }

    public virtual void OnMessage(IMessage message)
    {
        Logger?.LogTrace("Message received {message}", message);
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
            throw new RabbitRejectAndDoNotRequeueException("No correlation header in reply");
        }

        if (!ReplyHolder.TryGetValue((string)messageTag, out PendingReply pendingReply))
        {
            Logger?.LogWarning("Reply received after timeout for {tag}", messageTag);
            throw new RabbitRejectAndDoNotRequeueException("Reply received after timeout");
        }

        RestoreProperties(message, pendingReply);
        pendingReply.Reply(message);
    }

    public virtual List<string> GetExpectedQueueNames()
    {
        _isListener = true;
        List<string> replyQueue = null;

        if (ReplyAddress == null || ReplyAddress == Address.AmqRabbitMQReplyTo)
        {
            throw new InvalidOperationException("A listener container must not be provided when using direct reply-to");
        }

        var address = new Address(ReplyAddress);

        if (address.ExchangeName == string.Empty)
        {
            replyQueue = new List<string>
            {
                address.RoutingKey
            };
        }
        else
        {
            Logger?.LogInformation("Cannot verify reply queue because 'replyAddress' is not a simple queue name: {replyAddress}", ReplyAddress);
        }

        return replyQueue;
    }

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
        bool mandatory = (ReturnCallback != null || (correlationData != null && !string.IsNullOrEmpty(correlationData.Id))) &&
            MandatoryExpression.GetValue<bool>(EvaluationContext, message);

        Execute<object>(channel =>
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

    public virtual Task SendAsync(string exchange, string routingKey, IMessage message, CorrelationData correlationData,
        CancellationToken cancellationToken = default)
    {
        bool mandatory = (ReturnCallback != null || (correlationData != null && !string.IsNullOrEmpty(correlationData.Id))) &&
            MandatoryExpression.GetValue<bool>(EvaluationContext, message);

        return Task.Run(() => Execute<object>(channel =>
        {
            DoSend(channel, exchange, routingKey, message, mandatory, correlationData, cancellationToken);
            return null;
        }, ObtainTargetConnectionFactory(SendConnectionFactorySelectorExpression, message)), cancellationToken);
    }

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

    public virtual void ConvertAndSend(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CorrelationData correlationData)
    {
        IMessage messageToSend = ConvertMessageIfNecessary(message);

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

    public virtual Task ConvertAndSendAsync(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData,
        CancellationToken cancellationToken = default)
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

    public virtual Task ConvertAndSendAsync(string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(GetDefaultExchange(), routingKey, message, messagePostProcessor, null, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(string routingKey, object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData,
        CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(GetDefaultExchange(), routingKey, message, messagePostProcessor, correlationData, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(string exchange, string routingKey, object message, CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(exchange, routingKey, message, null, null, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(string exchange, string routingKey, object message, CorrelationData correlationData,
        CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(exchange, routingKey, message, null, correlationData, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(exchange, routingKey, message, messagePostProcessor, null, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CorrelationData correlationData, CancellationToken cancellationToken = default)
    {
        IMessage messageToSend = ConvertMessageIfNecessary(message);

        if (messagePostProcessor != null)
        {
            messageToSend = messagePostProcessor.PostProcessMessage(messageToSend, correlationData);
        }

        return SendAsync(exchange, routingKey, messageToSend, correlationData, cancellationToken);
    }

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

    public virtual IMessage Receive(int timeoutInMilliseconds)
    {
        return Receive(GetRequiredQueue(), timeoutInMilliseconds);
    }

    public virtual IMessage Receive(string queueName)
    {
        return Receive(queueName, ReceiveTimeout);
    }

    public virtual IMessage Receive(string queueName, int timeoutInMilliseconds)
    {
        if (timeoutInMilliseconds == 0)
        {
            return DoReceiveNoWait(queueName);
        }

        return DoReceive(queueName, timeoutInMilliseconds, default);
    }

    public virtual T ReceiveAndConvert<T>(int timeoutMillis)
    {
        return (T)ReceiveAndConvert(GetRequiredQueue(), timeoutMillis, typeof(T));
    }

    public virtual T ReceiveAndConvert<T>(string queueName)
    {
        return (T)ReceiveAndConvert(queueName, ReceiveTimeout, typeof(T));
    }

    public virtual T ReceiveAndConvert<T>(string queueName, int timeoutInMilliseconds)
    {
        return (T)ReceiveAndConvert(queueName, timeoutInMilliseconds, typeof(T));
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

    public virtual object ReceiveAndConvert(string queueName, int timeoutInMilliseconds, Type type)
    {
        return DoReceiveAndConvert(queueName, timeoutInMilliseconds, type);
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
        return Task.Run(() => (T)DoReceiveAndConvert(queueName, timeoutMillis, typeof(T), cancellationToken));
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
        return Task.Run(() => DoReceiveAndConvert(queueName, timeoutMillis, type, cancellationToken));
    }

    public virtual bool ReceiveAndReply<TReceive, TReply>(Func<TReceive, TReply> callback)
    {
        return ReceiveAndReply(GetRequiredQueue(), callback);
    }

    public virtual bool ReceiveAndReply<TReceive, TReply>(string queueName, Func<TReceive, TReply> callback)
    {
        return ReceiveAndReply(queueName, callback, (request, _) => GetReplyToAddress(request));
    }

    public virtual bool ReceiveAndReply<TReceive, TReply>(Func<TReceive, TReply> callback, string replyExchange, string replyRoutingKey)
    {
        return ReceiveAndReply(GetRequiredQueue(), callback, replyExchange, replyRoutingKey);
    }

    public virtual bool ReceiveAndReply<TReceive, TReply>(string queueName, Func<TReceive, TReply> callback, string replyExchange, string replyRoutingKey)
    {
        return ReceiveAndReply(queueName, callback, (_, _) => new Address(replyExchange, replyRoutingKey));
    }

    public virtual bool ReceiveAndReply<TReceive, TReply>(Func<TReceive, TReply> callback, Func<IMessage, TReply, Address> replyToAddressCallback)
    {
        return ReceiveAndReply(GetRequiredQueue(), callback, replyToAddressCallback);
    }

    public virtual bool ReceiveAndReply<TReceive, TReply>(string queueName, Func<TReceive, TReply> callback,
        Func<IMessage, TReply, Address> replyToAddressCallback)
    {
        return DoReceiveAndReply(queueName, callback, replyToAddressCallback);
    }

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

    public virtual Task<IMessage> SendAndReceiveAsync(string routingKey, IMessage message, CorrelationData correlationData,
        CancellationToken cancellationToken = default)
    {
        return SendAndReceiveAsync(GetDefaultExchange(), routingKey, message, null, cancellationToken);
    }

    public virtual Task<IMessage> SendAndReceiveAsync(string exchange, string routingKey, IMessage message, CancellationToken cancellationToken = default)
    {
        return SendAndReceiveAsync(exchange, routingKey, message, null, cancellationToken);
    }

    public virtual Task<IMessage> SendAndReceiveAsync(string exchange, string routingKey, IMessage message, CorrelationData correlationData,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => DoSendAndReceive(exchange, routingKey, message, correlationData, cancellationToken), cancellationToken);
    }

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

    public virtual T ConvertSendAndReceive<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CorrelationData correlationData)
    {
        return (T)ConvertSendAndReceiveAsType(exchange, routingKey, message, messagePostProcessor, correlationData, typeof(T));
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(object message, CorrelationData correlationData, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, correlationData, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(object message, IMessagePostProcessor messagePostProcessor,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, null, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, correlationData, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string routingKey, object message, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), routingKey, message, null, null, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string routingKey, object message, CorrelationData correlationData,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), routingKey, message, null, correlationData, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), routingKey, message, messagePostProcessor, null, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CorrelationData correlationData, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(GetDefaultExchange(), routingKey, message, messagePostProcessor, correlationData, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string exchange, string routingKey, object message, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(exchange, routingKey, message, null, null, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string exchange, string routingKey, object message, CorrelationData correlationData,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(exchange, routingKey, message, null, correlationData, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(exchange, routingKey, message, messagePostProcessor, null, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CorrelationData correlationData, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => (T)ConvertSendAndReceiveAsType(exchange, routingKey, message, messagePostProcessor, correlationData, typeof(T)),
            cancellationToken);
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

    public virtual object ConvertSendAndReceiveAsType(string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CorrelationData correlationData, Type type)
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

    public virtual object ConvertSendAndReceiveAsType(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CorrelationData correlationData, Type type)
    {
        IMessage replyMessage = ConvertSendAndReceiveRaw(exchange, routingKey, message, messagePostProcessor, correlationData);

        if (replyMessage == null)
        {
            return default;
        }

        object value = GetRequiredSmartMessageConverter().FromMessage(replyMessage, type);

        return value is Exception exception && ThrowReceivedExceptions ? throw exception : value;
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(object message, Type type, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, null, type, cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(object message, CorrelationData correlationData, Type type,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, correlationData, type, cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(object message, IMessagePostProcessor messagePostProcessor, Type type,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, null, type, cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(object message, IMessagePostProcessor messagePostProcessor, CorrelationData correlationData,
        Type type, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), GetDefaultRoutingKey(), message, messagePostProcessor, correlationData, type,
            cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string routingKey, object message, Type type, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), routingKey, message, null, null, type, cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string routingKey, object message, CorrelationData correlationData, Type type,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), routingKey, message, null, correlationData, type, cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string routingKey, object message, IMessagePostProcessor messagePostProcessor, Type type,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), routingKey, message, messagePostProcessor, null, type, cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CorrelationData correlationData, Type type, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(GetDefaultExchange(), routingKey, message, messagePostProcessor, correlationData, type, cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string exchange, string routingKey, object message, Type type,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(exchange, routingKey, message, null, null, type, cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string exchange, string routingKey, object message, CorrelationData correlationData, Type type,
        CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(exchange, routingKey, message, null, correlationData, type, cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        Type type, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsTypeAsync(exchange, routingKey, message, messagePostProcessor, null, type, cancellationToken);
    }

    public virtual Task<object> ConvertSendAndReceiveAsTypeAsync(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CorrelationData correlationData, Type type, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            IMessage replyMessage = ConvertSendAndReceiveRaw(exchange, routingKey, message, messagePostProcessor, correlationData);

            if (replyMessage == null)
            {
                return default;
            }

            object value = GetRequiredSmartMessageConverter().FromMessage(replyMessage, type);

            return value switch
            {
                Exception exception when ThrowReceivedExceptions => throw exception,
                _ => value
            };
        }, cancellationToken);
    }

    public virtual void CorrelationConvertAndSend(object message, CorrelationData correlationData)
    {
        ConvertAndSend(GetDefaultExchange(), GetDefaultRoutingKey(), message, null, correlationData);
    }

    public virtual ICollection<CorrelationData> GetUnconfirmed(long age)
    {
        var unconfirmed = new HashSet<CorrelationData>();
        long cutoffTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - age;

        foreach (RC.IModel channel in PublisherConfirmChannels.Keys)
        {
            if (channel is IPublisherCallbackChannel pubCallbackChan)
            {
                IList<PendingConfirm> confirms = pubCallbackChan.Expire(this, cutoffTime);

                foreach (PendingConfirm confirm in confirms)
                {
                    unconfirmed.Add(confirm.CorrelationInfo);
                }
            }
        }

        return unconfirmed.Count > 0 ? unconfirmed : null;
    }

    public virtual int GetUnconfirmedCount()
    {
        return PublisherConfirmChannels.Keys.Select(m =>
        {
            if (m is IPublisherCallbackChannel pubCallbackChan)
            {
                return pubCallbackChan.GetPendingConfirmsCount(this);
            }

            return 0;
        }).Sum();
    }

    public virtual void Execute(Action<RC.IModel> action)
    {
        _ = Execute<object>(channel =>
        {
            action(channel);
            return null;
        }, ConnectionFactory);
    }

    public virtual T Execute<T>(Func<RC.IModel, T> channelCallback)
    {
        return Execute(channelCallback, ConnectionFactory);
    }

    public virtual void AddListener(RC.IModel channel)
    {
        if (channel is IPublisherCallbackChannel publisherCallbackChannel)
        {
            RC.IModel key = channel is IChannelProxy proxy ? proxy.TargetChannel : channel;

            if (PublisherConfirmChannels.TryAdd(key, this))
            {
                publisherCallbackChannel.AddListener(this);
                Logger?.LogDebug("Added publisher confirm channel: {channel} to map, size now {size}", channel, PublisherConfirmChannels.Count);
            }
        }
        else
        {
            throw new InvalidOperationException("Channel does not support confirms or returns; is the connection factory configured for confirms or returns?");
        }
    }

    public virtual T Invoke<T>(Func<IRabbitTemplate, T> operationsCallback)
    {
        return Invoke(operationsCallback, null, null);
    }

    public virtual T Invoke<T>(Func<IRabbitTemplate, T> operationsCallback, Action<object, BasicAckEventArgs> acks, Action<object, BasicNackEventArgs> nacks)
    {
        RC.IModel currentChannel = DedicatedChannels.Value;

        if (currentChannel != null)
        {
            throw new InvalidOperationException($"Nested invoke() calls are not supported; channel '{currentChannel}' is already associated with this thread");
        }

        Interlocked.Increment(ref _activeTemplateCallbacks);
        RabbitResourceHolder resourceHolder = null;
        IConnection connection = null;
        RC.IModel channel;
        IConnectionFactory connectionFactory = ConnectionFactory;

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
                channel = connection.CreateChannel();

                if (channel == null)
                {
                    throw new InvalidOperationException("Connection returned a null channel");
                }

                if (!connectionFactory.IsPublisherConfirms)
                {
                    RabbitUtils.SetPhysicalCloseRequired(channel, true);
                }

                DedicatedChannels.Value = channel;
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Exception thrown while creating channel");
                RabbitUtils.CloseConnection(connection);
                throw;
            }
        }

        ConfirmListener listener = AddConfirmListener(acks, nacks, channel);

        try
        {
            return operationsCallback(this);
        }
        finally
        {
            CleanUpAfterAction(resourceHolder, connection, channel, listener);
        }
    }

    public virtual bool WaitForConfirms(int timeoutInMilliseconds)
    {
        RC.IModel channel = DedicatedChannels.Value;

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
            Logger?.LogError(e, "Exception thrown during WaitForConfirms");
            throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
        }
    }

    public virtual void WaitForConfirmsOrDie(int timeoutInMilliseconds)
    {
        RC.IModel channel = DedicatedChannels.Value;

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
            Logger?.LogError(e, "Exception thrown during WaitForConfirmsOrDie");
            throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
        }
    }

    public virtual void DetermineConfirmsReturnsCapability(IConnectionFactory connectionFactory)
    {
        _publisherConfirms = connectionFactory.IsPublisherConfirms;
        _confirmsOrReturnsCapable = _publisherConfirms || connectionFactory.IsPublisherReturns;
    }

    public virtual bool IsMandatoryFor(IMessage message)
    {
        return MandatoryExpression.GetValue<bool>(EvaluationContext, message);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopAsync().Wait();
        }
    }

    public virtual async Task StartAsync()
    {
        await DoStartAsync();
    }

    public virtual async Task StopAsync()
    {
        lock (DirectReplyToContainers)
        {
            foreach (DirectReplyToMessageListenerContainer c in DirectReplyToContainers.Values)
            {
                if (c.IsRunning)
                {
                    c.StopAsync();
                }
            }

            DirectReplyToContainers.Clear();
        }

        await DoStopAsync();
    }

    protected internal virtual IMessage ConvertMessageIfNecessary(object message)
    {
        if (message is IMessage<byte[]> byteArrayMessage)
        {
            return byteArrayMessage;
        }

        return GetRequiredMessageConverter().ToMessage(message, new MessageHeaders());
    }

    protected internal virtual IMessage DoSendAndReceiveWithTemporary(string exchange, string routingKey, IMessage message, CorrelationData correlationData,
        CancellationToken cancellationToken)
    {
        return Execute(channel =>
        {
            if (message.Headers.ReplyTo() != null)
            {
                throw new InvalidOperationException(
                    $"Send-and-receive methods can only be used if the message does not already have a {nameof(RabbitMessageHeaders.ReplyTo)} property.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var pendingReply = new PendingReply();
            string messageTag = Interlocked.Increment(ref _messageTagProvider).ToString(CultureInfo.InvariantCulture);
            ReplyHolder.TryAdd(messageTag, pendingReply);
            string replyTo;

            if (UsingFastReplyTo)
            {
                replyTo = Address.AmqRabbitMQReplyTo;
            }
            else
            {
                RC.QueueDeclareOk queueDeclaration = RC.IModelExensions.QueueDeclare(channel);
                replyTo = queueDeclaration.QueueName;
            }

            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ReplyTo = replyTo;

            string consumerTag = Guid.NewGuid().ToString();

            var consumer = new DoSendAndReceiveTemplateConsumer(this, channel, pendingReply);

            channel.ModelShutdown += (_, args) =>
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
                ReplyHolder.TryRemove(messageTag, out _);

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
        IMessage response = timeoutMillis == 0 ? DoReceiveNoWait(queueName) : DoReceive(queueName, timeoutMillis, cancellationToken);

        if (response != null)
        {
            return GetRequiredSmartMessageConverter().FromMessage(response, type);
        }

        return default;
    }

    protected virtual IMessage DoReceive(string queueName, int timeoutMillis, CancellationToken cancellationToken)
    {
        IMessage message = Execute(channel =>
        {
            Delivery delivery = ConsumeDelivery(channel, queueName, timeoutMillis, cancellationToken);

            if (delivery == null)
            {
                return null;
            }

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

        return DoReceive(destination.QueueName, ReceiveTimeout, default);
    }

    protected override Task<IMessage> DoReceiveAsync(RabbitDestination destination, CancellationToken cancellationToken)
    {
        return ReceiveAsync(destination.QueueName, cancellationToken);
    }

    protected override Task DoSendAsync(RabbitDestination destination, IMessage message, CancellationToken cancellationToken)
    {
        return SendAsync(destination.ExchangeName, destination.RoutingKey, message, cancellationToken);
    }

    protected override Task<IMessage> DoSendAndReceiveAsync(RabbitDestination destination, IMessage requestMessage,
        CancellationToken cancellationToken = default)
    {
        return SendAndReceiveAsync(destination.ExchangeName, destination.RoutingKey, requestMessage, cancellationToken);
    }

    protected override IMessage DoSendAndReceive(RabbitDestination destination, IMessage requestMessage)
    {
        return DoSendAndReceive(destination.ExchangeName, destination.RoutingKey, requestMessage, null, default);
    }

    protected virtual IMessage DoSendAndReceive(string exchange, string routingKey, IMessage message, CorrelationData correlationData,
        CancellationToken cancellationToken)
    {
        if (!EvaluatedFastReplyTo)
        {
            lock (Lock)
            {
                if (!EvaluatedFastReplyTo)
                {
                    EvaluateFastReplyTo();
                }
            }
        }

        if (UsingFastReplyTo && UseDirectReplyToContainer)
        {
            return DoSendAndReceiveWithDirect(exchange, routingKey, message, correlationData, cancellationToken);
        }

        if (ReplyAddress == null || UsingFastReplyTo)
        {
            return DoSendAndReceiveWithTemporary(exchange, routingKey, message, correlationData, cancellationToken);
        }

        return DoSendAndReceiveWithFixed(exchange, routingKey, message, correlationData, cancellationToken);
    }

    protected virtual IMessage DoSendAndReceiveWithFixed(string exchange, string routingKey, IMessage message, CorrelationData correlationData,
        CancellationToken cancellationToken)
    {
        if (!_isListener)
        {
            throw new InvalidOperationException($"RabbitTemplate is not configured as MessageListener - cannot use a 'replyAddress': {ReplyAddress}");
        }

        return Execute(channel => DoSendAndReceiveAsListener(exchange, routingKey, message, correlationData, channel, cancellationToken),
            ObtainTargetConnectionFactory(SendConnectionFactorySelectorExpression, message));
    }

    protected virtual IMessage DoSendAndReceiveWithDirect(string exchange, string routingKey, IMessage message, CorrelationData correlationData,
        CancellationToken cancellationToken = default)
    {
        IConnectionFactory connectionFactory = ObtainTargetConnectionFactory(SendConnectionFactorySelectorExpression, message);

        if (UsePublisherConnection && connectionFactory.PublisherConnectionFactory != null)
        {
            connectionFactory = connectionFactory.PublisherConnectionFactory;
        }

        if (!DirectReplyToContainers.TryGetValue(connectionFactory, out DirectReplyToMessageListenerContainer container))
        {
            lock (DirectReplyToContainers)
            {
                if (!DirectReplyToContainers.TryGetValue(connectionFactory, out container))
                {
                    container = new DirectReplyToMessageListenerContainer(null, connectionFactory);
                    container.MessageListener = this;
                    container.ServiceName = $"{ServiceName}#{Interlocked.Increment(ref _containerInstance)}";

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

                    container.StartAsync();
                    DirectReplyToContainers.TryAdd(connectionFactory, container);
                    _replyAddress = Address.AmqRabbitMQReplyTo;
                }
            }
        }

        DirectReplyToMessageListenerContainer.ChannelHolder channelHolder = container.GetChannelHolder();

        try
        {
            RC.IModel channel = channelHolder.Channel;

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
        IMessage message = Execute(channel =>
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                RC.BasicGetResult response = channel.BasicGet(queueName, !IsChannelTransacted);

                // Response can be null is the case that there is no message on the queue.
                if (response != null)
                {
                    ulong deliveryTag = response.DeliveryTag;

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

    protected virtual void DoSend(RC.IModel channel, string exchangeArg, string routingKeyArg, IMessage message, bool mandatory,
        CorrelationData correlationData, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string exchange = exchangeArg;
        string rKey = routingKeyArg;
        exchange ??= GetDefaultExchange();
        rKey ??= GetDefaultRoutingKey();

        Logger?.LogTrace("Original message to publish: {message}", message);

        IMessage messageToUse = message;
        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(messageToUse);

        if (mandatory)
        {
            accessor.SetHeader(PublisherCallbackChannel.ReturnListenerCorrelationKey, Uuid);
        }

        if (BeforePublishPostProcessors != null)
        {
            IList<IMessagePostProcessor> processors = BeforePublishPostProcessors;

            foreach (IMessagePostProcessor processor in processors)
            {
                messageToUse = processor.PostProcessMessage(messageToUse, correlationData);
            }
        }

        SetupConfirm(channel, messageToUse, correlationData);

        if (UserIdExpression != null && accessor.UserId == null)
        {
            string userId = UserIdExpression.GetValue<string>(EvaluationContext, messageToUse);

            if (userId != null)
            {
                accessor.UserId = userId;
            }
        }

        Logger?.LogDebug("Publishing message [{message}] on exchange [{exchange}], routingKey = [{routingKey}]", messageToUse, exchange, rKey);
        SendToRabbit(channel, exchange, rKey, mandatory, messageToUse);

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

    protected virtual void SendToRabbit(RC.IModel channel, string exchange, string routingKey, bool mandatory, IMessage message)
    {
        if (message.Payload is not byte[] body)
        {
            throw new InvalidOperationException("Unable to publish IMessage, payload must be a byte[]");
        }

        RC.IBasicProperties convertedMessageProperties = channel.CreateBasicProperties();
        MessagePropertiesConverter.FromMessageHeaders(message.Headers, convertedMessageProperties, Encoding);
        channel.BasicPublish(exchange, routingKey, mandatory, convertedMessageProperties, body);
    }

    protected virtual bool IsChannelLocallyTransacted(RC.IModel channel)
    {
        return IsChannelTransacted && !ConnectionFactoryUtils.IsChannelTransactional(channel, ConnectionFactory);
    }

    protected virtual IConnection CreateConnection()
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

    protected IMessage ConvertSendAndReceiveRaw(string exchange, string routingKey, object message, IMessagePostProcessor messagePostProcessor,
        CorrelationData correlationData)
    {
        IMessage requestMessage = ConvertMessageIfNecessary(message);

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

        return DefaultExchange;
    }

    protected virtual string GetDefaultRoutingKey()
    {
        if (DefaultSendDestination != null)
        {
            return DefaultSendDestination.RoutingKey;

            // var dest = ParseDestination(DefaultSendDestination);
            // return dest.Item2;
        }

        return DefaultRoutingKey;
    }

    protected virtual bool UseDirectReplyTo()
    {
        if (UseTemporaryReplyQueues)
        {
            if (ReplyAddress != null)
            {
                Logger?.LogWarning("'useTemporaryReplyQueues' is ignored when a 'replyAddress' is provided");
            }
            else
            {
                return false;
            }
        }

        if (ReplyAddress == null || ReplyAddress == Address.AmqRabbitMQReplyTo)
        {
            try
            {
                return Execute(channel =>
                {
                    channel.QueueDeclarePassive(Address.AmqRabbitMQReplyTo);
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

    protected virtual Task DoStartAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task DoStopAsync()
    {
        return Task.CompletedTask;
    }

    private void Configure(RabbitOptions options)
    {
        if (options == null)
        {
            return;
        }

        RabbitOptions.TemplateOptions templateOptions = options.Template;
        Mandatory = templateOptions.Mandatory || options.PublisherReturns;

        if (templateOptions.Retry.Enabled)
        {
            RetryTemplate = new PollyRetryTemplate(new Dictionary<Type, bool>(), templateOptions.Retry.MaxAttempts, true,
                (int)templateOptions.Retry.InitialInterval.TotalMilliseconds, (int)templateOptions.Retry.MaxInterval.TotalMilliseconds,
                templateOptions.Retry.Multiplier, Logger);
        }

        if (templateOptions.ReceiveTimeout.HasValue)
        {
            int asMillis = (int)templateOptions.ReceiveTimeout.Value.TotalMilliseconds;
            ReceiveTimeout = asMillis;
        }

        if (templateOptions.ReplyTimeout.HasValue)
        {
            int asMillis = (int)templateOptions.ReplyTimeout.Value.TotalMilliseconds;
            ReplyTimeout = asMillis;
        }

        DefaultSendDestination = $"{templateOptions.Exchange}/{templateOptions.RoutingKey}";
        DefaultReceiveDestination = templateOptions.DefaultReceiveQueue;
    }

    private void RestoreProperties(IMessage message, PendingReply pendingReply)
    {
        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);

        if (!UserCorrelationId)
        {
            // Restore the inbound correlation data
            string savedCorrelation = pendingReply.SavedCorrelation;

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
        string savedReplyTo = pendingReply.SavedReplyTo;
        accessor.ReplyTo = savedReplyTo;

        if (savedReplyTo != null)
        {
            Logger?.LogDebug("Restored replyTo to: {replyTo} ", savedReplyTo);
        }
    }

    private IMessage BuildMessageFromDelivery(Delivery delivery)
    {
        return BuildMessage(delivery.Envelope, delivery.Properties, delivery.Body, null);
    }

    private IMessage BuildMessageFromResponse(RC.BasicGetResult response)
    {
        return BuildMessage(new Envelope(response.DeliveryTag, response.Redelivered, response.Exchange, response.RoutingKey), response.BasicProperties,
            response.Body, response.MessageCount);
    }

    private IMessage BuildMessage(Envelope envelope, RC.IBasicProperties properties, byte[] body, uint? msgCount)
    {
        IMessageHeaders messageProps = MessagePropertiesConverter.ToMessageHeaders(properties, envelope, Encoding);

        if (msgCount.HasValue)
        {
            var accessor = MessageHeaderAccessor.GetAccessor<RabbitHeaderAccessor>(messageProps);
            accessor.MessageCount = msgCount.Value;
        }

        IMessage message = Message.Create(body, messageProps);

        if (AfterReceivePostProcessors != null)
        {
            IList<IMessagePostProcessor> processors = AfterReceivePostProcessors;
            IMessage postProcessed = message;

            foreach (IMessagePostProcessor processor in processors)
            {
                postProcessed = processor.PostProcessMessage(postProcessed);
            }

            message = postProcessed;
        }

        return message;
    }

    private IMessageConverter GetRequiredMessageConverter()
    {
        IMessageConverter converter = MessageConverter;

        if (converter == null)
        {
            throw new RabbitIllegalStateException("No 'messageConverter' specified. Check configuration of RabbitTemplate.");
        }

        return converter;
    }

    private ISmartMessageConverter GetRequiredSmartMessageConverter()
    {
        return GetRequiredMessageConverter() as ISmartMessageConverter ??
            throw new RabbitIllegalStateException("template's message converter must be a SmartMessageConverter");
    }

    private string GetRequiredQueue()
    {
        RabbitDestination name = DefaultReceiveDestination;

        if (name == null)
        {
            throw new RabbitIllegalStateException("No 'queue' specified. Check configuration of RabbitTemplate.");
        }

        return name;
    }

    private Address GetReplyToAddress(IMessage request)
    {
        Address replyTo = request.Headers.ReplyToAddress();

        if (replyTo == null)
        {
            string exchange = GetDefaultExchange();
            string routingKey = GetDefaultRoutingKey();

            if (exchange == null)
            {
                throw new RabbitException(
                    "Cannot determine ReplyTo message property value: Request message does not contain reply-to property, and no default Exchange was set.");
            }

            replyTo = new Address(exchange, routingKey);
        }

        return replyTo;
    }

    private void SetupConfirm(RC.IModel channel, IMessage message, CorrelationData correlationDataArg)
    {
        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);

        if ((_publisherConfirms || ConfirmCallback != null) && channel is IPublisherCallbackChannel callbackChannel)
        {
            IPublisherCallbackChannel publisherCallbackChannel = callbackChannel;

            CorrelationData correlationData = CorrelationDataPostProcessor != null
                ? CorrelationDataPostProcessor.PostProcess(message, correlationDataArg)
                : correlationDataArg;

            ulong nextPublishSeqNo = channel.NextPublishSeqNo;
            accessor.PublishSequenceNumber = nextPublishSeqNo;

            publisherCallbackChannel.AddPendingConfirm(this, nextPublishSeqNo,
                new PendingConfirm(correlationData, DateTimeOffset.Now.ToUnixTimeMilliseconds()));

            if (correlationData != null && !string.IsNullOrEmpty(correlationData.Id))
            {
                accessor.SetHeader(PublisherCallbackChannel.ReturnedMessageCorrelationKey, correlationData.Id);
            }
        }
        else if (channel is IChannelProxy proxy && proxy.IsConfirmSelected)
        {
            ulong nextPublishSeqNo = channel.NextPublishSeqNo;
            accessor.PublishSequenceNumber = nextPublishSeqNo;
        }
    }

    private IMessage DoSendAndReceiveAsListener(string exchange, string routingKey, IMessage message, CorrelationData correlationData, RC.IModel channel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pendingReply = new PendingReply();
        string messageTag = Interlocked.Increment(ref _messageTagProvider).ToString(CultureInfo.InvariantCulture);

        if (UserCorrelationId)
        {
            object correlationId = CorrelationKey != null ? message.Headers.Get<object>(CorrelationKey) : message.Headers.CorrelationId();

            if (correlationId == null)
            {
                ReplyHolder[messageTag] = pendingReply;
            }
            else
            {
                ReplyHolder[(string)correlationId] = pendingReply;
            }
        }
        else
        {
            ReplyHolder[messageTag] = pendingReply;
        }

        SaveAndSetProperties(message, pendingReply, messageTag);

        Logger?.LogDebug("Sending message with tag {tag}", messageTag);
        IMessage reply = null;

        try
        {
            reply = ExchangeMessages(exchange, routingKey, message, correlationData, channel, pendingReply, messageTag, cancellationToken);

            if (reply != null && AfterReceivePostProcessors != null)
            {
                IList<IMessagePostProcessor> processors = AfterReceivePostProcessors;
                IMessage postProcessed = reply;

                foreach (IMessagePostProcessor processor in processors)
                {
                    postProcessed = processor.PostProcessMessage(postProcessed);
                }

                reply = postProcessed;
            }
        }
        finally
        {
            ReplyHolder.TryRemove(messageTag, out _);
        }

        return reply;
    }

    private void SaveAndSetProperties(IMessage message, PendingReply pendingReply, string messageTag)
    {
        // Save any existing replyTo and correlation data
        string savedReplyTo = message.Headers.ReplyTo();
        pendingReply.SavedReplyTo = savedReplyTo;

        if (!string.IsNullOrEmpty(savedReplyTo))
        {
            Logger?.LogDebug("Replacing replyTo header: {savedReplyTo} in favor of template's configured reply-queue: {replyAddress}", savedReplyTo,
                ReplyAddress);
        }

        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
        accessor.ReplyTo = ReplyAddress;

        if (!UserCorrelationId)
        {
            object savedCorrelation = null;

            if (CorrelationKey == null)
            {
                // using standard correlationId property
                string correlationId = accessor.CorrelationId;

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

    private IMessage ExchangeMessages(string exchange, string routingKey, IMessage message, CorrelationData correlationData, RC.IModel channel,
        PendingReply pendingReply, string messageTag, CancellationToken cancellationToken)
    {
        IMessage reply;
        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
        bool mandatory = IsMandatoryFor(message);

        if (mandatory && ReturnCallback == null)
        {
            accessor.SetHeader(ReturnCorrelationKey, messageTag);
        }

        DoSend(channel, exchange, routingKey, message, mandatory, correlationData, cancellationToken);

        reply = ReplyTimeout < 0 ? pendingReply.Get() : pendingReply.Get(ReplyTimeout);
        Logger?.LogDebug("Reply: {reply} ", reply);

        if (reply == null)
        {
            ReplyTimedOut(accessor.CorrelationId);
        }

        return reply;
    }

    private void CancelConsumerQuietly(RC.IModel channel, RC.DefaultBasicConsumer consumer)
    {
        RabbitUtils.Cancel(channel, consumer.ConsumerTag);
    }

    private bool DoReceiveAndReply<TReceive, TReply>(string queueName, Func<TReceive, TReply> callback, Func<IMessage, TReply, Address> replyToAddressCallback)
    {
        bool result = Execute(channel =>
        {
            IMessage receiveMessage = ReceiveForReply(queueName, channel, default);

            if (receiveMessage != null)
            {
                return SendReply(callback, replyToAddressCallback, channel, receiveMessage);
            }

            return false;
        }, ObtainTargetConnectionFactory(ReceiveConnectionFactorySelectorExpression, queueName));

        return result;
    }

    private IMessage ReceiveForReply(string queueName, RC.IModel channel, CancellationToken cancellationToken)
    {
        bool channelTransacted = IsChannelTransacted;
        bool channelLocallyTransacted = IsChannelLocallyTransacted(channel);
        IMessage receiveMessage = null;

        if (ReceiveTimeout == 0)
        {
            RC.BasicGetResult response = channel.BasicGet(queueName, !channelTransacted);

            // Response can be null in the case that there is no message on the queue.
            if (response != null)
            {
                ulong deliveryTag1 = response.DeliveryTag;

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
            Delivery delivery = ConsumeDelivery(channel, queueName, ReceiveTimeout, cancellationToken);

            if (delivery != null)
            {
                ulong deliveryTag2 = delivery.Envelope.DeliveryTag;

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

    private Delivery ConsumeDelivery(RC.IModel channel, string queueName, int timeoutMillis, CancellationToken cancellationToken)
    {
        Delivery delivery = null;
        Exception exception = null;
        var future = new TaskCompletionSource<Delivery>();

        RC.DefaultBasicConsumer consumer = null;

        try
        {
            int consumeTimeout = timeoutMillis < 0 ? DefaultConsumeTimeout : timeoutMillis;
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
            Exception cause = e.InnerExceptions.FirstOrDefault();

            Logger?.LogError(cause, "Consumer {consumer} failed to receive message", consumer);
            exception = RabbitExceptionTranslator.ConvertRabbitAccessException(cause);
            throw exception;
        }
        finally
        {
            if (consumer != null && exception is not ConsumerCancelledException && channel.IsOpen)
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
            Logger?.LogDebug("Received no message");
        }
        else
        {
            Logger?.LogDebug("Received: {message}", message);
        }
    }

    private bool SendReply<TReceive, TReply>(Func<TReceive, TReply> receiveAndReplyCallback, Func<IMessage, TReply, Address> replyToAddressCallback,
        RC.IModel channel, IMessage receiveMessage)
    {
        object message = receiveMessage;

        if (message is not TReceive)
        {
            message = GetRequiredMessageConverter().FromMessage(receiveMessage, typeof(TReceive));
        }

        if (message is not TReceive messageAsTReceive)
        {
            throw new InvalidOperationException($"'receiveAndReplyCallback' can't handle received object of type '{message.GetType()}'.");
        }

        TReply reply = receiveAndReplyCallback(messageAsTReceive);

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

    private void DoSendReply<TReply>(Func<IMessage, TReply, Address> replyToAddressCallback, RC.IModel channel, IMessage receiveMessage, TReply reply)
    {
        Address replyTo = replyToAddressCallback(receiveMessage, reply);

        IMessage replyMessage = ConvertMessageIfNecessary(reply);

        RabbitHeaderAccessor receiveMessageAccessor = RabbitHeaderAccessor.GetMutableAccessor(receiveMessage);
        RabbitHeaderAccessor replyMessageAccessor = RabbitHeaderAccessor.GetMutableAccessor(replyMessage);

        object correlation = CorrelationKey == null ? receiveMessageAccessor.CorrelationId : receiveMessageAccessor.GetHeader(CorrelationKey);

        if (CorrelationKey == null || correlation == null)
        {
            // using standard correlationId property
            if (correlation == null)
            {
                string messageId = receiveMessageAccessor.MessageId;

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

    private RC.DefaultBasicConsumer CreateConsumer(string queueName, RC.IModel channel, TaskCompletionSource<Delivery> future, int timeoutMillis,
        CancellationToken cancellationToken)
    {
        channel.BasicQos(0, 1, false);
        var latch = new CountdownEvent(1);
        var consumer = new DefaultTemplateConsumer(channel, latch, future, queueName, cancellationToken);
        RC.IModelExensions.BasicConsume(channel, queueName, false, consumer);

        // Waiting for consumeOK, if latch hasn't signaled, then consumeOK response never hit
        if (!latch.Wait(TimeSpan.FromMilliseconds(timeoutMillis)))
        {
            if (channel is IChannelProxy asProxy)
            {
                asProxy.TargetChannel.Close();
            }

            future.TrySetException(
                new ConsumeOkNotReceivedException($"Blocking receive, consumer failed to consume within  ms: {timeoutMillis} for consumer {consumer}"));
        }

        return consumer;
    }

    private IConnectionFactory ObtainTargetConnectionFactory(IExpression expression, object rootObject)
    {
        if (ConnectionFactory is AbstractRoutingConnectionFactory routingConnectionFactory)
        {
            if (expression == null)
            {
                return routingConnectionFactory.DetermineTargetConnectionFactory();
            }

            object lookupKey = rootObject != null
                ? SendConnectionFactorySelectorExpression.GetValue(EvaluationContext, rootObject)
                : SendConnectionFactorySelectorExpression.GetValue(EvaluationContext);

            if (lookupKey != null)
            {
                IConnectionFactory connectionFactory = routingConnectionFactory.GetTargetConnectionFactory(lookupKey);

                if (connectionFactory != null)
                {
                    return connectionFactory;
                }

                if (!routingConnectionFactory.LenientFallback)
                {
                    throw new InvalidOperationException($"Cannot determine target ConnectionFactory for lookup key [{lookupKey}]");
                }
            }
        }

        return ConnectionFactory;
    }

    private T Execute<T>(Func<RC.IModel, T> action, IConnectionFactory connectionFactory)
    {
        if (RetryTemplate != null)
        {
            try
            {
                return RetryTemplate.Execute(_ => DoExecute(action, connectionFactory), (IRecoveryCallback<T>)RecoveryCallback);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Exception executing DoExecute in retry");
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
        }

        return DoExecute(action, connectionFactory);
    }

    private T DoExecute<T>(Func<RC.IModel, T> channelCallback, IConnectionFactory connectionFactory)
    {
        ArgumentGuard.NotNull(channelCallback);

        RC.IModel channel = null;
        bool invokeScope = false;

        // No need to check the thread local if we know that no invokes are in process
        if (_activeTemplateCallbacks > 0)
        {
            channel = DedicatedChannels.Value;
        }

        RabbitResourceHolder resourceHolder = null;
        IConnection connection = null;

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
                    channel = connection.CreateChannel();

                    if (channel == null)
                    {
                        throw new InvalidOperationException("Connection returned a null channel");
                    }
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, "Exception while creating channel");
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

    private T InvokeAction<T>(Func<RC.IModel, T> channelCallback, IConnectionFactory connectionFactory, RC.IModel channel)
    {
        if (!_confirmsOrReturnsCapable.HasValue)
        {
            DetermineConfirmsReturnsCapability(connectionFactory);
        }

        if (_confirmsOrReturnsCapable.Value)
        {
            AddListener(channel);
        }

        Logger?.LogDebug("Executing callback on RabbitMQ Channel: {channel}", channel);
        return channelCallback(channel);
    }

    private ConfirmListener AddConfirmListener(Action<object, BasicAckEventArgs> acks, Action<object, BasicNackEventArgs> nacks, RC.IModel channel)
    {
        if (acks == null || nacks == null || channel is not IChannelProxy { IsConfirmSelected: true })
        {
            return null;
        }

        return new ConfirmListener(acks, nacks, channel);
    }

    private void CleanUpAfterAction(RC.IModel channel, bool invokeScope, RabbitResourceHolder resourceHolder, IConnection connection)
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

    private void CleanUpAfterAction(RabbitResourceHolder resourceHolder, IConnection connection, RC.IModel channel, ConfirmListener listener)
    {
        if (listener != null)
        {
            listener.Remove();
        }

        Interlocked.Decrement(ref _activeTemplateCallbacks);
        DedicatedChannels.Value = null;

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

        while (cause != null && cause is not ShutdownSignalException && cause is not ProtocolException)
        {
            cause = cause.InnerException;
        }

        if (cause != null && RabbitUtils.IsPassiveDeclarationChannelClose(cause))
        {
            Logger?.LogWarning(ex, "Broker does not support fast replies via 'amq.rabbitmq.reply-to', temporary queues will be used: {cause}.", cause.Message);
            _replyAddress = null;
            return false;
        }

        if (ex != null)
        {
            Logger?.LogDebug(ex, "IO error, deferring directReplyTo detection");
        }

        return true;
    }

    private void EvaluateFastReplyTo()
    {
        UsingFastReplyTo = UseDirectReplyTo();
        EvaluatedFastReplyTo = true;
    }

    protected internal sealed class PendingReply
    {
        private readonly TaskCompletionSource<IMessage> _future = new();

        public string SavedReplyTo { get; set; }

        public string SavedCorrelation { get; set; }

        public IMessage Get()
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

        public IMessage Get(int timeout)
        {
            try
            {
                if (_future.Task.Wait(TimeSpan.FromMilliseconds(timeout)))
                {
                    return _future.Task.Result;
                }

                return null;
            }
            catch (Exception e)
            {
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e.InnerException);
            }
        }

        public void Reply(IMessage message)
        {
            _future.TrySetResult(message);
        }

        public void Returned(RabbitMessageReturnedException e)
        {
            CompleteExceptionally(e);
        }

        public void CompleteExceptionally(Exception exception)
        {
            _future.TrySetException(exception);
        }
    }

    protected class DoSendAndReceiveTemplateConsumer : AbstractTemplateConsumer
    {
        private readonly RabbitTemplate _template;
        private readonly PendingReply _pendingReply;

        public DoSendAndReceiveTemplateConsumer(RabbitTemplate template, RC.IModel channel, PendingReply pendingReply)
            : base(channel)
        {
            _template = template;
            _pendingReply = pendingReply;
        }

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
            RC.IBasicProperties properties, byte[] body)
        {
            IMessageHeaders messageProperties =
                _template.MessagePropertiesConverter.ToMessageHeaders(properties, new Envelope(deliveryTag, redelivered, exchange, routingKey),
                    _template.Encoding);

            IMessage<byte[]> reply = Message.Create(body, messageProperties);
            _template.Logger?.LogTrace("Message received {reply}", reply);

            if (_template.AfterReceivePostProcessors != null)
            {
                IList<IMessagePostProcessor> processors = _template.AfterReceivePostProcessors;
                IMessage postProcessed = reply;

                foreach (IMessagePostProcessor processor in processors)
                {
                    postProcessed = processor.PostProcessMessage(postProcessed);
                }
            }

            _pendingReply.Reply(reply);
        }

        public override void HandleModelShutdown(object model, RC.ShutdownEventArgs reason)
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

        public DefaultTemplateConsumer(RC.IModel channel, CountdownEvent latch, TaskCompletionSource<Delivery> completionSource, string queueName,
            CancellationToken cancellationToken)
            : base(channel)
        {
            _latch = latch;
            _completionSource = completionSource;
            _queueName = queueName;

            cancellationToken.Register(() =>
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

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
            RC.IBasicProperties properties, byte[] body)
        {
            base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);

            _completionSource.TrySetResult(
                new Delivery(consumerTag, new Envelope(deliveryTag, redelivered, exchange, routingKey), properties, body, _queueName));

            Signal();
        }

        public override void HandleModelShutdown(object model, RC.ShutdownEventArgs reason)
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

    protected abstract class AbstractTemplateConsumer : RC.DefaultBasicConsumer
    {
        protected AbstractTemplateConsumer(RC.IModel channel)
            : base(channel)
        {
        }

        public override string ToString()
        {
            return $"TemplateConsumer [channel={Model}, consumerTag={ConsumerTag}]";
        }
    }

    protected class ConfirmListener
    {
        private readonly Action<object, BasicAckEventArgs> _acks;
        private readonly Action<object, BasicNackEventArgs> _nacks;
        private readonly RC.IModel _channel;

        public ConfirmListener(Action<object, BasicAckEventArgs> acks, Action<object, BasicNackEventArgs> nacks, RC.IModel channel)
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

    private sealed class PendingReplyReturn : IReturnCallback
    {
        private readonly PendingReply _pendingReply;

        public PendingReplyReturn(PendingReply pendingReply)
        {
            _pendingReply = pendingReply;
        }

        public void ReturnedMessage(IMessage<byte[]> message, int replyCode, string replyText, string exchange, string routingKey)
        {
            _pendingReply.Returned(new RabbitMessageReturnedException("Message returned", message, replyCode, replyText, exchange, routingKey));
        }
    }

    public interface IReturnCallback
    {
        void ReturnedMessage(IMessage<byte[]> message, int replyCode, string replyText, string exchange, string routingKey);
    }
}
