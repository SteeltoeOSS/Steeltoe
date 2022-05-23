// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Channel
{
    public abstract class AbstractTaskSchedulerChannel : AbstractSubscribableChannel, ITaskSchedulerChannelInterceptorAware
    {
        protected TaskScheduler _executor;
        protected int _taskSchedulerInterceptorsSize;

        protected AbstractTaskSchedulerChannel(IApplicationContext context, IMessageDispatcher dispatcher, ILogger logger = null)
            : this(context, dispatcher, null, logger)
        {
        }

        protected AbstractTaskSchedulerChannel(IApplicationContext context, IMessageDispatcher dispatcher, TaskScheduler executor, ILogger logger = null)
            : this(context, dispatcher, executor, null, logger)
        {
        }

        protected AbstractTaskSchedulerChannel(IApplicationContext context, IMessageDispatcher dispatcher, TaskScheduler executor, string name, ILogger logger = null)
            : base(context, dispatcher, name, logger)
        {
            _executor = executor;
        }

        public override List<IChannelInterceptor> ChannelInterceptors
        {
            get
            {
                return base.ChannelInterceptors;
            }

            set
            {
                base.ChannelInterceptors = value;
                foreach (var interceptor in value)
                {
                    if (interceptor is ITaskSchedulerChannelInterceptor)
                    {
                        _taskSchedulerInterceptorsSize++;
                    }
                }
            }
        }

        public override void AddInterceptor(IChannelInterceptor interceptor)
        {
            base.AddInterceptor(interceptor);
            if (interceptor is ITaskSchedulerChannelInterceptor)
            {
                _taskSchedulerInterceptorsSize++;
            }
        }

        public override void AddInterceptor(int index, IChannelInterceptor interceptor)
        {
            base.AddInterceptor(index, interceptor);
            if (interceptor is ITaskSchedulerChannelInterceptor)
            {
                _taskSchedulerInterceptorsSize++;
            }
        }

        public override bool RemoveInterceptor(IChannelInterceptor interceptor)
        {
            var removed = base.RemoveInterceptor(interceptor);
            if (removed && interceptor is ITaskSchedulerChannelInterceptor)
            {
                _taskSchedulerInterceptorsSize--;
            }

            return removed;
        }

        public override IChannelInterceptor RemoveInterceptor(int index)
        {
            var interceptor = base.RemoveInterceptor(index);
            if (interceptor is ITaskSchedulerChannelInterceptor)
            {
                _taskSchedulerInterceptorsSize--;
            }

            return interceptor;
        }

        public virtual bool HasTaskSchedulerInterceptors
        {
            get { return _taskSchedulerInterceptorsSize > 0; }
        }

        protected class MessageHandlingTask : IMessageHandlingRunnable
        {
            private readonly IMessageHandlingRunnable _runnable;
            private readonly AbstractTaskSchedulerChannel _channel;
            private readonly ILogger _logger;

            public MessageHandlingTask(AbstractTaskSchedulerChannel channel, IMessageHandlingRunnable task, ILogger logger)
            {
                _channel = channel;
                _runnable = task;
                _logger = logger;
            }

            public IMessage Message { get => _runnable.Message; }

            public IMessageHandler MessageHandler { get => _runnable.MessageHandler; }

            public bool Run()
            {
                var message = _runnable.Message;
                var messageHandler = _runnable.MessageHandler;
                if (messageHandler == null)
                {
                    throw new InvalidOperationException("'messageHandler' must not be null");
                }

                var interceptorStack = new Queue<ITaskSchedulerChannelInterceptor>();
                try
                {
                    if (_channel.HasTaskSchedulerInterceptors)
                    {
                        message = ApplyBeforeHandle(message, interceptorStack);
                        if (message == null)
                        {
                            return true;
                        }
                    }

                    messageHandler.HandleMessage(message);

                    if (interceptorStack.Count > 0)
                    {
                        TriggerAfterMessageHandled(message, null, interceptorStack);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    if (interceptorStack.Count > 0)
                    {
                        TriggerAfterMessageHandled(message, ex, interceptorStack);
                    }

                    if (ex is MessagingException exception)
                    {
                        throw new MessagingExceptionWrapperException(message, exception);
                    }

                    var description = $"Failed to handle {message} to {this} in {messageHandler}";
                    throw new MessageDeliveryException(message, description, ex);
                }
            }

            private IMessage ApplyBeforeHandle(IMessage message, Queue<ITaskSchedulerChannelInterceptor> interceptorStack)
            {
                var theMessage = message;
                foreach (var interceptor in _channel.ChannelInterceptors)
                {
                    if (interceptor is ITaskSchedulerChannelInterceptor executorInterceptor)
                    {
                        theMessage = executorInterceptor.BeforeHandled(theMessage, _channel, _runnable.MessageHandler);
                        if (theMessage == null)
                        {
                            _logger?.LogDebug(executorInterceptor.GetType().Name + " returned null from beforeHandle, i.e. precluding the send.");
                            TriggerAfterMessageHandled(null, null, interceptorStack);
                            return null;
                        }

                        interceptorStack.Enqueue(executorInterceptor);
                    }
                }

                return theMessage;
            }

            private void TriggerAfterMessageHandled(IMessage message, Exception ex, Queue<ITaskSchedulerChannelInterceptor> interceptorStack)
            {
                var iterator = interceptorStack.Reverse();
                foreach (var interceptor in iterator)
                {
                    try
                    {
                        interceptor.AfterMessageHandled(message, _channel, _runnable.MessageHandler, ex);
                    }
                    catch (Exception ex2)
                    {
                        _logger?.LogError("Exception from afterMessageHandled in " + interceptor, ex2);
                    }
                }
            }
        }

        protected class MessageHandlingDecorator : IMessageHandlingDecorator
        {
            private readonly AbstractTaskSchedulerChannel _channel;

            public MessageHandlingDecorator(AbstractTaskSchedulerChannel channel)
            {
                _channel = channel;
            }

            public IMessageHandlingRunnable Decorate(IMessageHandlingRunnable messageHandlingRunnable)
            {
                var runnable = messageHandlingRunnable;
                if (_channel.HasTaskSchedulerInterceptors)
                {
                    runnable = new MessageHandlingTask(_channel, messageHandlingRunnable, _channel.logger);
                }

                return runnable;
            }
        }
    }
}
