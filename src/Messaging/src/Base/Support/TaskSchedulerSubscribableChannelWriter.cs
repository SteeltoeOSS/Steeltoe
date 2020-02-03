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
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Support
{
    public class TaskSchedulerSubscribableChannelWriter : AbstractMessageChannelWriter
    {
        public TaskSchedulerSubscribableChannelWriter(TaskSchedulerSubscribableChannel channel, ILogger logger = null)
            : base(channel, logger)
        {
        }

        public virtual TaskSchedulerSubscribableChannel Channel => (TaskSchedulerSubscribableChannel)channel;

        public override bool TryComplete(Exception error = null)
        {
            return false;
        }

        public override bool TryWrite(IMessage message)
        {
            return channel.Send(message, 0);
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
            }

            if (Channel.SubscriberCount > 0)
            {
                return new ValueTask<bool>(true);
            }

            return new ValueTask<bool>(false);
        }

        public override ValueTask WriteAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            return base.WriteAsync(message, cancellationToken);
        }
    }
}
