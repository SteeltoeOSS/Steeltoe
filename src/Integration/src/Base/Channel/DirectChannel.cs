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
using Steeltoe.Common.Contexts;
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
}
