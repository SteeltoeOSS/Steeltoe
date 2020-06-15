// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Integration.Channel
{
    /// <summary>
    ///  A channel that invokes a single subscriber for each sent Message.
    ///  The invocation will occur in the sender's thread.
    /// </summary>
    public class DirectChannel : AbstractSubscribableChannel
    {
        public DirectChannel(IServiceProvider serviceProvider, ILogger logger = null)
            : this(serviceProvider, new RoundRobinLoadBalancingStrategy(), null, logger)
        {
        }

        public DirectChannel(IServiceProvider serviceProvider, string name, ILogger logger = null)
            : this(serviceProvider, new RoundRobinLoadBalancingStrategy(), name, logger)
        {
        }

        public DirectChannel(IServiceProvider serviceProvider, ILoadBalancingStrategy loadBalancingStrategy, string name, ILogger logger = null)
            : base(serviceProvider, new UnicastingDispatcher(serviceProvider), name, logger)
        {
            Dispatcher.LoadBalancingStrategy = loadBalancingStrategy;
            Dispatcher.MaxSubscribers = int.MaxValue;
            Writer = new DirectChannelWriter(this, logger);
            Reader = new NotSupportedChannelReader();
        }
    }
}
