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
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Support
{
    public abstract class AbstractMessageChannelWriter : ChannelWriter<IMessage>
    {
        protected AbstractMessageChannel channel;
        protected ILogger logger;

        public AbstractMessageChannelWriter(AbstractMessageChannel channel, ILogger logger = null)
        {
            this.channel = channel ?? throw new ArgumentNullException(nameof(channel));
            this.logger = logger;
        }

        public override bool TryComplete(Exception error = null)
        {
            return false;
        }

        public override bool TryWrite(IMessage message)
        {
            return channel.Send(message);
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested ? new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken)) : new ValueTask<bool>(true);
        }

        public override ValueTask WriteAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            if (TryWrite(message))
            {
                return default;
            }

            return new ValueTask(Task.FromException(new MessageDeliveryException(message)));
        }
    }
}
