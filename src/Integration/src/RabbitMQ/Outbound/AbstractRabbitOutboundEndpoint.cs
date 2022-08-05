// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Rabbit.Support;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Support;

namespace Steeltoe.Integration.Rabbit.Outbound;

public abstract class AbstractRabbitOutboundEndpoint : AbstractReplyProducingMessageHandler, ILifecycle
{
    private readonly object _lock = new();
    private readonly string _noId = Guid.Empty.ToString();
    private readonly ILogger _logger;

    private Task ConfirmChecker { get; set; }

    private CancellationTokenSource TokenSource { get; set; }

    public string ExchangeName { get; set; }

    public string RoutingKey { get; set; }

    public IExpression ExchangeNameExpression { get; set; }

    public IExpression RoutingKeyExpression { get; set; }

    public ExpressionEvaluatingMessageProcessor<string> RoutingKeyGenerator { get; set; }

    public ExpressionEvaluatingMessageProcessor<string> ExchangeNameGenerator { get; set; }

    public IRabbitHeaderMapper HeaderMapper { get; set; }

    public IExpression ConfirmCorrelationExpression { get; set; }

    public ExpressionEvaluatingMessageProcessor<object> CorrelationDataGenerator { get; set; }

    public IMessageChannel ConfirmAckChannel { get; set; }

    public string ConfirmAckChannelName { get; set; }

    public IMessageChannel ConfirmNackChannel { get; set; }

    public string ConfirmNackChannelName { get; set; }

    public IMessageChannel ReturnChannel { get; set; }

    public MessageDeliveryMode DefaultDeliveryMode { get; set; }

    public bool LazyConnect { get; set; } = true;

    public IConnectionFactory ConnectionFactory { get; set; }

    public IExpression DelayExpression { get; set; }

    public ExpressionEvaluatingMessageProcessor<int> DelayGenerator { get; set; }

    public bool HeadersMappedLast { get; set; }

    public IErrorMessageStrategy ErrorMessageStrategy { get; set; } = new DefaultErrorMessageStrategy();

    public TimeSpan? ConfirmTimeout { get; set; }

    public bool Running { get; set; }

    public bool IsRunning => Running;

    protected AbstractRabbitOutboundEndpoint(IApplicationContext context, ILogger logger)
        : base(context)
    {
        _logger = logger;
        HeaderMapper = DefaultRabbitHeaderMapper.GetOutboundMapper(_logger);
    }

    public void SetExchangeNameExpressionString(string exchangeNameExpression)
    {
        if (string.IsNullOrEmpty(exchangeNameExpression))
        {
            throw new ArgumentNullException(nameof(exchangeNameExpression));
        }

        ExchangeNameExpression = IntegrationServices.ExpressionParser.ParseExpression(exchangeNameExpression);
    }

    public void SetRoutingKeyExpressionString(string routingKeyExpression)
    {
        if (string.IsNullOrEmpty(routingKeyExpression))
        {
            throw new ArgumentNullException(nameof(routingKeyExpression));
        }

        RoutingKeyExpression = IntegrationServices.ExpressionParser.ParseExpression(routingKeyExpression);
    }

    public void SetConfirmCorrelationExpressionString(string confirmCorrelationExpression)
    {
        if (string.IsNullOrEmpty(confirmCorrelationExpression))
        {
            throw new ArgumentNullException(nameof(confirmCorrelationExpression));
        }

        ConfirmCorrelationExpression = IntegrationServices.ExpressionParser.ParseExpression(confirmCorrelationExpression);
    }

    public void SetDelay(int delay)
    {
        DelayExpression = new ValueExpression<int>(delay);
    }

    public void SetDelayExpressionString(string delayExpression)
    {
        DelayExpression = delayExpression == null ? null : IntegrationServices.ExpressionParser.ParseExpression(delayExpression);
    }

    public void SetConfirmTimeout(int confirmTimeout)
    {
        ConfirmTimeout = TimeSpan.FromMilliseconds(confirmTimeout);
    }

    public void Start()
    {
        lock (_lock)
        {
            if (!Running)
            {
                if (!LazyConnect && ConnectionFactory != null)
                {
                    try
                    {
                        IConnection connection = ConnectionFactory.CreateConnection();

                        if (connection != null)
                        {
                            connection.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError("Failed to eagerly establish the connection.", e);
                    }
                }

                DoStart();

                if (ConfirmTimeout != null && GetConfirmNackChannel() != null && GetRabbitTemplate() != null)
                {
                    TokenSource = new CancellationTokenSource();
                    ConfirmChecker = Task.Run(() => CheckUnconfirmed(TokenSource.Token));
                }

                Running = true;
            }
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (Running)
            {
                DoStop();
            }

            Running = false;

            if (TokenSource != null)
            {
                TokenSource.Cancel();
                TokenSource = null;
                ConfirmChecker = null;
            }
        }
    }

    public override void Initialize()
    {
        ConfigureExchangeNameGenerator(ApplicationContext);
        ConfigureRoutingKeyGenerator(ApplicationContext);
        ConfigureCorrelationDataGenerator();
        ConfigureDelayGenerator(ApplicationContext);

        EndpointInit();
    }

    Task ILifecycle.StartAsync()
    {
        return Task.Run(Start);
    }

    Task ILifecycle.StopAsync()
    {
        Stop();
        return Task.CompletedTask;
    }

    protected virtual void DoStart()
    {
    }

    protected virtual void DoStop()
    {
    }

    protected virtual IMessageChannel GetConfirmAckChannel()
    {
        if (ConfirmAckChannel == null && ConfirmAckChannelName != null)
        {
            ConfirmAckChannel = IntegrationServices.ChannelResolver.ResolveDestination(ConfirmAckChannelName);
        }

        return ConfirmAckChannel;
    }

    protected virtual IMessageChannel GetConfirmNackChannel()
    {
        if (ConfirmNackChannel == null && ConfirmNackChannelName != null)
        {
            ConfirmNackChannel = IntegrationServices.ChannelResolver.ResolveDestination(ConfirmNackChannelName);
        }

        return ConfirmNackChannel;
    }

    protected virtual void EndpointInit()
    {
    }

    protected abstract RabbitTemplate GetRabbitTemplate();

    protected virtual CorrelationData GenerateCorrelationData(IMessage requestMessage)
    {
        CorrelationData correlationData = null;

        string messageId = requestMessage.Headers.Id ?? _noId;

        if (CorrelationDataGenerator != null)
        {
            object userData = CorrelationDataGenerator.ProcessMessage(requestMessage);

            if (userData != null)
            {
                correlationData = new CorrelationDataWrapper(messageId, userData, requestMessage);
            }
            else
            {
                _logger?.LogDebug("'confirmCorrelationExpression' resolved to 'null'; no publisher confirm will be sent to the ack or nack channel");
            }
        }

        if (correlationData == null)
        {
            object correlation = requestMessage.Headers[RabbitMessageHeaders.PublishConfirmCorrelation];

            if (correlation is CorrelationData cdata)
            {
                correlationData = cdata;
            }

            if (correlationData != null)
            {
                correlationData = new CorrelationDataWrapper(messageId, correlationData, requestMessage);
            }
        }

        return correlationData;
    }

    protected virtual string GenerateExchangeName(IMessage requestMessage)
    {
        string exchange = ExchangeName;

        if (ExchangeNameGenerator != null)
        {
            exchange = ExchangeNameGenerator.ProcessMessage(requestMessage);
        }

        return exchange;
    }

    protected virtual string GenerateRoutingKey(IMessage requestMessage)
    {
        string key = RoutingKey;

        if (RoutingKeyGenerator != null)
        {
            key = RoutingKeyGenerator.ProcessMessage(requestMessage);
        }

        return key;
    }

    protected virtual void AddDelayProperty(IMessage message)
    {
        if (DelayGenerator != null)
        {
            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.Delay = DelayGenerator.ProcessMessage(message);
        }
    }

    protected virtual IMessageBuilder BuildReply(IMessageConverter converter, IMessage amqpReplyMessage)
    {
        object replyObject = converter.FromMessage(amqpReplyMessage, null);
        IMessageBuilder builder = PrepareMessageBuilder(replyObject);

        // var headers = getHeaderMapper().toHeadersFromReply(amqpReplyMessage.getMessageProperties());
        builder.CopyHeadersIfAbsent(amqpReplyMessage.Headers);
        return builder;
    }

    protected virtual IMessage BuildReturnedMessage(IMessage message, int replyCode, string replyText, string exchange, string returnedRoutingKey,
        IMessageConverter converter)
    {
        object returnedObject = converter.FromMessage(message, null);
        IMessageBuilder builder = PrepareMessageBuilder(returnedObject);

        // TODO: Map < String, ?> headers = getHeaderMapper().toHeadersFromReply(message.getMessageProperties());
        if (ErrorMessageStrategy == null)
        {
            builder.CopyHeadersIfAbsent(message.Headers).SetHeader(RabbitMessageHeaders.ReturnReplyCode, replyCode)
                .SetHeader(RabbitMessageHeaders.ReturnReplyText, replyText).SetHeader(RabbitMessageHeaders.ReturnExchange, exchange)
                .SetHeader(RabbitMessageHeaders.ReturnRoutingKey, returnedRoutingKey);
        }

        IMessage returnedMessage = builder.Build();

        if (ErrorMessageStrategy != null)
        {
            returnedMessage = ErrorMessageStrategy.BuildErrorMessage(
                new ReturnedRabbitMessageException(returnedMessage, replyCode, replyText, exchange, returnedRoutingKey), null);
        }

        return returnedMessage;
    }

    protected void HandleConfirm(CorrelationData correlationData, bool ack, string cause)
    {
        var wrapper = (CorrelationDataWrapper)correlationData;

        if (correlationData == null)
        {
            _logger.LogDebug("No correlation data provided for ack: " + ack + " cause:" + cause);
            return;
        }

        object userCorrelationData = wrapper.UserData;
        IMessage confirmMessage;
        confirmMessage = BuildConfirmMessage(ack, cause, wrapper, userCorrelationData);

        if (ack && GetConfirmAckChannel() != null)
        {
            SendOutput(confirmMessage, GetConfirmAckChannel(), true);
        }
        else if (!ack && GetConfirmNackChannel() != null)
        {
            SendOutput(confirmMessage, GetConfirmNackChannel(), true);
        }
        else
        {
            _logger.LogInformation("Nowhere to send publisher confirm " + (ack ? "ack" : "nack") + " for " + userCorrelationData);
        }
    }

    private IMessage BuildConfirmMessage(bool ack, string cause, CorrelationDataWrapper wrapper, object userCorrelationData)
    {
        if (ErrorMessageStrategy == null || ack)
        {
            var headers = new Dictionary<string, object>
            {
                { RabbitMessageHeaders.PublishConfirm, ack }
            };

            if (!ack && !string.IsNullOrEmpty(cause))
            {
                headers.Add(RabbitMessageHeaders.PublishConfirmNackCause, cause);
            }

            return PrepareMessageBuilder(userCorrelationData).CopyHeaders(headers).Build();
        }

        return ErrorMessageStrategy.BuildErrorMessage(new NackedRabbitMessageException(wrapper.Message, wrapper.UserData, cause), null);
    }

    private IMessageBuilder PrepareMessageBuilder(object replyObject)
    {
        IMessageBuilderFactory factory = IntegrationServices.MessageBuilderFactory;
        return replyObject is IMessage message ? factory.FromMessage(message) : factory.WithPayload(replyObject);
    }

    private void CheckUnconfirmed(CancellationToken cancellationToken)
    {
        double delay = ConfirmTimeout.Value.TotalMilliseconds / 2L;

        while (!cancellationToken.IsCancellationRequested)
        {
            RabbitTemplate rabbitTemplate = GetRabbitTemplate();

            if (rabbitTemplate != null)
            {
                ICollection<CorrelationData> unconfirmed = rabbitTemplate.GetUnconfirmed((long)ConfirmTimeout.Value.TotalMilliseconds);

                if (unconfirmed != null)
                {
                    foreach (CorrelationData cd in unconfirmed)
                    {
                        HandleConfirm(cd, false, "Confirm timed out");
                    }
                }
            }

            Thread.Sleep((int)delay);
        }
    }

    private void ConfigureExchangeNameGenerator(IApplicationContext context)
    {
        if (ExchangeNameExpression != null && ExchangeName != null)
        {
            throw new InvalidOperationException("Either a exchangeName or an exchangeNameExpression can be provided, but not both");
        }

        if (ExchangeNameExpression != null)
        {
            ExchangeNameGenerator = new ExpressionEvaluatingMessageProcessor<string>(context, ExchangeNameExpression);
        }
    }

    private void ConfigureRoutingKeyGenerator(IApplicationContext context)
    {
        if (RoutingKeyExpression != null && RoutingKey != null)
        {
            throw new InvalidOperationException("Either a routingKey or an routingKeyExpression can be provided, but not both");
        }

        if (RoutingKeyExpression != null)
        {
            RoutingKeyGenerator = new ExpressionEvaluatingMessageProcessor<string>(context, RoutingKeyExpression);
        }
    }

    private void ConfigureCorrelationDataGenerator()
    {
        if (ConfirmCorrelationExpression != null)
        {
            CorrelationDataGenerator = new ExpressionEvaluatingMessageProcessor<object>(ApplicationContext, ConfirmCorrelationExpression);
        }
        else
        {
            var nullChannel = IntegrationServicesUtils.ExtractTypeIfPossible<NullChannel>(ConfirmAckChannel);

            if (!((ConfirmAckChannel == null || nullChannel != null) && ConfirmAckChannelName == null))
            {
                throw new InvalidOperationException("A 'confirmCorrelationExpression' is required when specifying a 'confirmAckChannel'");
            }

            nullChannel = IntegrationServicesUtils.ExtractTypeIfPossible<NullChannel>(ConfirmNackChannel);

            if (!((ConfirmNackChannel == null || nullChannel != null) && ConfirmNackChannelName == null))
            {
                throw new InvalidOperationException("A 'confirmCorrelationExpression' is required when specifying a 'confirmNackChannel'");
            }
        }
    }

    private void ConfigureDelayGenerator(IApplicationContext context)
    {
        if (DelayExpression != null)
        {
            DelayGenerator = new ExpressionEvaluatingMessageProcessor<int>(context, DelayExpression);
        }
    }

    protected class CorrelationDataWrapper : CorrelationData
    {
        public object UserData { get; }

        public IMessage Message { get; }

        public override TaskCompletionSource<Confirm> FutureSource
        {
            get
            {
                if (UserData is CorrelationData data)
                {
                    return data.FutureSource;
                }

                return base.FutureSource;
            }
        }

        public override IMessage ReturnedMessage
        {
            get
            {
                if (UserData is CorrelationData data)
                {
                    return data.ReturnedMessage;
                }

                return base.ReturnedMessage;
            }

            set
            {
                if (UserData is CorrelationData data)
                {
                    data.ReturnedMessage = value;
                }

                base.ReturnedMessage = value;
            }
        }

        public CorrelationDataWrapper(string id, object userData, IMessage message)
            : base(id)
        {
            UserData = userData;
            Message = message;
        }
    }
}
