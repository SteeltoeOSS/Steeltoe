﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Channel
{
    public abstract class AbstractPollableChannel : AbstractMessageChannel, IPollableChannel, ITaskSchedulerChannelInterceptorAware
    {
        protected AbstractPollableChannel(IApplicationContext context, ILogger logger = null)
            : base(context, logger)
        {
        }

        protected AbstractPollableChannel(IApplicationContext context, string name, ILogger logger = null)
            : base(context, name, logger)
        {
        }

        public int TaskSchedulerInterceptorsSize { get; private set; }

        public virtual ValueTask<IMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<IMessage>(DoReceive(cancellationToken));
        }

        public virtual IMessage Receive()
        {
            return Receive(-1);
        }

        public virtual IMessage Receive(int timeout)
        {
            return DoReceive(timeout);
        }

        protected virtual IMessage DoReceive(int timeout)
        {
            if (timeout == 0)
            {
                return DoReceive();
            }
            else
            {
                using (var source = new CancellationTokenSource())
                {
                    source.CancelAfter(timeout);
                    return DoReceive(source.Token);
                }
            }
        }

        protected virtual IMessage DoReceive(CancellationToken cancellationToken = default)
        {
            var interceptorList = Interceptors;
            Stack<IChannelInterceptor> interceptorStack = null;
            try
            {
                Logger?.LogTrace("PreReceive on channel '" + this + "'");
                if (interceptorList.Count > 0)
                {
                    interceptorStack = new Stack<IChannelInterceptor>();

                    if (!interceptorList.PreReceive(this, interceptorStack))
                    {
                        return null;
                    }
                }

                var message = DoReceiveInternal(cancellationToken);

                if (message == null)
                {
                    Logger?.LogTrace("PostReceive on channel '" + ServiceName + "', message is null");
                }
                else
                {
                    Logger?.LogDebug("PostReceive on channel '" + ServiceName + "', message: " + message);
                }

                if (interceptorStack != null && message != null)
                {
                    message = interceptorList.PostReceive(message, this);
                }

                interceptorList.AfterReceiveCompletion(message, this, null, interceptorStack);
                return message;
            }
            catch (Exception ex)
            {
                interceptorList.AfterReceiveCompletion(null, this, ex, interceptorStack);
                throw;
            }
        }

        protected abstract IMessage DoReceiveInternal(CancellationToken cancellationToken);

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
                        TaskSchedulerInterceptorsSize++;
                    }
                }
            }
        }

        public override void AddInterceptor(IChannelInterceptor interceptor)
        {
            base.AddInterceptor(interceptor);
            if (interceptor is ITaskSchedulerChannelInterceptor)
            {
                TaskSchedulerInterceptorsSize++;
            }
        }

        public override void AddInterceptor(int index, IChannelInterceptor interceptor)
        {
            base.AddInterceptor(index, interceptor);
            if (interceptor is ITaskSchedulerChannelInterceptor)
            {
                TaskSchedulerInterceptorsSize++;
            }
        }

        public override bool RemoveInterceptor(IChannelInterceptor interceptor)
        {
            var removed = base.RemoveInterceptor(interceptor);
            if (removed && interceptor is ITaskSchedulerChannelInterceptor)
            {
                TaskSchedulerInterceptorsSize--;
            }

            return removed;
        }

        public override IChannelInterceptor RemoveInterceptor(int index)
        {
            var interceptor = base.RemoveInterceptor(index);
            if (interceptor is ITaskSchedulerChannelInterceptor)
            {
                TaskSchedulerInterceptorsSize--;
            }

            return interceptor;
        }

        public virtual bool HasTaskSchedulerInterceptors
        {
            get { return TaskSchedulerInterceptorsSize > 0; }
        }
    }
}
