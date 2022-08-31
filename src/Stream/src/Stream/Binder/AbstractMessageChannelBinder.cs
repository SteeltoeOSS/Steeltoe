// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Configuration;
using Steeltoe.Stream.Provisioning;
using AbstractMessageChannel = Steeltoe.Integration.Channel.AbstractMessageChannel;
using AbstractSubscribableChannel = Steeltoe.Integration.Channel.AbstractSubscribableChannel;

namespace Steeltoe.Stream.Binder;

public abstract class AbstractMessageChannelBinder : AbstractBinder<IMessageChannel>
{
    private readonly ILogger _logger;
    protected readonly IProvisioningProvider InnerProvisioningProvider;
    protected readonly EmbeddedHeadersChannelInterceptor CurrentEmbeddedHeadersChannelInterceptor = new();
    protected readonly string[] HeadersToEmbed;
    protected bool producerBindingExist;

    protected virtual IListenerContainerCustomizer ListenerContainerCustomizer { get; }

    protected virtual IMessageSourceCustomizer MessageSourceCustomizer { get; }

    public override Type TargetType { get; } = typeof(IMessageChannel);

    protected AbstractMessageChannelBinder(IApplicationContext context, string[] headersToEmbed, IProvisioningProvider provisioningProvider, ILogger logger)
        : this(context, headersToEmbed, provisioningProvider, null, null, logger)
    {
        _logger = logger;
    }

    protected AbstractMessageChannelBinder(IApplicationContext context, string[] headersToEmbed, IProvisioningProvider provisioningProvider,
        IListenerContainerCustomizer containerCustomizer, IMessageSourceCustomizer sourceCustomizer, ILogger logger)
        : base(context, logger)
    {
        HeadersToEmbed = headersToEmbed ?? Array.Empty<string>();
        InnerProvisioningProvider = provisioningProvider;
        ListenerContainerCustomizer = containerCustomizer;
        MessageSourceCustomizer = sourceCustomizer;
        _logger = logger;
    }

    protected override IBinding DoBindProducer(string name, IMessageChannel outboundTarget, IProducerOptions producerOptions)
    {
        if (outboundTarget is not ISubscribableChannel subscribableChannel)
        {
            throw new ArgumentException($"Binding is supported only for {nameof(ISubscribableChannel)} instances.", nameof(outboundTarget));
        }

        IMessageHandler producerMessageHandler;
        IProducerDestination producerDestination;

        try
        {
            producerDestination = InnerProvisioningProvider.ProvisionProducerDestination(name, producerOptions);
            ISubscribableChannel errorChannel = producerOptions.ErrorChannelEnabled ? RegisterErrorInfrastructure(producerDestination) : null;
            producerMessageHandler = CreateProducerMessageHandler(producerDestination, producerOptions, subscribableChannel, errorChannel);
        }
        catch (Exception e) when (e is BinderException || e is ProvisioningException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new BinderException("Exception thrown while building outbound endpoint", e);
        }

        if (producerOptions.AutoStartup && producerMessageHandler is ILifecycle producerMsgHandlerLifecycle)
        {
            producerMsgHandlerLifecycle.StartAsync();
        }

        PostProcessOutputChannel(subscribableChannel, producerOptions);

        var sendingHandler = new SendingHandler(ApplicationContext, producerMessageHandler, HeaderMode.EmbeddedHeaders.Equals(producerOptions.HeaderMode),
            HeadersToEmbed, UseNativeEncoding(producerOptions));

        sendingHandler.Initialize();
        subscribableChannel.Subscribe(sendingHandler);

        IBinding binding = new DefaultProducingMessageChannelBinding(this, name, subscribableChannel, producerMessageHandler as ILifecycle, producerOptions,
            producerDestination, _logger);

        producerBindingExist = true;
        return binding;
    }

    protected virtual bool UseNativeEncoding(IProducerOptions producerOptions)
    {
        return producerOptions.UseNativeEncoding;
    }

    protected virtual void PostProcessOutputChannel(IMessageChannel outputChannel, IProducerOptions producerOptions)
    {
        // default no-op
    }

    protected virtual IMessageHandler CreateProducerMessageHandler(IProducerDestination destination, IProducerOptions producerProperties,
        IMessageChannel channel, IMessageChannel errorChannel)
    {
        return CreateProducerMessageHandler(destination, producerProperties, errorChannel);
    }

    protected abstract IMessageHandler CreateProducerMessageHandler(IProducerDestination destination, IProducerOptions producerProperties,
        IMessageChannel errorChannel);

    protected virtual void AfterUnbindProducer(IProducerDestination destination, IProducerOptions producerOptions)
    {
    }

    protected override IBinding DoBindConsumer(string name, string group, IMessageChannel inputTarget, IConsumerOptions consumerOptions)
    {
        IMessageProducer consumerEndpoint = null;

        try
        {
            IConsumerDestination destination = InnerProvisioningProvider.ProvisionConsumerDestination(name, group, consumerOptions);

            // TODO: the function support for the inbound channel is only for Sink
            // if (ShouldWireFunctionToChannel(false))
            // {
            //    inputChannel = PostProcessInboundChannelForFunction(inputChannel, options);
            // }
            if (consumerOptions.HeaderMode == HeaderMode.EmbeddedHeaders)
            {
                EnhanceMessageChannel(inputTarget);
            }

            consumerEndpoint = CreateConsumerEndpoint(destination, group, consumerOptions);
            consumerEndpoint.OutputChannel = inputTarget;

            if (consumerOptions.AutoStartup && consumerEndpoint is ILifecycle lifecycle)
            {
                lifecycle.StartAsync();
            }

            IBinding binding = new DefaultConsumerMessageChannelBinding(this, name, group, inputTarget, consumerEndpoint as ILifecycle, consumerOptions,
                destination, _logger);

            return binding;
        }
        catch (Exception e)
        {
            if (consumerEndpoint is ILifecycle lifecycle)
            {
                lifecycle.StopAsync();
            }

            if (e is BinderException)
            {
                throw;
            }

            if (e is ProvisioningException)
            {
                throw;
            }

            throw new BinderException("Exception thrown while starting consumer: ", e);
        }
    }

    protected abstract IMessageProducer CreateConsumerEndpoint(IConsumerDestination destination, string group, IConsumerOptions consumerOptions);

    protected virtual void AfterUnbindConsumer(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
    {
    }

    protected virtual ErrorInfrastructure RegisterErrorInfrastructure(IConsumerDestination destination, string group, IConsumerOptions consumerOptions,
        ILogger logger)
    {
        return RegisterErrorInfrastructure(destination, group, consumerOptions, false, logger);
    }

    protected virtual ErrorInfrastructure RegisterErrorInfrastructure(IConsumerDestination destination, string group, IConsumerOptions consumerOptions,
        bool polled, ILogger logger)
    {
        IErrorMessageStrategy errorMessageStrategy = GetErrorMessageStrategy();

        string errorChannelName = GetErrorsBaseName(destination, group, consumerOptions);

        ISubscribableChannel errorChannel = GetErrorChannel(logger, errorChannelName);

        var recoverer = new ErrorMessageSendingRecoverer(ApplicationContext, errorChannel, errorMessageStrategy);

        string recovererBeanName = GetErrorRecovererName(destination, group, consumerOptions);
        ApplicationContext.Register(recovererBeanName, recoverer);

        IMessageHandler handler = polled
            ? GetPolledConsumerErrorMessageHandler(destination, group, consumerOptions)
            : GetErrorMessageHandler(destination, group, consumerOptions);

        var defaultErrorChannel = ApplicationContext.GetService<IMessageChannel>(IntegrationContextUtils.ErrorChannelBeanName);

        if (handler == null && errorChannel is ILastSubscriberAwareChannel channel)
        {
            handler = GetDefaultErrorMessageHandler(channel, defaultErrorChannel != null);
        }

        string errorMessageHandlerName = GetErrorMessageHandlerName(destination, group, consumerOptions);

        if (handler != null)
        {
            handler.ServiceName = errorMessageHandlerName;

            if (IsSubscribable(errorChannel))
            {
                IMessageHandler errorHandler = handler;
                ApplicationContext.Register(errorMessageHandlerName, errorHandler);
                errorChannel.Subscribe(handler);
            }
            else
            {
                _logger?.LogWarning(
                    "The provided errorChannel '{channel}' is an instance of DirectChannel, so no more subscribers could be added " +
                    "which may affect DLQ processing. Resolution: Configure your own errorChannel as an instance of PublishSubscribeChannel", errorChannelName);
            }
        }

        if (defaultErrorChannel != null)
        {
            if (IsSubscribable(errorChannel))
            {
                var errorBridge = new BridgeHandler(ApplicationContext)
                {
                    OutputChannel = defaultErrorChannel
                };

                errorChannel.Subscribe(errorBridge);

                string errorBridgeHandlerName = GetErrorBridgeName(destination, group, consumerOptions);
                errorBridge.ServiceName = errorBridgeHandlerName;
                ApplicationContext.Register(errorBridgeHandlerName, errorBridge);
            }
            else
            {
                _logger?.LogWarning(
                    "The provided errorChannel '{channel}' is an instance of DirectChannel, so no more subscribers could be added " +
                    "and no error messages will be sent to global error channel. Resolution: Configure your own errorChannel as an instance of PublishSubscribeChannel",
                    errorChannelName);
            }
        }

        return new ErrorInfrastructure(errorChannel, recoverer, handler);
    }

    protected virtual IMessageHandler GetErrorMessageHandler(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
    {
        return null;
    }

    protected virtual IMessageHandler GetPolledConsumerErrorMessageHandler(IConsumerDestination destination, string group, IConsumerOptions consumerProperties)
    {
        return null;
    }

    protected virtual IMessageHandler GetDefaultErrorMessageHandler(ILastSubscriberAwareChannel errorChannel, bool defaultErrorChannelPresent)
    {
        return new FinalRethrowingErrorMessageHandler(errorChannel, defaultErrorChannelPresent);
    }

    protected virtual IErrorMessageStrategy GetErrorMessageStrategy()
    {
        return null;
    }

    protected virtual string GetErrorRecovererName(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
    {
        return $"{GetErrorsBaseName(destination, group, consumerOptions)}.recoverer";
    }

    protected virtual string GetErrorMessageHandlerName(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
    {
        return $"{GetErrorsBaseName(destination, group, consumerOptions)}.handler";
    }

    protected virtual string GetErrorsBaseName(IProducerDestination destination)
    {
        return $"{destination.Name}.errors";
    }

    protected virtual string GetErrorsBaseName(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
    {
        return $"{destination.Name}.{group}.errors";
    }

    protected virtual string GetErrorBridgeName(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
    {
        return $"{GetErrorsBaseName(destination, group, consumerOptions)}.bridge";
    }

    protected virtual string GetErrorBridgeName(IProducerDestination destination)
    {
        return $"{GetErrorsBaseName(destination)}.bridge";
    }

    private static bool IsSubscribable(ISubscribableChannel errorChannel)
    {
        return errorChannel is PublishSubscribeChannel || errorChannel is not AbstractSubscribableChannel ||
            (errorChannel is AbstractSubscribableChannel subscribableChannel && subscribableChannel.SubscriberCount == 0);
    }

    private static Dictionary<string, object> DoGetExtendedInfo(object destination, object properties)
    {
        var extendedInfo = new Dictionary<string, object>
        {
            { "bindingDestination", destination.ToString() }
        };

        object value;

        if (properties is string stringValue)
        {
            value = JsonSerializer.Deserialize<Dictionary<string, object>>(stringValue);
        }
        else
        {
            value = properties;
        }

        extendedInfo.Add(properties.GetType().Name, value);
        return extendedInfo;
    }

    private ISubscribableChannel GetErrorChannel(ILogger logger, string errorChannelName)
    {
        ISubscribableChannel errorChannel;
        var errorChannelObject = ApplicationContext.GetService<IMessageChannel>(errorChannelName);

        if (errorChannelObject != null)
        {
            if (errorChannelObject is not ISubscribableChannel subscribableChannel)
            {
                throw new InvalidOperationException($"Error channel '{errorChannelName}' must be a {nameof(ISubscribableChannel)}.");
            }

            errorChannel = subscribableChannel;
        }
        else
        {
            errorChannel = new BinderErrorChannel(ApplicationContext, errorChannelName, logger);
            ApplicationContext.Register(errorChannelName, errorChannel);
        }

        return errorChannel;
    }

    private ISubscribableChannel RegisterErrorInfrastructure(IProducerDestination destination)
    {
        string errorChannelName = GetErrorsBaseName(destination);
        ISubscribableChannel errorChannel;
        var errorChannelObject = ApplicationContext.GetService<IMessageChannel>(errorChannelName);

        if (errorChannelObject != null)
        {
            if (errorChannelObject is not ISubscribableChannel subscribableChannel)
            {
                throw new InvalidOperationException($"Error channel '{errorChannelName}' must be a ISubscribableChannel");
            }

            errorChannel = subscribableChannel;
        }
        else
        {
            errorChannel = new PublishSubscribeChannel(ApplicationContext);
            ApplicationContext.Register(errorChannelName, errorChannel);
        }

        var defaultErrorChannel = ApplicationContext.GetService<IMessageChannel>(IntegrationContextUtils.ErrorChannelBeanName);

        if (defaultErrorChannel != null)
        {
            var errorBridge = new BridgeHandler(ApplicationContext)
            {
                OutputChannel = defaultErrorChannel
            };

            errorChannel.Subscribe(errorBridge);
            string errorBridgeHandlerName = GetErrorBridgeName(destination);
            ApplicationContext.Register(errorBridgeHandlerName, errorBridge);
        }

        return errorChannel;
    }

    private void DestroyErrorInfrastructure(IProducerDestination destination)
    {
        string errorChannelName = GetErrorsBaseName(destination);
        string errorBridgeHandlerName = GetErrorBridgeName(destination);

        if (ApplicationContext.GetService<IMessageChannel>(errorChannelName) is ISubscribableChannel channel)
        {
            var bridgeHandler = ApplicationContext.GetService<IMessageHandler>(errorBridgeHandlerName);

            if (bridgeHandler != null)
            {
                channel.Unsubscribe(bridgeHandler);
                ApplicationContext.Deregister(errorBridgeHandlerName);
            }

            ApplicationContext.Deregister(errorChannelName);
        }
    }

    private void DestroyErrorInfrastructure(IConsumerDestination destination, string group, IConsumerOptions options)
    {
        try
        {
            string recoverer = GetErrorRecovererName(destination, group, options);

            DestroyBean(recoverer);

            string errorChannelName = GetErrorsBaseName(destination, group, options);
            string errorMessageHandlerName = GetErrorMessageHandlerName(destination, group, options);
            string errorBridgeHandlerName = GetErrorBridgeName(destination, group, options);

            if (ApplicationContext.GetService<IMessageChannel>(errorChannelName) is ISubscribableChannel channel)
            {
                var bridgeHandler = ApplicationContext.GetService<IMessageHandler>(errorBridgeHandlerName);

                if (bridgeHandler != null)
                {
                    channel.Unsubscribe(bridgeHandler);
                    DestroyBean(errorBridgeHandlerName);
                }

                var messageHandler = ApplicationContext.GetService<IMessageHandler>(errorMessageHandlerName);

                if (messageHandler != null)
                {
                    channel.Unsubscribe(messageHandler);
                    DestroyBean(errorMessageHandlerName);
                }

                DestroyBean(errorChannelName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, ex.Message);
        }
    }

    private void DestroyBean(string beanName)
    {
        ApplicationContext.Deregister(beanName);
    }

    private void EnhanceMessageChannel(IMessageChannel inputChannel)
    {
        ((AbstractMessageChannel)inputChannel).AddInterceptor(0, CurrentEmbeddedHeadersChannelInterceptor);
    }

    // private void doPublishEvent(ApplicationEvent event)
    // {
    //    if (this.applicationEventPublisher != null)
    //    {
    //        this.applicationEventPublisher.publishEvent(event);
    //    }
    // }

    // private bool ShouldWireFunctionToChannel(bool producer)
    // {
    //    if (!producer && this.producerBindingExist)
    //    {
    //        return false;
    //    }
    //    else
    //    {
    //        return this.streamFunctionProperties != null
    //                && StringUtils.hasText(this.streamFunctionProperties.getDefinition())
    //                && (!this.getApplicationContext()
    //                        .containsBean("integrationFlowCreator")
    //                        || this.getApplicationContext()
    //                                .getBean("integrationFlowCreator").equals(null));
    //    }
    // }

    // private SubscribableChannel postProcessOutboundChannelForFunction( MessageChannel outputChannel, ProducerProperties producerProperties)
    //        {
    //            if (this.integrationFlowFunctionSupport != null)
    //            {
    //                Publisher <?> publisher = MessageChannelReactiveUtils
    //                        .toPublisher(outputChannel);
    //                // If the app has an explicit Supplier bean defined, make that as the
    //                // publisher
    //                if (this.integrationFlowFunctionSupport.containsFunction(Supplier.class)) {
    // IntegrationFlowBuilder integrationFlowBuilder = IntegrationFlows
    //                        .from(outputChannel).bridge();
    //        publisher = integrationFlowBuilder.toReactivePublisher();
    // }
    // if (this.integrationFlowFunctionSupport.containsFunction(Function.class,

    // this.streamFunctionProperties.getDefinition())) {
    //        DirectChannel actualOutputChannel = new DirectChannel();
    //        if (outputChannel instanceof AbstractMessageChannel) {
    //            moveChannelInterceptors((AbstractMessageChannel)outputChannel,
    //                    actualOutputChannel);
    //        }
    //        this.integrationFlowFunctionSupport.andThenFunction(publisher,
    //                actualOutputChannel, this.streamFunctionProperties);
    //        return actualOutputChannel;
    //    }
    //    }
    // return (SubscribableChannel) outputChannel;
    // }

    // private SubscribableChannel postProcessInboundChannelForFunction(MessageChannel inputChannel, ConsumerProperties consumerProperties)
    // {
    //    if (this.integrationFlowFunctionSupport != null
    //            && (this.integrationFlowFunctionSupport.containsFunction(Consumer.class)
    // || this.integrationFlowFunctionSupport
    //                                .containsFunction(Function.class))) {
    // DirectChannel actualInputChannel = new DirectChannel();
    // if (inputChannel instanceof AbstractMessageChannel) {
    // moveChannelInterceptors((AbstractMessageChannel) inputChannel,
    //                        actualInputChannel);
    // }

    // this.integrationFlowFunctionSupport.andThenFunction(
    //                    MessageChannelReactiveUtils.toPublisher(actualInputChannel),
    //                    inputChannel, this.streamFunctionProperties);
    // return actualInputChannel;
    // }
    // return (SubscribableChannel) inputChannel;
    // }

    // private void moveChannelInterceptors(AbstractMessageChannel existingMessageChannel,  AbstractMessageChannel actualMessageChannel)
    // {
    //    for (ChannelInterceptor channelInterceptor : existingMessageChannel
    //            .getChannelInterceptors())
    //    {
    //        actualMessageChannel.addInterceptor(channelInterceptor);
    //        existingMessageChannel.removeInterceptor(channelInterceptor);
    //    }
    // }
    public class ErrorInfrastructure
    {
        public ISubscribableChannel ErrorChannel { get; }

        public ErrorMessageSendingRecoverer Recoverer { get; }

        public IMessageHandler Handler { get; }

        public ErrorInfrastructure(ISubscribableChannel errorChannel, ErrorMessageSendingRecoverer recoverer, IMessageHandler handler)
        {
            ErrorChannel = errorChannel;
            Recoverer = recoverer;
            Handler = handler;
        }
    }

    protected class PolledConsumerResources
    {
        protected internal IMessageSource Source { get; }

        protected internal ErrorInfrastructure ErrorInfrastructure { get; }

        public PolledConsumerResources(IMessageSource source, ErrorInfrastructure errorInfrastructure)
        {
            Source = source;
            ErrorInfrastructure = errorInfrastructure;
        }
    }

    protected class SendingHandler : AbstractMessageHandler, ILifecycle
    {
        private readonly bool _embedHeaders;

        private readonly string[] _embeddedHeaders;

        private readonly IMessageHandler _handler;

        private readonly bool _useNativeEncoding;

        public bool IsRunning => _handler is ILifecycle lifecycle && lifecycle.IsRunning;

        public SendingHandler(IApplicationContext context, IMessageHandler handler, bool embedHeaders, string[] headersToEmbed, bool useNativeEncoding)
            : base(context)
        {
            _handler = handler;
            _embedHeaders = embedHeaders;
            _embeddedHeaders = headersToEmbed;
            _useNativeEncoding = useNativeEncoding;
        }

        public override void Initialize()
        {
        }

        public Task StartAsync()
        {
            if (_handler is ILifecycle lifecycle)
            {
                return lifecycle.StartAsync();
            }

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (_handler is ILifecycle lifecycle)
            {
                return lifecycle.StopAsync();
            }

            return Task.CompletedTask;
        }

        protected override void HandleMessageInternal(IMessage message)
        {
            IMessage messageToSend = _useNativeEncoding ? message : SerializeAndEmbedHeadersIfApplicable(message);
            _handler.HandleMessage(messageToSend);
        }

        private IMessage SerializeAndEmbedHeadersIfApplicable(IMessage message)
        {
            var transformed = new MessageValues(message);

            object payload;

            if (_embedHeaders)
            {
                transformed.TryGetValue(MessageHeaders.ContentType, out object contentType);

                // transform content type headers to String, so that they can be properly
                // embedded in JSON
                if (contentType != null)
                {
                    transformed[MessageHeaders.ContentType] = contentType.ToString();
                }

                payload = EmbeddedHeaderUtils.EmbedHeaders(transformed, _embeddedHeaders);
            }
            else
            {
                payload = transformed.Payload;
            }

            return IntegrationServices.MessageBuilderFactory.WithPayload(payload).CopyHeaders(transformed.Headers).Build();
        }
    }

    protected class DefaultProducingMessageChannelBinding : DefaultBinding<IMessageChannel>
    {
        private readonly AbstractMessageChannelBinder _binder;
        private readonly IProducerOptions _options;
        private readonly IProducerDestination _producerDestination;
        private readonly ILogger _logger;

        public override IDictionary<string, object> ExtendedInfo => DoGetExtendedInfo(Name, _options);

        public override bool IsInput => false;

        public DefaultProducingMessageChannelBinding(AbstractMessageChannelBinder binder, string destination, IMessageChannel target, ILifecycle lifecycle,
            IProducerOptions options, IProducerDestination producerDestination, ILogger logger = null)
            : base(destination, target, lifecycle)
        {
            _binder = binder;
            _options = options;
            _producerDestination = producerDestination;
            _logger = logger;
        }

        protected override void AfterUnbind()
        {
            try
            {
                _binder.DestroyErrorInfrastructure(_producerDestination);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, ex.Message);
            }

            _binder.AfterUnbindProducer(_producerDestination, _options);
        }
    }

    protected class DefaultConsumerMessageChannelBinding : DefaultBinding<IMessageChannel>
    {
        private readonly AbstractMessageChannelBinder _binder;
        private readonly IConsumerOptions _options;
        private readonly IConsumerDestination _destination;
        private readonly ILogger _logger;

        public override IDictionary<string, object> ExtendedInfo => DoGetExtendedInfo(_destination, _options);

        public override bool IsInput => true;

        public DefaultConsumerMessageChannelBinding(AbstractMessageChannelBinder binder, string name, string group, IMessageChannel inputChannel,
            ILifecycle lifecycle, IConsumerOptions options, IConsumerDestination consumerDestination, ILogger logger = null)
            : base(name, group, inputChannel, lifecycle)
        {
            _binder = binder;
            _options = options;
            _destination = consumerDestination;
            _logger = logger;
        }

        protected override void AfterUnbind()
        {
            try
            {
                if (Endpoint is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, ex.Message);
            }

            _binder.AfterUnbindConsumer(_destination, Group, _options);
            _binder.DestroyErrorInfrastructure(_destination, Group, _options);
        }
    }

    protected class DefaultPollableChannelBinding : DefaultBinding<IPollableSource<IMessageHandler>>
    {
        private readonly AbstractMessageChannelBinder _binder;
        private readonly IConsumerOptions _options;
        private readonly IConsumerDestination _destination;

        public override IDictionary<string, object> ExtendedInfo => DoGetExtendedInfo(_destination, _options);

        public override bool IsInput => true;

        public DefaultPollableChannelBinding(AbstractMessageChannelBinder binder, string name, string group, IPollableSource<IMessageHandler> inboundBindTarget,
            ILifecycle lifecycle, IConsumerOptions options, IConsumerDestination consumerDestination)
            : base(name, group, inboundBindTarget, lifecycle)
        {
            _binder = binder;
            _options = options;
            _destination = consumerDestination;
        }

        protected override void AfterUnbind()
        {
            _binder.AfterUnbindConsumer(_destination, Group, _options);
            _binder.DestroyErrorInfrastructure(_destination, Group, _options);
        }
    }

    protected class EmbeddedHeadersChannelInterceptor : AbstractChannelInterceptor
    {
        protected readonly ILogger Logger;

        public EmbeddedHeadersChannelInterceptor(ILogger logger = null)
        {
            Logger = logger;
        }

        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            if (message.Payload is byte[] payloadBytes && !message.Headers.ContainsKey(BinderHeaders.NativeHeadersPresent) &&
                EmbeddedHeaderUtils.MayHaveEmbeddedHeaders(payloadBytes))
            {
                MessageValues messageValues;

                try
                {
                    messageValues = EmbeddedHeaderUtils.ExtractHeaders((IMessage<byte[]>)message, true);
                }
                catch (Exception e)
                {
                    /*
                     * debug() rather then error() since we don't know for sure that it
                     * really is a message with embedded headers, it just meets the
                     * criteria in EmbeddedHeaderUtils.mayHaveEmbeddedHeaders().
                     */

                    Logger?.LogDebug(e, e.Message);
                    messageValues = new MessageValues(message);
                }

                return messageValues.ToMessage();
            }

            return message;
        }
    }
}
