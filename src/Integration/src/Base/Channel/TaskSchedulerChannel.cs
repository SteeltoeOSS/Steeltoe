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
using System.Threading.Tasks;

namespace Steeltoe.Integration.Channel
{
    public class TaskSchedulerChannel : AbstractTaskSchedulerChannel
    {
        public TaskSchedulerChannel(IApplicationContext context, TaskScheduler executor, ILogger logger = null)
            : this(context, executor, new RoundRobinLoadBalancingStrategy(), logger)
        {
        }

        public TaskSchedulerChannel(IApplicationContext context, TaskScheduler executor, ILoadBalancingStrategy loadBalancingStrategy, ILogger logger = null)
            : this(context, executor, loadBalancingStrategy, null, logger)
        {
        }

        public TaskSchedulerChannel(IApplicationContext context, TaskScheduler executor, ILoadBalancingStrategy loadBalancingStrategy, string name, ILogger logger = null)
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

        protected UnicastingDispatcher UnicastingDispatcher => (UnicastingDispatcher)Dispatcher;
    }
}