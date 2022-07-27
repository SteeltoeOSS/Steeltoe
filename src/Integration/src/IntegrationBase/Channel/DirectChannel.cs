// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Channel;

/// <summary>
///  A channel that invokes a single subscriber for each sent Message.
///  The invocation will occur in the sender's thread.
/// </summary>
public class DirectChannel : AbstractSubscribableChannel
{
    public DirectChannel(ILogger logger = null)
        : this(null, logger)
    {
    }

    public DirectChannel(IApplicationContext context, ILogger logger = null)
        : this(context, new RoundRobinLoadBalancingStrategy(), null, logger)
    {
    }

    public DirectChannel(IApplicationContext context, string name, ILogger logger = null)
        : this(context, new RoundRobinLoadBalancingStrategy(), name, logger)
    {
    }

    public DirectChannel(IApplicationContext context, ILoadBalancingStrategy loadBalancingStrategy, string name, ILogger logger = null)
        : base(context, new UnicastingDispatcher(context), name, logger)
    {
        Dispatcher.LoadBalancingStrategy = loadBalancingStrategy;
        Dispatcher.MaxSubscribers = int.MaxValue;
        Writer = new DirectChannelWriter(this, logger);
        Reader = new NotSupportedChannelReader();
    }
}