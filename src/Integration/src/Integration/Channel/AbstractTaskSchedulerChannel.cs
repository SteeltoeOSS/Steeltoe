// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Channel;

public abstract class AbstractTaskSchedulerChannel : AbstractSubscribableChannel, ITaskSchedulerChannelInterceptorAware
{
    protected TaskScheduler executor;
    protected int taskSchedulerInterceptorsSize;

    public override List<IChannelInterceptor> ChannelInterceptors
    {
        get => base.ChannelInterceptors;
        set
        {
            base.ChannelInterceptors = value;

            foreach (IChannelInterceptor interceptor in value)
            {
                if (interceptor is ITaskSchedulerChannelInterceptor)
                {
                    taskSchedulerInterceptorsSize++;
                }
            }
        }
    }

    public virtual bool HasTaskSchedulerInterceptors => taskSchedulerInterceptorsSize > 0;

    protected AbstractTaskSchedulerChannel(IApplicationContext context, IMessageDispatcher dispatcher, ILogger logger = null)
        : this(context, dispatcher, null, logger)
    {
    }

    protected AbstractTaskSchedulerChannel(IApplicationContext context, IMessageDispatcher dispatcher, TaskScheduler executor, ILogger logger = null)
        : this(context, dispatcher, executor, null, logger)
    {
    }

    protected AbstractTaskSchedulerChannel(IApplicationContext context, IMessageDispatcher dispatcher, TaskScheduler executor, string name,
        ILogger logger = null)
        : base(context, dispatcher, name, logger)
    {
        this.executor = executor;
    }

    public override void AddInterceptor(IChannelInterceptor interceptor)
    {
        base.AddInterceptor(interceptor);

        if (interceptor is ITaskSchedulerChannelInterceptor)
        {
            taskSchedulerInterceptorsSize++;
        }
    }

    public override void AddInterceptor(int index, IChannelInterceptor interceptor)
    {
        base.AddInterceptor(index, interceptor);

        if (interceptor is ITaskSchedulerChannelInterceptor)
        {
            taskSchedulerInterceptorsSize++;
        }
    }

    public override bool RemoveInterceptor(IChannelInterceptor interceptor)
    {
        bool removed = base.RemoveInterceptor(interceptor);

        if (removed && interceptor is ITaskSchedulerChannelInterceptor)
        {
            taskSchedulerInterceptorsSize--;
        }

        return removed;
    }

    public override IChannelInterceptor RemoveInterceptor(int index)
    {
        IChannelInterceptor interceptor = base.RemoveInterceptor(index);

        if (interceptor is ITaskSchedulerChannelInterceptor)
        {
            taskSchedulerInterceptorsSize--;
        }

        return interceptor;
    }

    protected class MessageHandlingTask : IMessageHandlingRunnable
    {
        private readonly IMessageHandlingRunnable _runnable;
        private readonly AbstractTaskSchedulerChannel _channel;
        private readonly ILogger _logger;

        public IMessage Message => _runnable.Message;

        public IMessageHandler MessageHandler => _runnable.MessageHandler;

        public MessageHandlingTask(AbstractTaskSchedulerChannel channel, IMessageHandlingRunnable task, ILogger logger)
        {
            _channel = channel;
            _runnable = task;
            _logger = logger;
        }

        public bool Run()
        {
            IMessage message = _runnable.Message;
            IMessageHandler messageHandler = _runnable.MessageHandler;

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

                string description = $"Failed to handle {message} to {this} in {messageHandler}";
                throw new MessageDeliveryException(message, description, ex);
            }
        }

        private IMessage ApplyBeforeHandle(IMessage message, Queue<ITaskSchedulerChannelInterceptor> interceptorStack)
        {
            IMessage theMessage = message;

            foreach (IChannelInterceptor interceptor in _channel.ChannelInterceptors)
            {
                if (interceptor is ITaskSchedulerChannelInterceptor executorInterceptor)
                {
                    theMessage = executorInterceptor.BeforeHandled(theMessage, _channel, _runnable.MessageHandler);

                    if (theMessage == null)
                    {
                        _logger?.LogDebug("{name} returned null from beforeHandle, i.e. precluding the send.", executorInterceptor.GetType().Name);
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
            IEnumerable<ITaskSchedulerChannelInterceptor> iterator = interceptorStack.Reverse();

            foreach (ITaskSchedulerChannelInterceptor interceptor in iterator)
            {
                try
                {
                    interceptor.AfterMessageHandled(message, _channel, _runnable.MessageHandler, ex);
                }
                catch (Exception ex2)
                {
                    _logger?.LogError(ex2, "Exception from afterMessageHandled in {interceptor}", interceptor);
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
            IMessageHandlingRunnable runnable = messageHandlingRunnable;

            if (_channel.HasTaskSchedulerInterceptors)
            {
                runnable = new MessageHandlingTask(_channel, messageHandlingRunnable, _channel.Logger);
            }

            return runnable;
        }
    }
}
