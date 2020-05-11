// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Util;
using Steeltoe.Integration;
using Steeltoe.Integration.Endpoint;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Retry;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Provisioning;
using System;
using System.Collections.Generic;
using System.Threading;
using static Steeltoe.Stream.TestBinder.TestChannelBinderProvisioner;

namespace Steeltoe.Stream.TestBinder
{
    public class TestChannelBinder : AbstractPollableMessageSourceBinder
    {
        public TestChannelBinder(IServiceProvider serviceProvider, TestChannelBinderProvisioner provisioningProvider)
            : base(serviceProvider, Array.Empty<string>(), provisioningProvider)
        {
        }

        public IMessage LastError { get; private set; }

        public override string Name => "testbinder";

        public IMessageSource MessageSourceDelegate { get; set; } = new MessageSource();

        protected override IMessageHandler CreateProducerMessageHandler(IProducerDestination destination, IProducerOptions producerProperties, IMessageChannel errorChannel)
        {
            var handler = new BridgeHandler(ServiceProvider)
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
            var endpoint = new TestMessageProducerSupportEndpoint(ServiceProvider, messageListenerContainer);

            var groupName = !string.IsNullOrEmpty(group) ? group : "anonymous";
            var errorInfrastructure = RegisterErrorInfrastructure(destination, groupName, consumerOptions);
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
            return new PolledConsumerResources(MessageSourceDelegate, RegisterErrorInfrastructure(destination, group, consumerOptions));
        }

        public class ErrorMessageHandler : ILastSubscriberMessageHandler
        {
            public ErrorMessageHandler(TestChannelBinder binder)
            {
                Binder = binder;
            }

            public TestChannelBinder Binder { get; }

            public void HandleMessage(IMessage message)
            {
                Binder.LastError = message;
            }
        }

        public class MessageSource : IMessageSource
        {
            public IMessage Receive()
            {
                var message = new GenericMessage(
                    "polled data",
                    new MessageHeaders(new Dictionary<string, object>() { { MessageHeaders.CONTENT_TYPE, "text/plain" } }));
                return message;
            }
        }

        public class TestMessageListeningContainer : IMessageHandler
        {
            public void HandleMessage(IMessage message)
            {
                MessageListener.Invoke(message);
            }

            public Action<IMessage> MessageListener { get; set; }
        }

        public class TestMessageProducerSupportEndpoint : MessageProducerSupportEndpoint
        {
            private static readonly AsyncLocal<IAttributeAccessor> _attributesHolder = new AsyncLocal<IAttributeAccessor>();
            private readonly TestMessageListeningContainer _messageListenerContainer;

            public TestMessageProducerSupportEndpoint(IServiceProvider serviceProvider, TestMessageListeningContainer messageListenerContainer)
                : base(serviceProvider)
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
}
