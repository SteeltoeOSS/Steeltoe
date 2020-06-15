// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Messaging.Support;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Channel
{
    public class TaskSchedulerChannel : AbstractTaskSchedulerChannel
    {
        public TaskSchedulerChannel(IServiceProvider serviceProvider, TaskScheduler executor, ILogger logger = null)
            : this(serviceProvider, executor, new RoundRobinLoadBalancingStrategy(), logger)
        {
        }

        public TaskSchedulerChannel(IServiceProvider serviceProvider, TaskScheduler executor, ILoadBalancingStrategy loadBalancingStrategy, ILogger logger = null)
            : this(serviceProvider, executor, loadBalancingStrategy, null, logger)
        {
        }

        public TaskSchedulerChannel(IServiceProvider serviceProvider, TaskScheduler executor, ILoadBalancingStrategy loadBalancingStrategy, string name, ILogger logger = null)
            : base(serviceProvider, new UnicastingDispatcher(serviceProvider, executor, logger), executor, name, logger)
        {
            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            Dispatcher.MessageHandlingDecorator = new MessageHandlingDecorator(this);
            Dispatcher.LoadBalancingStrategy = loadBalancingStrategy;
            Dispatcher.MessageHandlingDecorator = new MessageHandlingDecorator(this);
            Writer = new TaskSchedulerChannelWriter(this, logger);
            Reader = new NotSupportedChannelReader();
        }

        protected UnicastingDispatcher UnicastingDispatcher => (UnicastingDispatcher)Dispatcher;
    }
}