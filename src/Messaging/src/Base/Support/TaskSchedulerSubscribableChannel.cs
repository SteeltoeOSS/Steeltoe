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

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Support
{
    public class TaskSchedulerSubscribableChannel : AbstractSubscribableChannel
    {
        protected List<ITaskSchedulerChannelInterceptor> _schedulerInterceptors = new List<ITaskSchedulerChannelInterceptor>();
        private object _lock = new object();

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
                    if (interceptor is ITaskSchedulerChannelInterceptor)
                    {
                        schedulerInterceptors.Add((ITaskSchedulerChannelInterceptor)interceptor);
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

                var description = "Failed to handle " + message + " to " + this + " in " + handler;
                throw new MessageDeliveryException(message, description, ex);
            }
        }

        private void UpdateInterceptorsFor(IChannelInterceptor interceptor)
        {
            if (interceptor is ITaskSchedulerChannelInterceptor)
            {
                lock (_lock)
                {
                    var schedulerInterceptors = new List<ITaskSchedulerChannelInterceptor>(_schedulerInterceptors);
                    schedulerInterceptors.Add((ITaskSchedulerChannelInterceptor)interceptor);
                    _schedulerInterceptors = schedulerInterceptors;
                }
            }
        }

        internal struct SendTask : IMessageHandlingRunnable
        {
            private readonly TaskSchedulerSubscribableChannel channel;
            private readonly List<ITaskSchedulerChannelInterceptor> interceptors;
            private int interceptorIndex;

            public SendTask(TaskSchedulerSubscribableChannel channel, List<ITaskSchedulerChannelInterceptor> interceptors, IMessage message, IMessageHandler messageHandler)
            {
                this.channel = channel;
                Message = message;
                MessageHandler = messageHandler;
                this.interceptors = interceptors;
                interceptorIndex = -1;
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

                    var description = "Failed to handle " + message + " to " + this + " in " + MessageHandler;
                    throw new MessageDeliveryException(message, description, ex);
                }
            }

            private IMessage ApplyBeforeHandled(IMessage message)
            {
                if (interceptors.Count == 0)
                {
                    return message;
                }

                var messageToUse = message;
                foreach (var interceptor in interceptors)
                {
                    messageToUse = interceptor.BeforeHandled(messageToUse, channel, MessageHandler);
                    if (messageToUse == null)
                    {
                        var name = interceptor.GetType().Name;
                        channel.Logger?.LogDebug(name + " returned null from beforeHandle, i.e. precluding the send.");
                        TriggerAfterMessageHandled(message, null);
                        return null;
                    }

                    interceptorIndex++;
                }

                return messageToUse;
            }

            private void TriggerAfterMessageHandled(IMessage message, Exception ex)
            {
                if (interceptorIndex == -1)
                {
                    return;
                }

                for (var i = interceptorIndex; i >= 0; i--)
                {
                    var interceptor = channel._schedulerInterceptors[i];
                    try
                    {
                        interceptor.AfterMessageHandled(message, channel, MessageHandler, ex);
                    }
                    catch (Exception ex2)
                    {
                        channel.Logger?.LogError("Exception from afterMessageHandled in " + interceptor, ex2);
                    }
                }
            }
        }
    }
}
