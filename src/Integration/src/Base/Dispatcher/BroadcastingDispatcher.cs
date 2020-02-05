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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Dispatcher
{
    public class BroadcastingDispatcher : AbstractDispatcher
    {
        private readonly bool _requireSubscribers;

        private IMessageBuilderFactory _messageBuilderFactory;

        public BroadcastingDispatcher(IServiceProvider serviceProvider, ILogger logger = null)
            : this(serviceProvider, null, false, logger)
        {
        }

        public BroadcastingDispatcher(IServiceProvider serviceProvider, TaskScheduler executor, ILogger logger = null)
            : this(serviceProvider, executor, false, logger)
        {
        }

        public BroadcastingDispatcher(IServiceProvider serviceProvider, bool requireSubscribers, ILogger logger = null)
            : this(serviceProvider, null, requireSubscribers, logger)
        {
        }

        public BroadcastingDispatcher(IServiceProvider serviceProvider, TaskScheduler executor, bool requireSubscribers, ILogger logger = null)
            : base(serviceProvider, executor, logger)
        {
            _requireSubscribers = requireSubscribers;
        }

        public virtual bool IgnoreFailures { get; set; }

        public virtual bool ApplySequence { get; set; }

        public virtual int MinSubscribers { get; set; }

        internal IMessageBuilderFactory MessageBuilderFactory
        {
            get
            {
                if (_messageBuilderFactory == null)
                {
                    _messageBuilderFactory = _serviceProvider.GetService<IMessageBuilderFactory>();
                }

                if (_messageBuilderFactory == null)
                {
                    _messageBuilderFactory = new DefaultMessageBuilderFactory();
                }

                return _messageBuilderFactory;
            }

            set
            {
                _messageBuilderFactory = value;
            }
        }

        internal ILogger Logger => _logger;

        protected override bool DoDispatch(IMessage message, CancellationToken cancellationToken)
        {
            var dispatched = 0;
            var sequenceNumber = 1;
            var handlers = _handlers;

            if (_requireSubscribers && handlers.Count == 0)
            {
                throw new MessageDispatchingException(message, "Dispatcher has no subscribers");
            }

            var sequenceSize = handlers.Count;
            var messageToSend = message;
            Guid? sequenceId = null;

            if (ApplySequence)
            {
                sequenceId = message.Headers.Id;
            }

            foreach (var handler in handlers)
            {
                if (ApplySequence)
                {
                    messageToSend = MessageBuilderFactory
                            .FromMessage(message)
                            .PushSequenceDetails(sequenceId, sequenceNumber++, sequenceSize)
                            .Build();
                    if (message is IMessageDecorator)
                    {
                        messageToSend = ((IMessageDecorator)message).DecorateMessage(messageToSend);
                    }
                }

                if (_executor != null)
                {
                    var task = CreateMessageHandlingTask(handler, messageToSend);
                    _factory.StartNew(() =>
                    {
                        task.Run();
                    });
                    dispatched++;
                }
                else
                {
                    InvokeHandler(handler, messageToSend);
                    dispatched++;
                }
            }

            if (dispatched == 0 && MinSubscribers == 0)
            {
                if (sequenceSize > 0)
                {
                    Logger?.LogDebug("No subscribers received message, default behavior is ignore");
                }
                else
                {
                    Logger?.LogDebug("No subscribers, default behavior is ignore");
                }
            }

            return dispatched >= MinSubscribers;
        }

        protected void InvokeHandler(IMessageHandler handler, IMessage message)
        {
            try
            {
                handler.HandleMessage(message);
            }
            catch (Exception e)
            {
                if (!IgnoreFailures)
                {
                    if (_factory != null && ErrorHandler != null)
                    {
                        ErrorHandler.HandleError(e);
                        return;
                    }

                    if (e is MessagingException && ((MessagingException)e).FailedMessage == null)
                    {
                        throw new MessagingException(message, "Failed to handle Message", e);
                    }

                    throw;
                }

                _logger?.LogWarning("Suppressing Exception since 'ignoreFailures' is set to TRUE.", e);
            }
        }

        private IMessageHandlingRunnable CreateMessageHandlingTask(IMessageHandler handler, IMessage message)
        {
            var messageHandlingRunnable = new MessageHandlingRunnable(this, handler, message);

            if (MessageHandlingDecorator != null)
            {
                return MessageHandlingDecorator.Decorate(messageHandlingRunnable);
            }

            return messageHandlingRunnable;
        }

        private class MessageHandlingRunnable : IMessageHandlingRunnable
        {
            private readonly BroadcastingDispatcher _dispatcher;

            public MessageHandlingRunnable(BroadcastingDispatcher dispatcher, IMessageHandler handler, IMessage message)
            {
                _dispatcher = dispatcher;
                Message = message;
                MessageHandler = handler;
            }

            public bool Run()
            {
                _dispatcher.InvokeHandler(MessageHandler, Message);
                return true;
            }

            public IMessage Message { get; }

            public IMessageHandler MessageHandler { get; }
        }
    }
}
