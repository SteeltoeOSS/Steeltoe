// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Provisioning;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Steeltoe.Stream.Binder
{
    public abstract class AbstractMessageChannelBinder : AbstractBinder<IMessageChannel>
    {
        protected readonly IProvisioningProvider _provisioningProvider;
        protected readonly EmbeddedHeadersChannelInterceptor _embeddedHeadersChannelInterceptor = new EmbeddedHeadersChannelInterceptor();
        protected readonly string[] _headersToEmbed;
        protected bool _producerBindingExist;

        protected AbstractMessageChannelBinder(
            IApplicationContext context,
            string[] headersToEmbed,
            IProvisioningProvider provisioningProvider,
            ILogger logger)
        : this(context, headersToEmbed, provisioningProvider, null, null, logger)
        {
        }

        protected AbstractMessageChannelBinder(
            IApplicationContext context,
            string[] headersToEmbed,
            IProvisioningProvider provisioningProvider,
            IListenerContainerCustomizer containerCustomizer,
            IMessageSourceCustomizer sourceCustomizer,
            ILogger logger)
            : base(context, logger)
        {
            _headersToEmbed = headersToEmbed ?? (new string[0]);
            _provisioningProvider = provisioningProvider;
            ListenerContainerCustomizer = containerCustomizer;
            MessageSourceCustomizer = sourceCustomizer;
        }

        public override Type TargetType { get; } = typeof(IMessageChannel);

        protected virtual IListenerContainerCustomizer ListenerContainerCustomizer { get; }

        protected virtual IMessageSourceCustomizer MessageSourceCustomizer { get; }

        protected override IBinding DoBindProducer(string name, IMessageChannel outboundTarget, IProducerOptions producerOptions)
        {
            if (!(outboundTarget is ISubscribableChannel))
            {
                throw new ArgumentException("Binding is supported only for ISubscribableChannel instances");
            }

            IMessageHandler producerMessageHandler;
            IProducerDestination producerDestination;

            try
            {
                producerDestination = _provisioningProvider.ProvisionProducerDestination(name, producerOptions);
                var errorChannel = producerOptions.ErrorChannelEnabled ? RegisterErrorInfrastructure(producerDestination) : null;
                producerMessageHandler = CreateProducerMessageHandler(producerDestination, producerOptions, outboundTarget, errorChannel);
            }
            catch (Exception e) when (e is BinderException || e is ProvisioningException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new BinderException("Exception thrown while building outbound endpoint", e);
            }

            if (producerOptions.AutoStartup && producerMessageHandler is ILifecycle)
            {
                ((ILifecycle)producerMessageHandler).Start();
            }

            PostProcessOutputChannel(outboundTarget, producerOptions);
            var sendingHandler = new SendingHandler(ApplicationContext, producerMessageHandler, HeaderMode.EmbeddedHeaders.Equals(producerOptions.HeaderMode), _headersToEmbed, UseNativeEncoding(producerOptions));
            sendingHandler.Initialize();
            ((ISubscribableChannel)outboundTarget).Subscribe(sendingHandler);

            IBinding binding = new DefaultProducingMessageChannelBinding(
                this,
                name,
                outboundTarget,
                producerMessageHandler is ILifecycle ? (ILifecycle)producerMessageHandler : null,
                producerOptions,
                producerDestination);

            _producerBindingExist = true;
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

        protected virtual IMessageHandler CreateProducerMessageHandler(IProducerDestination destination, IProducerOptions producerProperties, IMessageChannel channel, IMessageChannel errorChannel)
        {
            return CreateProducerMessageHandler(destination, producerProperties, errorChannel);
        }

        protected abstract IMessageHandler CreateProducerMessageHandler(IProducerDestination destination, IProducerOptions producerProperties, IMessageChannel errorChannel);

        protected virtual void AfterUnbindProducer(IProducerDestination destination, IProducerOptions producerOptions)
        {
        }

        protected override IBinding DoBindConsumer(string name, string group, IMessageChannel inputTarget, IConsumerOptions consumerOptions)
        {
            IMessageProducer consumerEndpoint = null;
            try
            {
                var destination = _provisioningProvider.ProvisionConsumerDestination(name, group, consumerOptions);

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

                if (consumerOptions.AutoStartup && consumerEndpoint is ILifecycle)
                {
                    ((ILifecycle)consumerEndpoint).Start();
                }

                IBinding binding = new DefaultConsumerMessageChannelBinding(
                            this,
                            name,
                            group,
                            inputTarget,
                            consumerEndpoint is ILifecycle ? (ILifecycle)consumerEndpoint : null,
                            consumerOptions,
                            destination);

                return binding;
            }
            catch (Exception e)
            {
                if (consumerEndpoint is ILifecycle)
                {
                    ((ILifecycle)consumerEndpoint).Stop();
                }

                if (e is BinderException)
                {
                    throw;
                }
                else if (e is ProvisioningException)
                {
                    throw;
                }
                else
                {
                    throw new BinderException("Exception thrown while starting consumer: ", e);
                }
            }
        }

        protected abstract IMessageProducer CreateConsumerEndpoint(IConsumerDestination destination, string group, IConsumerOptions consumerOptions);

        protected virtual void AfterUnbindConsumer(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
        {
        }

        protected virtual ErrorInfrastructure RegisterErrorInfrastructure(IConsumerDestination destination, string group, IConsumerOptions consumerOptions, ILogger logger)
        {
            return RegisterErrorInfrastructure(destination, group, consumerOptions, false, logger);
        }

        protected virtual ErrorInfrastructure RegisterErrorInfrastructure(IConsumerDestination destination, string group, IConsumerOptions consumerOptions, bool polled, ILogger logger)
        {
            var errorMessageStrategy = GetErrorMessageStrategy();

            var errorChannelName = GetErrorsBaseName(destination, group, consumerOptions);
            ISubscribableChannel errorChannel;
            var errorChannelObject = ApplicationContext.GetService<IMessageChannel>(errorChannelName);
            if (errorChannelObject != null)
            {
                if (!(errorChannelObject is ISubscribableChannel))
                {
                    throw new ArgumentException("Error channel '" + errorChannelName + "' must be a ISubscribableChannel");
                }

                errorChannel = (ISubscribableChannel)errorChannelObject;
            }
            else
            {
                errorChannel = new BinderErrorChannel(ApplicationContext, errorChannelName, logger);
                ApplicationContext.Register(errorChannelName, errorChannel);
            }

            ErrorMessageSendingRecoverer recoverer;
            if (errorMessageStrategy == null)
            {
                recoverer = new ErrorMessageSendingRecoverer(ApplicationContext, errorChannel);
            }
            else
            {
                recoverer = new ErrorMessageSendingRecoverer(ApplicationContext, errorChannel, errorMessageStrategy);
            }

            var recovererBeanName = GetErrorRecovererName(destination, group, consumerOptions);
            ApplicationContext.Register(recovererBeanName, recoverer);

            IMessageHandler handler;
            if (polled)
            {
                handler = GetPolledConsumerErrorMessageHandler(destination, group, consumerOptions);
            }
            else
            {
                handler = GetErrorMessageHandler(destination, group, consumerOptions);
            }

            var defaultErrorChannel = ApplicationContext.GetService<IMessageChannel>(IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME);

            if (handler == null && errorChannel is ILastSubscriberAwareChannel)
            {
                handler = GetDefaultErrorMessageHandler((ILastSubscriberAwareChannel)errorChannel, defaultErrorChannel != null);
            }

            var errorMessageHandlerName = GetErrorMessageHandlerName(destination, group, consumerOptions);

            if (handler != null)
            {
                handler.ServiceName = errorMessageHandlerName;
                if (IsSubscribable(errorChannel))
                {
                    var errorHandler = handler;
                    ApplicationContext.Register(errorMessageHandlerName, errorHandler);
                    errorChannel.Subscribe(handler);
                }
                else
                {
                    // this.logger.warn("The provided errorChannel '" + errorChannelName
                    //        + "' is an instance of DirectChannel, "
                    //        + "so no more subscribers could be added which may affect DLQ processing. "
                    //        + "Resolution: Configure your own errorChannel as "
                    //        + "an instance of PublishSubscribeChannel");
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

                    var errorBridgeHandlerName = GetErrorBridgeName(destination, group, consumerOptions);
                    errorBridge.ServiceName = errorBridgeHandlerName;
                    ApplicationContext.Register(errorBridgeHandlerName, errorBridge);
                }
                else
                {
                    // this.logger.warn("The provided errorChannel '" + errorChannelName
                    //        + "' is an instance of DirectChannel, "
                    //        + "so no more subscribers could be added and no error messages will be sent to global error channel. "
                    //        + "Resolution: Configure your own errorChannel as "
                    //        + "an instance of PublishSubscribeChannel");
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
            return GetErrorsBaseName(destination, group, consumerOptions) + ".recoverer";
        }

        protected virtual string GetErrorMessageHandlerName(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
        {
            return GetErrorsBaseName(destination, group, consumerOptions) + ".handler";
        }

        protected virtual string GetErrorsBaseName(IProducerDestination destination)
        {
            return destination.Name + ".errors";
        }

        protected virtual string GetErrorsBaseName(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
        {
            return destination.Name + "." + group + ".errors";
        }

        protected virtual string GetErrorBridgeName(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
        {
            return GetErrorsBaseName(destination, group, consumerOptions) + ".bridge";
        }

        protected virtual string GetErrorBridgeName(IProducerDestination destination)
        {
            return GetErrorsBaseName(destination) + ".bridge";
        }

        private ISubscribableChannel RegisterErrorInfrastructure(IProducerDestination destination)
        {
            var errorChannelName = GetErrorsBaseName(destination);
            ISubscribableChannel errorChannel;
            var errorChannelObject = ApplicationContext.GetService<IMessageChannel>(errorChannelName);
            if (errorChannelObject != null)
            {
                if (!(errorChannelObject is ISubscribableChannel))
                {
                    throw new InvalidOperationException("Error channel '" + errorChannelName + "' must be a ISubscribableChannel");
                }

                errorChannel = (ISubscribableChannel)errorChannelObject;
            }
            else
            {
                errorChannel = new PublishSubscribeChannel(ApplicationContext);
                ApplicationContext.Register(errorChannelName, errorChannel);
            }

            var defaultErrorChannel = (IMessageChannel)ApplicationContext.GetService<IMessageChannel>(IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME);

            if (defaultErrorChannel != null)
            {
                var errorBridge = new BridgeHandler(ApplicationContext)
                {
                    OutputChannel = defaultErrorChannel
                };
                errorChannel.Subscribe(errorBridge);
                var errorBridgeHandlerName = GetErrorBridgeName(destination);
                ApplicationContext.Register(errorBridgeHandlerName, errorBridge);
            }

            return errorChannel;
        }

        private bool IsSubscribable(ISubscribableChannel errorChannel)
        {
            if (errorChannel is PublishSubscribeChannel)
            {
                return true;
            }

            return errorChannel is Integration.Channel.AbstractSubscribableChannel ? ((Integration.Channel.AbstractSubscribableChannel)errorChannel).SubscriberCount == 0 : true;
        }

        private void DestroyErrorInfrastructure(IProducerDestination destination)
        {
            var errorChannelName = GetErrorsBaseName(destination);
            var errorBridgeHandlerName = GetErrorBridgeName(destination);
            if (ApplicationContext.GetService<IMessageChannel>(errorChannelName) is ISubscribableChannel channel)
            {
                if (ApplicationContext.GetService<IMessageHandler>(errorBridgeHandlerName) is IMessageHandler bridgeHandler)
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
                var recoverer = GetErrorRecovererName(destination, group, options);

                DestroyBean(recoverer);

                var errorChannelName = GetErrorsBaseName(destination, group, options);
                var errorMessageHandlerName = GetErrorMessageHandlerName(destination, group, options);
                var errorBridgeHandlerName = GetErrorBridgeName(destination, group, options);

                if (ApplicationContext.GetService<IMessageChannel>(errorChannelName) is ISubscribableChannel channel)
                {
                    if (ApplicationContext.GetService<IMessageHandler>(errorBridgeHandlerName) is IMessageHandler bridgeHandler)
                    {
                        channel.Unsubscribe(bridgeHandler);
                        DestroyBean(errorBridgeHandlerName);
                    }

                    if (ApplicationContext.GetService<IMessageHandler>(errorMessageHandlerName) is IMessageHandler handler)
                    {
                        channel.Unsubscribe(handler);
                        DestroyBean(errorMessageHandlerName);
                    }

                    DestroyBean(errorChannelName);
                }
            }
            catch (Exception)
            {
                // Log ... context is shutting down.
            }
        }

        private void DestroyBean(string beanName)
        {
            ApplicationContext.Deregister(beanName);
        }

        private Dictionary<string, object> DoGetExtendedInfo(object destination, object properties)
        {
            var extendedInfo = new Dictionary<string, object>();
            extendedInfo.Add("bindingDestination", destination.ToString());

            object value;
            if (properties is string)
            {
                value = JsonSerializer.Deserialize<Dictionary<string, object>>((string)properties);
            }
            else
            {
                value = properties;
            }

            extendedInfo.Add(properties.GetType().Name, value);
            return extendedInfo;
        }

        private void EnhanceMessageChannel(IMessageChannel inputChannel)
        {
            ((Integration.Channel.AbstractMessageChannel)inputChannel).AddInterceptor(0, _embeddedHeadersChannelInterceptor);
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
            public ErrorInfrastructure(ISubscribableChannel errorChannel, ErrorMessageSendingRecoverer recoverer, IMessageHandler handler)
            {
                ErrorChannel = errorChannel;
                Recoverer = recoverer;
                Handler = handler;
            }

            public ISubscribableChannel ErrorChannel { get; }

            public ErrorMessageSendingRecoverer Recoverer { get; }

            public IMessageHandler Handler { get; }
        }

        protected class PolledConsumerResources
        {
            public PolledConsumerResources(IMessageSource source, ErrorInfrastructure errorInfrastructure)
            {
                Source = source;
                ErrorInfrastructure = errorInfrastructure;
            }

            protected internal IMessageSource Source { get; }

            protected internal ErrorInfrastructure ErrorInfrastructure { get; }
        }

        protected class SendingHandler : AbstractMessageHandler, ILifecycle
        {
            private readonly bool _embedHeaders;

            private readonly string[] _embeddedHeaders;

            private readonly IMessageHandler _handler;

            private readonly bool _useNativeEncoding;

            public SendingHandler(IApplicationContext context, IMessageHandler handler, bool embedHeaders, string[] headersToEmbed, bool useNativeEncoding)
                : base(context)
            {
                this._handler = handler;
                this._embedHeaders = embedHeaders;
                _embeddedHeaders = headersToEmbed;
                this._useNativeEncoding = useNativeEncoding;
            }

            public override void Initialize()
            {
            }

            public Task Start()
            {
                if (_handler is ILifecycle)
                {
                    return ((ILifecycle)_handler).Start();
                }

                return Task.CompletedTask;
            }

            public Task Stop()
            {
                if (_handler is ILifecycle)
                {
                    return ((ILifecycle)_handler).Stop();
                }

                return Task.CompletedTask;
            }

            public bool IsRunning
            {
                get
                {
                    return _handler is ILifecycle && ((ILifecycle)_handler).IsRunning;
                }
            }

            protected override void HandleMessageInternal(IMessage message)
            {
                var messageToSend = _useNativeEncoding ? message : SerializeAndEmbedHeadersIfApplicable(message);
                _handler.HandleMessage(messageToSend);
            }

            private IMessage SerializeAndEmbedHeadersIfApplicable(IMessage message)
            {
                var transformed = new MessageValues(message);

                object payload;
                if (_embedHeaders)
                {
                    transformed.TryGetValue(MessageHeaders.CONTENT_TYPE, out var contentType);

                    // transform content type headers to String, so that they can be properly
                    // embedded in JSON
                    if (contentType != null)
                    {
                        transformed[MessageHeaders.CONTENT_TYPE] = contentType.ToString();
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

            public DefaultProducingMessageChannelBinding(
                AbstractMessageChannelBinder binder,
                string destination,
                IMessageChannel target,
                ILifecycle lifecycle,
                IProducerOptions options,
                IProducerDestination producerDestination)
                : base(destination, target, lifecycle)
            {
                this._binder = binder;
                this._options = options;
                this._producerDestination = producerDestination;
            }

            // @Override
            public override IDictionary<string, object> ExtendedInfo
            {
                get { return _binder.DoGetExtendedInfo(Name, _options); }
            }

            public override bool IsInput
            {
                get { return false; }
            }

            protected override void AfterUnbind()
            {
                try
                {
                    _binder.DestroyErrorInfrastructure(_producerDestination);
                }
                catch (Exception)
                {
                    // Log
                }

                _binder.AfterUnbindProducer(_producerDestination, _options);
            }
        }

        protected class DefaultConsumerMessageChannelBinding : DefaultBinding<IMessageChannel>
        {
            private readonly AbstractMessageChannelBinder _binder;
            private readonly IConsumerOptions _options;
            private readonly IConsumerDestination _destination;

            public DefaultConsumerMessageChannelBinding(
                AbstractMessageChannelBinder binder,
                string name,
                string group,
                IMessageChannel inputChannel,
                ILifecycle lifecycle,
                IConsumerOptions options,
                IConsumerDestination consumerDestination)
                : base(name, group, inputChannel, lifecycle)
            {
                this._binder = binder;
                this._options = options;
                _destination = consumerDestination;
            }

            public override IDictionary<string, object> ExtendedInfo
            {
                get { return _binder.DoGetExtendedInfo(_destination, _options); }
            }

            public override bool IsInput
            {
                get { return true; }
            }

            protected override void AfterUnbind()
            {
                // TODO: Figure out IDisposable/Closeable usage
                // try
                // {
                //    if (Endpoint is IDisposable)
                //    {
                //        ((IDisposable)Endpoint).Dispose();
                //    }
                // }
                // catch (Exception)
                // {
                //    // Log
                // }
                _binder.AfterUnbindConsumer(_destination, Group, _options);
                _binder.DestroyErrorInfrastructure(_destination, Group, _options);
            }
        }

        protected class DefaultPollableChannelBinding : DefaultBinding<IPollableSource<IMessageHandler>>
        {
            private readonly AbstractMessageChannelBinder _binder;
            private readonly IConsumerOptions _options;
            private readonly IConsumerDestination _destination;

            public DefaultPollableChannelBinding(
                        AbstractMessageChannelBinder binder,
                        string name,
                        string group,
                        IPollableSource<IMessageHandler> inboundBindTarget,
                        ILifecycle lifecycle,
                        IConsumerOptions options,
                        IConsumerDestination consumerDestination)
                  : base(name, group, inboundBindTarget, lifecycle)
            {
                this._binder = binder;
                this._options = options;
                _destination = consumerDestination;
            }

            public override IDictionary<string, object> ExtendedInfo
            {
                get { return _binder.DoGetExtendedInfo(_destination, _options); }
            }

            public override bool IsInput
            {
                get { return true; }
            }

            protected override void AfterUnbind()
            {
                _binder.AfterUnbindConsumer(_destination, Group, _options);
                _binder.DestroyErrorInfrastructure(_destination, Group, _options);
            }
        }

        protected class EmbeddedHeadersChannelInterceptor : AbstractChannelInterceptor
        {
            protected readonly ILogger logger;

            public EmbeddedHeadersChannelInterceptor(ILogger logger = null)
            {
                this.logger = logger;
            }

            public override IMessage PreSend(IMessage message, IMessageChannel channel)
            {
                if (message.Payload is byte[]
                        && !message.Headers.ContainsKey(BinderHeaders.NATIVE_HEADERS_PRESENT)
                        && EmbeddedHeaderUtils.MayHaveEmbeddedHeaders((byte[])message.Payload))
                {
                    MessageValues messageValues;
                    try
                    {
                        messageValues = EmbeddedHeaderUtils.ExtractHeaders((IMessage<byte[]>)message, true);
                    }
                    catch (Exception)
                    {
                        /*
                         * debug() rather then error() since we don't know for sure that it
                         * really is a message with embedded headers, it just meets the
                         * criteria in EmbeddedHeaderUtils.mayHaveEmbeddedHeaders().
                         */

                        // this.logger?.LogDebug(EmbeddedHeaderUtils.DecodeExceptionMessage(message), e);
                        messageValues = new MessageValues(message);
                    }

                    return messageValues.ToMessage();
                }

                return message;
            }
        }
    }
}
