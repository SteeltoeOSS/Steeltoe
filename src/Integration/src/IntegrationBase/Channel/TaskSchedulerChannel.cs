// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Channel;

public class TaskSchedulerChannel : AbstractTaskSchedulerChannel
{
    protected UnicastingDispatcher UnicastingDispatcher => (UnicastingDispatcher)Dispatcher;

    public TaskSchedulerChannel(IApplicationContext context, TaskScheduler executor, ILogger logger = null)
        : this(context, executor, new RoundRobinLoadBalancingStrategy(), logger)
    {
    }

    public TaskSchedulerChannel(IApplicationContext context, TaskScheduler executor, ILoadBalancingStrategy loadBalancingStrategy, ILogger logger = null)
        : this(context, executor, loadBalancingStrategy, null, logger)
    {
    }

    public TaskSchedulerChannel(IApplicationContext context, TaskScheduler executor, ILoadBalancingStrategy loadBalancingStrategy, string name,
        ILogger logger = null)
        : base(context, new UnicastingDispatcher(context, executor, logger), executor, name, logger)
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
}
