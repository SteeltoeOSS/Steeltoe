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
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Channel
{
    public abstract class AbstractSubscribableChannelWriter : AbstractMessageChannelWriter
    {
        protected AbstractSubscribableChannelWriter(AbstractSubscribableChannel channel, ILogger logger = null)
            : base(channel, logger)
        {
        }

        public virtual AbstractSubscribableChannel Channel => (AbstractSubscribableChannel)channel;

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
    }
}
