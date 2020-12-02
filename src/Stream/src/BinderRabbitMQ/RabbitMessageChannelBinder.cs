// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Integration;
using Steeltoe.Integration.Rabbit.Inbound;
using Steeltoe.Integration.Rabbit.Outbound;
using Steeltoe.Integration.Rabbit.Support;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Retry;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;
using Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;
using Steeltoe.Stream.Binder.Rabbit.Config;
using Steeltoe.Stream.Binder.Rabbit.Provisioning;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Provisioning;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IntegrationChannel = Steeltoe.Integration.Channel;
using MessagingSupport = Steeltoe.Messaging.Support;
using SteeltoeConnectionFactory = Steeltoe.Messaging.RabbitMQ.Connection.IConnectionFactory;

namespace Steeltoe.Stream.Binder.Rabbit
{
    public class RabbitMessageChannelBinder : AbstractPollableMessageSourceBinder
    {
        private static readonly SimplePassthroughMessageConverter _passThoughConverter = new SimplePassthroughMessageConverter();
        private static readonly IMessageHeadersConverter _inboundMessagePropertiesConverter = new DefaultBinderMessagePropertiesConverter();
        private static readonly RabbitMessageHeaderErrorMessageStrategy _errorMessageStrategy = new RabbitMessageHeaderErrorMessageStrategy();
        private static readonly Regex _interceptorNeededPattern = new Regex("(payload|#root|#this)");

        public RabbitMessageChannelBinder(IApplicationContext context, ILogger logger, SteeltoeConnectionFactory connectionFactory, RabbitOptions rabbitOptions, RabbitBinderOptions binderOptions, RabbitBindingsOptions bindingsOptions, RabbitExchangeQueueProvisioner provisioningProvider)
            : this(context, logger, connectionFactory, rabbitOptions, binderOptions, bindingsOptions, provisioningProvider, null, null)
        {
        }

        public RabbitMessageChannelBinder(IApplicationContext context, ILogger logger, SteeltoeConnectionFactory connectionFactory, RabbitOptions rabbitOptions, RabbitBinderOptions binderOptions, RabbitBindingsOptions bindingsOptions, RabbitExchangeQueueProvisioner provisioningProvider, IListenerContainerCustomizer containerCustomizer)
        : this(context, logger, connectionFactory, rabbitOptions, binderOptions, bindingsOptions, provisioningProvider, containerCustomizer, null)
        {
        }

        public RabbitMessageChannelBinder(IApplicationContext context, ILogger logger, SteeltoeConnectionFactory connectionFactory, RabbitOptions rabbitOptions, RabbitBinderOptions binderOptions, RabbitBindingsOptions bindingsOptions, RabbitExchangeQueueProvisioner provisioningProvider, IListenerContainerCustomizer containerCustomizer, IMessageSourceCustomizer sourceCustomizer)
            : base(context, new string[0], provisioningProvider, containerCustomizer, sourceCustomizer, logger)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            if (rabbitOptions == null)
            {
                throw new ArgumentNullException(nameof(rabbitOptions));
            }

            _logger = logger;
            ConnectionFactory = connectionFactory;
            RabbitConnectionOptions = rabbitOptions;
            BinderOptions = binderOptions;
            BindingsOptions = bindingsOptions;
        }

        protected ILogger _logger;

        public Steeltoe.Messaging.RabbitMQ.Connection.IConnectionFactory ConnectionFactory { get; }

        public RabbitOptions RabbitConnectionOptions { get; }

        public RabbitBinderOptions BinderOptions { get; }

        public RabbitBindingsOptions BindingsOptions { get; }

        public IMessagePostProcessor DecompressingPostProcessor { get; set; } = new DelegatingDecompressingPostProcessor();

        public IMessagePostProcessor CompressingPostProcessor { get; set; } = new GZipPostProcessor();

        public string[] AdminAddresses { get; set; }

        public string[] Nodes { get; set; }

        public bool Clustered => Nodes?.Length > 1;

        public override string ServiceName { get; set; }

        protected RabbitExchangeQueueProvisioner ProvisioningProvider => (RabbitExchangeQueueProvisioner)_provisioningProvider;

        public void Initialize()
        {
            // TODO: Add this to IBinder interface -> OnInit() code
        }

        public void Destroy()
        {
            ConnectionFactory?.Destroy();
        }

        public RabbitConsumerOptions GetConsumerOptions(string channelName)
        {
            return BindingsOptions.GetRabbitConsumerOptions(channelName);
        }

        public RabbitProducerOptions GetProducerOptions(string channelName)
        {
            return BindingsOptions.GetRabbitProducerOptions(channelName);
        }

        // public string GetDefaultsPrefix()
        // {
        //    return this.extendedBindingProperties.getDefaultsPrefix();
        // }
        protected override IMessageHandler CreateProducerMessageHandler(IProducerDestination producerDestination, IProducerOptions producerProperties, IMessageChannel errorChannel)
        {
            if (producerProperties.HeaderMode == HeaderMode.EmbeddedHeaders)
            {
                throw new InvalidOperationException("The RabbitMQ binder does not support embedded headers since RabbitMQ supports headers natively");
            }

            //var extendedProperties = BindingsOptions.GetRabbitProducerOptions(producerProperties.BindingName);
            var extendedProperties = ((ExtendedProducerOptions<RabbitProducerOptions>)producerProperties).Extension;
            var prefix = extendedProperties.Prefix;
            var exchangeName = producerDestination.Name;
            var destination = string.IsNullOrEmpty(prefix) ? exchangeName : exchangeName.Substring(prefix.Length);
            var endpoint = new RabbitOutboundEndpoint(ApplicationContext, BuildRabbitTemplate(extendedProperties, errorChannel != null));
            endpoint.ExchangeName = producerDestination.Name;
            var expressionInterceptorNeeded = ExpressionInterceptorNeeded(extendedProperties);
            var routingKeyExpression = extendedProperties.RoutingKeyExpression;
            if (!producerProperties.IsPartitioned)
            {
                if (routingKeyExpression == null)
                {
                    endpoint.RoutingKey = destination;
                }
                else
                {
                    if (expressionInterceptorNeeded)
                    {
                        endpoint.SetRoutingKeyExpressionString("headers['" + RabbitExpressionEvaluatingInterceptor.ROUTING_KEY_HEADER + "']");
                    }
                    else
                    {
                        endpoint.RoutingKeyExpression = ExpressionParser.ParseExpression(routingKeyExpression);
                    }
                }
            }
            else
            {
                if (routingKeyExpression == null)
                {
                    endpoint.RoutingKeyExpression = BuildPartitionRoutingExpression(destination, false);
                }
                else
                {
                    if (expressionInterceptorNeeded)
                    {
                        endpoint.RoutingKeyExpression = BuildPartitionRoutingExpression("headers['" + RabbitExpressionEvaluatingInterceptor.ROUTING_KEY_HEADER + "']", true);
                    }
                    else
                    {
                        endpoint.RoutingKeyExpression = BuildPartitionRoutingExpression(routingKeyExpression, true);
                    }
                }
            }

            if (extendedProperties.DelayExpression != null)
            {
                if (expressionInterceptorNeeded)
                {
                    endpoint.SetDelayExpressionString("headers['" + RabbitExpressionEvaluatingInterceptor.DELAY_HEADER + "']");
                }
                else
                {
                    endpoint.DelayExpression = ExpressionParser.ParseExpression(extendedProperties.DelayExpression);
                }
            }

            // DefaultAmqpHeaderMapper mapper = DefaultAmqpHeaderMapper.outboundMapper();
            // List<String> headerPatterns = new ArrayList<>(
            //        extendedProperties.getHeaderPatterns().length + 1);
            // headerPatterns.add("!" + BinderHeaders.PARTITION_HEADER);
            // headerPatterns.addAll(Arrays.asList(extendedProperties.getHeaderPatterns()));
            // mapper.setRequestHeaderNames(
            //        headerPatterns.toArray(new String[headerPatterns.size()]));
            // endpoint.setHeaderMapper(mapper);
            endpoint.DefaultDeliveryMode = extendedProperties.DeliveryMode.Value;
            if (errorChannel != null)
            {
                CheckConnectionFactoryIsErrorCapable();
                endpoint.ReturnChannel = errorChannel;
                endpoint.ConfirmNackChannel = errorChannel;
                var ackChannelBeanName = !string.IsNullOrEmpty(extendedProperties.ConfirmAckChannel) ? extendedProperties.ConfirmAckChannel : IntegrationContextUtils.NULL_CHANNEL_BEAN_NAME;
                if (!ackChannelBeanName.Equals(IntegrationContextUtils.NULL_CHANNEL_BEAN_NAME) && !ApplicationContext.ContainsService<IMessageChannel>(ackChannelBeanName))
                {
                    // GenericApplicationContext context = (GenericApplicationContext)getApplicationContext();
                    var ackChannel = new IntegrationChannel.DirectChannel(ApplicationContext);
                    ApplicationContext.Register(ackChannelBeanName, ackChannel);
                }

                endpoint.ConfirmAckChannelName = ackChannelBeanName;
                endpoint.SetConfirmCorrelationExpressionString("#root");
                endpoint.ErrorMessageStrategy = new DefaultErrorMessageStrategy();
            }

            endpoint.HeadersMappedLast = true;
            endpoint.Initialize();
            return endpoint;
        }

        protected void PostProcessOutputChannel(IMessageChannel outputChannel, RabbitProducerOptions extendedProperties)
        {
            if (ExpressionInterceptorNeeded(extendedProperties))
            {
                var rkExpression = ExpressionParser.ParseExpression(extendedProperties.RoutingKeyExpression);
                var delayExpression = ExpressionParser.ParseExpression(extendedProperties.DelayExpression);
                ((IntegrationChannel.AbstractMessageChannel)outputChannel).AddInterceptor(0, new RabbitExpressionEvaluatingInterceptor(rkExpression, delayExpression, EvaluationContext));
            }
        }

        protected override IMessageProducer CreateConsumerEndpoint(IConsumerDestination consumerDestination, string group, IConsumerOptions consumerOptions)
        {
            if (consumerOptions.HeaderMode == HeaderMode.EmbeddedHeaders)
            {
                throw new InvalidOperationException("The RabbitMQ binder does not support embedded headers since RabbitMQ supports headers natively");
            }

            var destination = consumerDestination.Name;

            //  var properties = BindingsOptions.GetRabbitConsumerOptions(consumerOptions.BindingName);
            var properties = ((ExtendedConsumerOptions<RabbitConsumerOptions>)consumerOptions).Extension;
            var listenerContainer = new DirectMessageListenerContainer(ApplicationContext, ConnectionFactory);
            listenerContainer.AcknowledgeMode = properties.AcknowledgeMode.GetValueOrDefault(AcknowledgeMode.AUTO);
            listenerContainer.IsChannelTransacted = properties.Transacted.GetValueOrDefault();
            listenerContainer.DefaultRequeueRejected = properties.RequeueRejected ?? true;
            int concurrency = consumerOptions.Concurrency;
            concurrency = concurrency > 0 ? concurrency : 1;
            listenerContainer.ConsumersPerQueue = concurrency;
            listenerContainer.PrefetchCount = properties.Prefetch ?? listenerContainer.PrefetchCount;
            listenerContainer.RecoveryInterval = properties.RecoveryInterval ?? listenerContainer.RecoveryInterval;
            var queueNames = destination.Split(',', StringSplitOptions.RemoveEmptyEntries).Select((s) => s.Trim());
            listenerContainer.SetQueueNames(queueNames.ToArray());
            listenerContainer.SetAfterReceivePostProcessors(DecompressingPostProcessor);
            listenerContainer.MessageHeadersConverter = _inboundMessagePropertiesConverter;
            listenerContainer.Exclusive = properties.Exclusive ?? listenerContainer.Exclusive;
            listenerContainer.MissingQueuesFatal = properties.MissingQueuesFatal ?? listenerContainer.MissingQueuesFatal;
            if (properties.FailedDeclarationRetryInterval != null)
            {
                listenerContainer.FailedDeclarationRetryInterval = properties.FailedDeclarationRetryInterval.Value;
            }

            // if (getApplicationEventPublisher() != null)
            // {
            //    listenerContainer
            //            .setApplicationEventPublisher(getApplicationEventPublisher());
            // }
            // else if (getApplicationContext() != null)
            // {
            //    listenerContainer.setApplicationEventPublisher(getApplicationContext());
            // }
            ListenerContainerCustomizer?.Configure(listenerContainer, consumerDestination.Name, group);
            if (!string.IsNullOrEmpty(properties.ConsumerTagPrefix))
            {
                listenerContainer.ConsumerTagStrategy = new RabbitBinderConsumerTagStrategy(properties.ConsumerTagPrefix);
            }

            listenerContainer.Initialize();
            var adapter = new RabbitInboundChannelAdapter(ApplicationContext, listenerContainer, _logger);
            adapter.BindSourceMessage = true;
            adapter.ServiceName = "inbound." + destination;

            // DefaultAmqpHeaderMapper mapper = DefaultAmqpHeaderMapper.inboundMapper();
            // mapper.setRequestHeaderNames(properties.getExtension().getHeaderPatterns());
            // adapter.setHeaderMapper(mapper);
            var errorInfrastructure = RegisterErrorInfrastructure(consumerDestination, group, consumerOptions, _logger);
            if (consumerOptions.MaxAttempts > 1)
            {
                adapter.RetryTemplate = BuildRetryTemplate(consumerOptions);
                adapter.RecoveryCallback = errorInfrastructure.Recoverer;
            }
            else
            {
                adapter.ErrorMessageStrategy = _errorMessageStrategy;
                adapter.ErrorChannel = errorInfrastructure.ErrorChannel;
            }

            adapter.MessageConverter = _passThoughConverter;
            return adapter;
        }

        protected override PolledConsumerResources CreatePolledConsumerResources(string name, string group, IConsumerDestination destination, IConsumerOptions consumerProperties)
        {
            if (consumerProperties.Multiplex)
            {
                throw new InvalidOperationException("The Polled MessageSource does not currently support muiltiple queues");
            }

            var source = new RabbitMessageSource(ApplicationContext, ConnectionFactory, destination.Name);
            source.RawMessageHeader = true;
            MessageSourceCustomizer?.Configure(source, destination.Name, group);
            return new PolledConsumerResources(source, RegisterErrorInfrastructure(destination, group, consumerProperties, true, _logger));
        }

        protected override void PostProcessPollableSource(DefaultPollableMessageSource bindingTarget)
        {
            bindingTarget.AttributeProvider = (accessor, message) =>
            {
                var rawMessage = message.Headers.Get<IMessage>(RabbitMessageHeaderErrorMessageStrategy.AMQP_RAW_MESSAGE);
                if (rawMessage != null)
                {
                    accessor.SetAttribute(RabbitMessageHeaderErrorMessageStrategy.AMQP_RAW_MESSAGE, rawMessage);
                }
            };
        }

        protected override IErrorMessageStrategy GetErrorMessageStrategy()
        {
            return _errorMessageStrategy;
        }

        protected override IMessageHandler GetErrorMessageHandler(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
        {
            var properties = BindingsOptions.GetRabbitConsumerOptions(consumerOptions.BindingName);
            if (properties.RepublishToDlq.Value)
            {
                return new RepublishToDlqErrorMessageHandler(this, properties);
            }
            else if (consumerOptions.MaxAttempts > 1)
            {
                return new RejectingErrorMessageHandler();
            }

            return base.GetErrorMessageHandler(destination, group, consumerOptions);
        }

        protected override IMessageHandler GetPolledConsumerErrorMessageHandler(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
        {
            var handler = GetErrorMessageHandler(destination, group, consumerOptions);
            if (handler != null)
            {
                return handler;
            }

            var superHandler = base.GetErrorMessageHandler(destination, group, consumerOptions);
            var properties = BindingsOptions.GetRabbitConsumerOptions(consumerOptions.BindingName);
            return new DefaultPolledConsumerErrorMessageHandler(superHandler, properties);
        }

        protected override string GetErrorsBaseName(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
        {
            return destination.Name + ".errors";
        }

        protected override void AfterUnbindConsumer(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
        {
            ProvisioningProvider.CleanAutoDeclareContext(destination, consumerOptions);
        }

        private string GetDeadLetterExchangeName(RabbitCommonOptions properties)
        {
            if (properties.DeadLetterExchange == null)
            {
                return ApplyPrefix(properties.Prefix, RabbitCommonOptions.DEAD_LETTER_EXCHANGE);
            }
            else
            {
                return properties.DeadLetterExchange;
            }
        }

        private bool ExpressionInterceptorNeeded(RabbitProducerOptions extendedProperties)
        {
            var rkExpression = extendedProperties.RoutingKeyExpression;
            var delayExpression = extendedProperties.DelayExpression;
            return (rkExpression != null && _interceptorNeededPattern.IsMatch(rkExpression)) || (delayExpression != null && _interceptorNeededPattern.IsMatch(delayExpression));
        }

        private void CheckConnectionFactoryIsErrorCapable()
        {
            // if (!(ConnectionFactory is CachingConnectionFactory))
            // {
            //    logger.warn(
            //            "Unknown connection factory type, cannot determine error capabilities: "
            //                    + ConnectionFactory.GetType());
            // }
            // else
            // {
            //    CachingConnectionFactory ccf = (CachingConnectionFactory)ConnectionFactory;
            //    if (!ccf.IsPublisherConfirms && !ccf.IsPublisherReturns)
            //    {
            //        logger.warn(
            //                "Producer error channel is enabled, but the connection factory is not configured for "
            //                        + "returns or confirms; the error channel will receive no messages");
            //    }
            //    else if (!ccf.IsPublisherConfirms)
            //    {
            //        logger.info(
            //                "Producer error channel is enabled, but the connection factory is only configured to "
            //                        + "handle returned messages; negative acks will not be reported");
            //    }
            //    else if (!ccf.IsPublisherReturns)
            //    {
            //        logger.info(
            //                "Producer error channel is enabled, but the connection factory is only configured to "
            //                        + "handle negatively acked messages; returned messages will not be reported");
            //    }
            // }
        }

        private IExpression BuildPartitionRoutingExpression(string expressionRoot, bool rootIsExpression)
        {
            var partitionRoutingExpression = rootIsExpression
                    ? expressionRoot + " + '-' + headers['" + BinderHeaders.PARTITION_HEADER
                            + "']"
                    : "'" + expressionRoot + "-' + headers['" + BinderHeaders.PARTITION_HEADER
                            + "']";
            return ExpressionParser.ParseExpression(partitionRoutingExpression);
        }

        private RabbitTemplate BuildRabbitTemplate(RabbitProducerOptions properties, bool mandatory)
        {
            RabbitTemplate rabbitTemplate;
            if (properties.BatchingEnabled.Value)
            {
                var batchingStrategy = new SimpleBatchingStrategy(properties.BatchSize.Value, properties.BatchBufferLimit.Value, properties.BatchTimeout.Value);
                rabbitTemplate = new BatchingRabbitTemplate(RabbitConnectionOptions, null, batchingStrategy);
            }
            else
            {
                rabbitTemplate = new RabbitTemplate();
            }

            rabbitTemplate.MessageConverter = _passThoughConverter;
            rabbitTemplate.IsChannelTransacted = properties.Transacted.Value;
            rabbitTemplate.ConnectionFactory = ConnectionFactory;
            rabbitTemplate.UsePublisherConnection = true;
            if (properties.Compress.Value)
            {
                rabbitTemplate.SetBeforePublishPostProcessors(CompressingPostProcessor);
            }

            rabbitTemplate.Mandatory = mandatory; // returned messages
            if (RabbitConnectionOptions != null && RabbitConnectionOptions.Template.Retry.Enabled)
            {
                var retry = RabbitConnectionOptions.Template.Retry;
                var retryTemplate = new PollyRetryTemplate(retry.MaxAttempts, (int)retry.InitialInterval.TotalMilliseconds, (int)retry.MaxInterval.TotalMilliseconds, retry.Multiplier, _logger);
                rabbitTemplate.RetryTemplate = retryTemplate;
            }

            return rabbitTemplate;
        }

        private class DefaultPolledConsumerErrorMessageHandler : IMessageHandler
        {
            private readonly IMessageHandler _superHandler;
            private readonly RabbitConsumerOptions _properties;

            public DefaultPolledConsumerErrorMessageHandler(IMessageHandler superHandler, RabbitConsumerOptions properties)
            {
                _superHandler = superHandler;
                _properties = properties;
                ServiceName = GetType() + "@" + GetHashCode();
            }

            public string ServiceName { get; set; }

            public void HandleMessage(IMessage message)
            {
                var amqpMessage = message.Headers.Get<IMessage>(RabbitMessageHeaderErrorMessageStrategy.AMQP_RAW_MESSAGE);
                var errorMessage = message as MessagingSupport.ErrorMessage;
                if (errorMessage == null)
                {
                    // logger.error("Expected an ErrorMessage, not a " + message.GetType() + " for: " + message);
                }
                else if (amqpMessage == null)
                {
                    if (_superHandler != null)
                    {
                        _superHandler.HandleMessage(message);
                    }
                }
                else
                {
                    var payload = errorMessage.Payload as MessagingException;
                    if (payload != null)
                    {
                        var ack = StaticMessageHeaderAccessor.GetAcknowledgmentCallback(payload.FailedMessage);
                        if (ack != null)
                        {
                            if (_properties.RequeueRejected.Value)
                            {
                                ack.Acknowledge(Integration.Acks.Status.REQUEUE);
                            }
                            else
                            {
                                ack.Acknowledge(Integration.Acks.Status.REJECT);
                            }
                        }
                    }
                }
            }
        }

        private class RejectingErrorMessageHandler : IMessageHandler
        {
            private readonly RejectAndDontRequeueRecoverer _recoverer = new RejectAndDontRequeueRecoverer();

            public RejectingErrorMessageHandler()
            {
                ServiceName = GetType() + "@" + GetHashCode();
            }

            public string ServiceName { get; set; }

            public void HandleMessage(IMessage message)
            {
                // message.Headers.Get<>(IntegrationMessageHeaderAccessor.SOURCE_DATA);
                var errorMessage = message as MessagingSupport.ErrorMessage;
                if (errorMessage == null)
                {
                    // logger.error("Expected an ErrorMessage, not a " + message.getClass().toString() + " for: " + message);
                    throw new ListenerExecutionFailedException("Unexpected error message " + message.ToString(), new RabbitRejectAndDontRequeueException(string.Empty), null);
                }

                _recoverer.Recover(errorMessage, errorMessage.Payload);
            }
        }

        private class RepublishToDlqErrorMessageHandler : IMessageHandler
        {
            private readonly RabbitMessageChannelBinder _binder;
            private readonly RabbitTemplate _template;
            private readonly string _exchange;
            private readonly string _routingKey;
            private readonly int _frameMaxHeaderoom;
            private readonly RabbitConsumerOptions _properties;
            private int _maxStackTraceLength = -1;

            public RepublishToDlqErrorMessageHandler(RabbitMessageChannelBinder binder, RabbitConsumerOptions properties)
            {
                _binder = binder;
                _template = new RabbitTemplate(_binder.ConnectionFactory);
                _template.UsePublisherConnection = true;
                _exchange = _binder.GetDeadLetterExchangeName(properties);
                _routingKey = properties.DeadLetterRoutingKey;
                _frameMaxHeaderoom = properties.FrameMaxHeadroom.Value;
                _properties = properties;
                ServiceName = GetType() + "@" + GetHashCode();
            }

            public string ServiceName { get; set; }

            public void HandleMessage(IMessage message)
            {
                // Message amqpMessage = StaticMessageHeaderAccessor.getSourceData(message);
                var errorMessage = message as MessagingSupport.ErrorMessage;
                if (errorMessage == null)
                {
                    // logger.error("Expected an ErrorMessage, not a " + message.getClass().toString() + " for: " + message);
                    return;
                }

                var cause = errorMessage.Payload as Exception;
                if (!ShouldRepublish(cause))
                {
                    // logger.debug("Skipping republish of: " + message);
                    return;
                }

                var stackTraceAsString = cause.StackTrace;
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                if (_maxStackTraceLength < 0)
                {
                    var result = RabbitUtils.GetMaxFrame(_binder.ConnectionFactory);
                    if (result > 0)
                    {
                        _maxStackTraceLength = result - _frameMaxHeaderoom;
                    }
                }

                if (_maxStackTraceLength > 0 && stackTraceAsString.Length > _maxStackTraceLength)
                {
                    stackTraceAsString = stackTraceAsString.Substring(0, _maxStackTraceLength);

                    // logger.warn("Stack trace in republished message header truncated due to frame_max limitations; consider increasing frame_max on the broker or reduce the stack trace depth", cause);
                }

                accessor.SetHeader(RepublishMessageRecoverer.X_EXCEPTION_STACKTRACE, stackTraceAsString);
                accessor.SetHeader(RepublishMessageRecoverer.X_EXCEPTION_MESSAGE, cause.InnerException != null ? cause.InnerException.Message : cause.Message);
                accessor.SetHeader(RepublishMessageRecoverer.X_ORIGINAL_EXCHANGE, accessor.ReceivedExchange);
                accessor.SetHeader(RepublishMessageRecoverer.X_ORIGINAL_ROUTING_KEY, accessor.ReceivedRoutingKey);
                if (_properties.RepublishDeliveryMode != null)
                {
                    accessor.DeliveryMode = _properties.RepublishDeliveryMode.Value;
                }

                _template.Send(_exchange, _routingKey != null ? _routingKey : accessor.ConsumerQueue, message);
            }

            private bool ShouldRepublish(Exception exception)
            {
                var cause = exception;
                while (cause != null && !(cause is RabbitRejectAndDontRequeueException) && !(cause is ImmediateAcknowledgeException))
                {
                    cause = cause.InnerException;
                }

                return !(cause is ImmediateAcknowledgeException);
            }
        }

        private class RabbitBinderConsumerTagStrategy : IConsumerTagStrategy
        {
            private readonly string _prefix;

            private AtomicInteger _index = new AtomicInteger();

            public RabbitBinderConsumerTagStrategy(string prefix)
            {
                _prefix = prefix;
            }

            public string ServiceName { get; set; } = nameof(RabbitBinderConsumerTagStrategy);

            public string CreateConsumerTag(string queue)
            {
                return _prefix + "#" + _index.IncrementAndGet();
            }
        }

        private class DefaultBinderMessagePropertiesConverter : DefaultMessageHeadersConverter
        {
            public override IMessageHeaders ToMessageHeaders(IBasicProperties source, Envelope envelope, Encoding charset)
            {
                var result = base.ToMessageHeaders(source, envelope, charset);
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(result);
                accessor.DeliveryMode = null;
                return accessor.MessageHeaders;
            }
        }

        private class SimplePassthroughMessageConverter : AbstractMessageConverter
        {
            private readonly SimpleMessageConverter _converter = new SimpleMessageConverter();

            public SimplePassthroughMessageConverter()
            {
            }

            public override string ServiceName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override object FromMessage(IMessage message, Type targetClass, object conversionHint)
            {
                return message.Payload;
            }

            protected override IMessage CreateMessage(object payload, IMessageHeaders messageProperties, object conversionHint)
            {
                if (payload is byte[])
                {
                    return Message.Create((byte[])payload, messageProperties);
                }
                else
                {
                    // just for safety (backwards compatibility)
                    return _converter.ToMessage(payload, messageProperties);
                }
            }
        }
    }
}