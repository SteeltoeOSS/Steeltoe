// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Integration;
using Steeltoe.Integration.Endpoint;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Provisioning;
using System;
using System.Collections.Generic;
using System.Threading;
using static Steeltoe.Stream.TestBinder.TestChannelBinderProvisioner;

namespace Steeltoe.Stream.TestBinder;

public class TestChannelBinder : AbstractPollableMessageSourceBinder
{
    private readonly ILogger _logger;

    public TestChannelBinder(IApplicationContext context, TestChannelBinderProvisioner provisioningProvider, ILogger<TestChannelBinder> logger)
        : base(context, Array.Empty<string>(), provisioningProvider, logger)
    {
        _logger = logger;
    }

    public IMessage LastError { get; private set; }

    public override string ServiceName { get; set; } = "testbinder";

    public IMessageSource MessageSourceDelegate { get; set; } = new MessageSource();

    public override void Dispose()
    {
        // Nothing to do here
    }

    protected override IMessageHandler CreateProducerMessageHandler(IProducerDestination destination, IProducerOptions producerProperties, IMessageChannel errorChannel)
    {
        var handler = new BridgeHandler(ApplicationContext)
        {
            OutputChannel = ((SpringIntegrationProducerDestination)destination).Channel
        };
        return handler;
    }

    protected override IMessageProducer CreateConsumerEndpoint(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
    {
        IErrorMessageStrategy errorMessageStrategy = new DefaultErrorMessageStrategy();
        var siBinderInputChannel = ((SpringIntegrationConsumerDestination)destination).Channel;

        var messageListenerContainer = new TestMessageListeningContainer();
        var endpoint = new TestMessageProducerSupportEndpoint(ApplicationContext, messageListenerContainer, _logger);

        var groupName = !string.IsNullOrEmpty(group) ? group : "anonymous";
        var errorInfrastructure = RegisterErrorInfrastructure(destination, groupName, consumerOptions, _logger);
        if (consumerOptions.MaxAttempts > 1)
        {
            endpoint.RetryTemplate = BuildRetryTemplate(consumerOptions);
            endpoint.RecoveryCallback = errorInfrastructure.Recoverer;
        }
        else
        {
            endpoint.ErrorMessageStrategy = errorMessageStrategy;
            endpoint.ErrorChannel = errorInfrastructure.ErrorChannel;
        }

        endpoint.Init();

        siBinderInputChannel.Subscribe(messageListenerContainer);

        return endpoint;
    }

    protected override IMessageHandler GetErrorMessageHandler(IConsumerDestination destination, string group, IConsumerOptions consumerOptions)
    {
        return new ErrorMessageHandler(this);
    }

    protected override PolledConsumerResources CreatePolledConsumerResources(string name, string group, IConsumerDestination destination, IConsumerOptions consumerOptions)
    {
        return new PolledConsumerResources(MessageSourceDelegate, RegisterErrorInfrastructure(destination, group, consumerOptions, _logger));
    }

    public class ErrorMessageHandler : ILastSubscriberMessageHandler
    {
        public ErrorMessageHandler(TestChannelBinder binder)
        {
            Binder = binder;
            ServiceName = GetType().Name + "@" + GetHashCode();
        }

        public TestChannelBinder Binder { get; }

        public virtual string ServiceName { get; set; }

        public void HandleMessage(IMessage message)
        {
            Binder.LastError = message;
        }
    }

    public class MessageSource : IMessageSource
    {
        public IMessage Receive()
        {
            var message = Message.Create(
                "polled data",
                new MessageHeaders(new Dictionary<string, object>() { { MessageHeaders.CONTENT_TYPE, "text/plain" } }));
            return message;
        }
    }

    public class TestMessageListeningContainer : IMessageHandler
    {
        public TestMessageListeningContainer()
        {
            ServiceName = GetType().Name + "@" + GetHashCode();
        }

        public void HandleMessage(IMessage message)
        {
            MessageListener.Invoke(message);
        }

        public virtual string ServiceName { get; set; }

        public Action<IMessage> MessageListener { get; set; }
    }

    public class TestMessageProducerSupportEndpoint : MessageProducerSupportEndpoint
    {
        private static readonly AsyncLocal<IAttributeAccessor> _attributesHolder = new ();
        private readonly TestMessageListeningContainer _messageListenerContainer;

        public TestMessageProducerSupportEndpoint(IApplicationContext context, TestMessageListeningContainer messageListenerContainer, ILogger logger)
            : base(context, logger)
        {
            _messageListenerContainer = messageListenerContainer;
        }

        public RetryTemplate RetryTemplate { get; set; }

        public IRecoveryCallback RecoveryCallback { get; set; }

        public void Init()
        {
            if (RetryTemplate != null && ErrorChannel != null)
            {
                throw new InvalidOperationException(
                    "Cannot have an 'errorChannel' property when a 'RetryTemplate' is "
                    + "provided; use an 'ErrorMessageSendingRecoverer' in the 'recoveryCallback' property to "
                    + "send an error message when retries are exhausted");
            }

            var messageListener = new Listener(this);
            if (RetryTemplate != null)
            {
                RetryTemplate.RegisterListener(messageListener);
            }

            _messageListenerContainer.MessageListener = (m) => messageListener.Accept(m);
        }

        protected class Listener : IRetryListener
        {
            private readonly TestMessageProducerSupportEndpoint _adapter;

            public Listener(TestMessageProducerSupportEndpoint adapter)
            {
                _adapter = adapter;
            }

            public void Accept(IMessage message)
            {
                try
                {
                    if (_adapter.RetryTemplate == null)
                    {
                        try
                        {
                            ProcessMessage(message);
                        }
                        finally
                        {
                            _attributesHolder.Value = null;
                        }
                    }
                    else
                    {
                        _adapter.RetryTemplate.Execute((ctx) => ProcessMessage(message), _adapter.RecoveryCallback);
                    }
                }
                catch (Exception e)
                {
                    if (_adapter.ErrorChannel != null)
                    {
                        _adapter.MessagingTemplate
                            .Send(
                                _adapter.ErrorChannel,
                                _adapter.BuildErrorMessage(null, new InvalidOperationException("Message conversion failed: " + message, e)));
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            public bool Open(IRetryContext context)
            {
                if (_adapter.RecoveryCallback != null)
                {
                    _attributesHolder.Value = context;
                }

                return true;
            }

            public void Close(IRetryContext context, Exception exception)
            {
                _attributesHolder.Value = null;
            }

            public void OnError(IRetryContext context, Exception exception)
            {
                // Ignore
            }

            private void ProcessMessage(IMessage message)
            {
                _adapter.SendMessage(message);
            }
        }
    }
}