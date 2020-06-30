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
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Channel
{
    public class NullChannel : Channel<IMessage>, IPollableChannel
    {
        private readonly ILogger _logger;

        public string ServiceName { get; set; } = IntegrationContextUtils.NULL_CHANNEL_BEAN_NAME;

        public NullChannel(ILogger logger = null)
        {
            _logger = logger;
            Writer = new NotSupportedChannelWriter();
            Reader = new NotSupportedChannelReader();
        }

        public IMessage Receive()
        {
            _logger?.LogDebug("receive called on null channel");
            return null;
        }

        public IMessage Receive(int timeout)
        {
            _logger?.LogDebug("receive called on null channel");
            return null;
        }

        public ValueTask<IMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("receive called on null channel");
            return new ValueTask<IMessage>((IMessage)null);
        }

        public bool Send(IMessage message)
        {
            _logger?.LogDebug("message sent to null channel: " + message);
            return true;
        }

        public bool Send(IMessage message, int timeout)
        {
            _logger?.LogDebug("message sent to null channel: " + message);
            return Send(message);
        }

        public ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            _logger?.LogDebug("message sent to null channel: " + message);
            return new ValueTask<bool>(false);
        }
    }
}
