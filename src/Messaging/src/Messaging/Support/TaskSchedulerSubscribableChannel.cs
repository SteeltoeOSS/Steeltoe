// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Messaging.Support;

public class TaskSchedulerSubscribableChannel : AbstractSubscribableChannel
{
    private readonly object _lock = new();
    protected List<ITaskSchedulerChannelInterceptor> schedulerInterceptors = new();

    protected TaskScheduler Scheduler { get; }

    protected TaskFactory Factory { get; }

    public TaskSchedulerSubscribableChannel(ILogger logger = null)
        : this(null, logger)
    {
    }

    public TaskSchedulerSubscribableChannel(TaskScheduler scheduler, ILogger logger = null)
        : base(logger)
    {
        Scheduler = scheduler;

        if (Scheduler != null)
        {
            Factory = new TaskFactory(Scheduler);
        }

        Writer = new TaskSchedulerSubscribableChannelWriter(this, logger);
        Reader = new NotSupportedChannelReader();
    }

    public override void SetInterceptors(List<IChannelInterceptor> interceptors)
    {
        base.SetInterceptors(interceptors);

        lock (_lock)
        {
            var newInterceptors = new List<ITaskSchedulerChannelInterceptor>();

            foreach (IChannelInterceptor interceptor in interceptors)
            {
                if (interceptor is ITaskSchedulerChannelInterceptor interceptor1)
                {
                    newInterceptors.Add(interceptor1);
                }
            }

            schedulerInterceptors = newInterceptors;
        }
    }

    public override void AddInterceptor(IChannelInterceptor interceptor)
    {
        base.AddInterceptor(interceptor);
        UpdateInterceptorsFor(interceptor);
    }

    public override void AddInterceptor(int index, IChannelInterceptor interceptor)
    {
        base.AddInterceptor(index, interceptor);
        UpdateInterceptorsFor(interceptor);
    }

    protected override bool DoSendInternal(IMessage message, CancellationToken cancellationToken)
    {
        List<ITaskSchedulerChannelInterceptor> interceptors = schedulerInterceptors;
        HashSet<IMessageHandler> handlers = Handlers;

        foreach (IMessageHandler handler in handlers)
        {
            if (Scheduler == null)
            {
                Invoke(interceptors, message, handler);
            }
            else
            {
                ConfiguredTaskAwaitable task = Factory.StartNew(() =>
                {
                    Invoke(interceptors, message, handler);
                }).ConfigureAwait(false);

                task.GetAwaiter().GetResult();
            }
        }

        return true;
    }

    private void Invoke(List<ITaskSchedulerChannelInterceptor> interceptors, IMessage message, IMessageHandler handler)
    {
        if (interceptors.Count > 0)
        {
            var sendTask = new SendTask(this, interceptors, message, handler);
            sendTask.Run();
        }
        else
        {
            InvokeDirect(message, handler);
        }
    }

    private void InvokeDirect(IMessage message, IMessageHandler handler)
    {
        try
        {
            handler.HandleMessage(message);
        }
        catch (Exception ex)
        {
            if (ex is MessagingException)
            {
                throw;
            }

            string description = $"Failed to handle {message} to {this} in {handler}";
            throw new MessageDeliveryException(message, description, ex);
        }
    }

    private void UpdateInterceptorsFor(IChannelInterceptor interceptor)
    {
        if (interceptor is ITaskSchedulerChannelInterceptor interceptor1)
        {
            lock (_lock)
            {
                var interceptors = new List<ITaskSchedulerChannelInterceptor>(schedulerInterceptors)
                {
                    interceptor1
                };

                schedulerInterceptors = interceptors;
            }
        }
    }

    internal struct SendTask : IMessageHandlingRunnable
    {
        private readonly TaskSchedulerSubscribableChannel _channel;
        private readonly List<ITaskSchedulerChannelInterceptor> _interceptors;
        private int _interceptorIndex;

        public SendTask(TaskSchedulerSubscribableChannel channel, List<ITaskSchedulerChannelInterceptor> interceptors, IMessage message,
            IMessageHandler messageHandler)
        {
            _channel = channel;
            Message = message;
            MessageHandler = messageHandler;
            _interceptors = interceptors;
            _interceptorIndex = -1;
        }

        public IMessage Message { get; }

        public IMessageHandler MessageHandler { get; }

        public bool Run()
        {
            IMessage message = Message;

            try
            {
                message = ApplyBeforeHandled(message);

                if (message == null)
                {
                    return false;
                }

                MessageHandler.HandleMessage(message);
                TriggerAfterMessageHandled(message, null);
                return true;
            }
            catch (Exception ex)
            {
                TriggerAfterMessageHandled(message, ex);

                if (ex is MessagingException)
                {
                    throw;
                }

                string description = $"Failed to handle {message} to {this} in {MessageHandler}";
                throw new MessageDeliveryException(message, description, ex);
            }
        }

        private IMessage ApplyBeforeHandled(IMessage message)
        {
            if (_interceptors.Count == 0)
            {
                return message;
            }

            IMessage messageToUse = message;

            foreach (ITaskSchedulerChannelInterceptor interceptor in _interceptors)
            {
                messageToUse = interceptor.BeforeHandled(messageToUse, _channel, MessageHandler);

                if (messageToUse == null)
                {
                    string name = interceptor.GetType().Name;
                    _channel.Logger?.LogDebug("{name} returned null from beforeHandle, i.e. precluding the send.", name);
                    TriggerAfterMessageHandled(message, null);
                    return null;
                }

                _interceptorIndex++;
            }

            return messageToUse;
        }

        private void TriggerAfterMessageHandled(IMessage message, Exception ex)
        {
            if (_interceptorIndex == -1)
            {
                return;
            }

            for (int i = _interceptorIndex; i >= 0; i--)
            {
                ITaskSchedulerChannelInterceptor interceptor = _channel.schedulerInterceptors[i];

                try
                {
                    interceptor.AfterMessageHandled(message, _channel, MessageHandler, ex);
                }
                catch (Exception ex2)
                {
                    _channel.Logger?.LogError(ex2, "Exception from afterMessageHandled in {interceptor}", interceptor);
                }
            }
        }
    }
}
