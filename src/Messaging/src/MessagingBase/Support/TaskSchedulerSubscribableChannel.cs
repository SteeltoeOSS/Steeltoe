// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Support;

public class TaskSchedulerSubscribableChannel : AbstractSubscribableChannel
{
    protected List<ITaskSchedulerChannelInterceptor> _schedulerInterceptors = new ();
    private object _lock = new ();

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

    protected TaskScheduler Scheduler { get; }

    protected TaskFactory Factory { get; }

    public override void SetInterceptors(List<IChannelInterceptor> interceptors)
    {
        base.SetInterceptors(interceptors);
        lock (_lock)
        {
            var schedulerInterceptors = new List<ITaskSchedulerChannelInterceptor>();
            foreach (var interceptor in interceptors)
            {
                if (interceptor is ITaskSchedulerChannelInterceptor interceptor1)
                {
                    schedulerInterceptors.Add(interceptor1);
                }
            }

            _schedulerInterceptors = schedulerInterceptors;
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
        var interceptors = _schedulerInterceptors;
        var handlers = _handlers;

        foreach (var handler in handlers)
        {
            if (Scheduler == null)
            {
                Invoke(interceptors, message, handler);
            }
            else
            {
                var task = Factory.StartNew(() =>
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

            var description = $"Failed to handle {message} to {this} in {handler}";
            throw new MessageDeliveryException(message, description, ex);
        }
    }

    private void UpdateInterceptorsFor(IChannelInterceptor interceptor)
    {
        if (interceptor is ITaskSchedulerChannelInterceptor interceptor1)
        {
            lock (_lock)
            {
                var schedulerInterceptors = new List<ITaskSchedulerChannelInterceptor>(_schedulerInterceptors)
                {
                    interceptor1
                };
                _schedulerInterceptors = schedulerInterceptors;
            }
        }
    }

    internal struct SendTask : IMessageHandlingRunnable
    {
        private readonly TaskSchedulerSubscribableChannel _channel;
        private readonly List<ITaskSchedulerChannelInterceptor> _interceptors;
        private int _interceptorIndex;

        public SendTask(TaskSchedulerSubscribableChannel channel, List<ITaskSchedulerChannelInterceptor> interceptors, IMessage message, IMessageHandler messageHandler)
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
            var message = Message;
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

                var description = $"Failed to handle {message} to {this} in {MessageHandler}";
                throw new MessageDeliveryException(message, description, ex);
            }
        }

        private IMessage ApplyBeforeHandled(IMessage message)
        {
            if (_interceptors.Count == 0)
            {
                return message;
            }

            var messageToUse = message;
            foreach (var interceptor in _interceptors)
            {
                messageToUse = interceptor.BeforeHandled(messageToUse, _channel, MessageHandler);
                if (messageToUse == null)
                {
                    var name = interceptor.GetType().Name;
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

            for (var i = _interceptorIndex; i >= 0; i--)
            {
                var interceptor = _channel._schedulerInterceptors[i];
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
