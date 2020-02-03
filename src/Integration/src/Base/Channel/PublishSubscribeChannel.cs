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
using Steeltoe.Common.Util;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Messaging.Support;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Channel
{
    public class PublishSubscribeChannel : AbstractTaskSchedulerChannel
    {
        public PublishSubscribeChannel(IServiceProvider serviceProvider, ILogger logger = null)
        : this(serviceProvider, null, null, logger)
        {
        }

        public PublishSubscribeChannel(IServiceProvider serviceProvider, string name, ILogger logger = null)
            : this(serviceProvider, null, name, logger)
        {
        }

        public PublishSubscribeChannel(IServiceProvider serviceProvider, TaskScheduler executor, ILogger logger = null)
            : this(serviceProvider, executor, null, logger)
        {
        }

        public PublishSubscribeChannel(IServiceProvider serviceProvider, TaskScheduler executor, string name, ILogger logger = null)
            : base(serviceProvider, new BroadcastingDispatcher(serviceProvider, executor), executor, name, logger)
        {
            BroadcastingDispatcher.IgnoreFailures = false;
            BroadcastingDispatcher.ApplySequence = false;
            BroadcastingDispatcher.MinSubscribers = 0;
            Dispatcher.MessageHandlingDecorator = new MessageHandlingDecorator(this);
            Writer = new PublishSubscribeChannelWriter(this, logger);
            Reader = new NotSupportedChannelReader();
        }

        public override string ComponentType
        {
            get { return "publish-subscribe-channel"; }
        }

        public virtual IErrorHandler ErrorHandler
        {
            get { return BroadcastingDispatcher.ErrorHandler; }
            set { BroadcastingDispatcher.ErrorHandler = value; }
        }

        public virtual bool IgnoreFailures
        {
            get { return BroadcastingDispatcher.IgnoreFailures; }
            set { BroadcastingDispatcher.IgnoreFailures = value; }
        }

        public virtual bool ApplySequence
        {
            get { return BroadcastingDispatcher.ApplySequence; }
            set { BroadcastingDispatcher.ApplySequence = value; }
        }

        public virtual int MinSubscribers
        {
            get { return BroadcastingDispatcher.MinSubscribers; }
            set { BroadcastingDispatcher.MinSubscribers = value; }
        }

        protected BroadcastingDispatcher BroadcastingDispatcher => (BroadcastingDispatcher)Dispatcher;
    }
}
