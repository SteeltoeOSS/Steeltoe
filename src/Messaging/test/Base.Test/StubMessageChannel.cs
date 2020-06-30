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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Test
{
    internal class StubMessageChannel : ISubscribableChannel
    {
        private readonly List<IMessage<byte[]>> messages = new List<IMessage<byte[]>>();

        private readonly List<IMessageHandler> handlers = new List<IMessageHandler>();

        public string ServiceName { get; set; } = "StubMessageChannel";

        public ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken)
        {
            messages.Add((IMessage<byte[]>)message);
            return new ValueTask<bool>(true);
        }

        public bool Send(IMessage message, int timeout)
        {
            messages.Add((IMessage<byte[]>)message);
            return true;
        }

        public bool Subscribe(IMessageHandler handler)
        {
            handlers.Add(handler);
            return true;
        }

        public bool Unsubscribe(IMessageHandler handler)
        {
            handlers.Remove(handler);
            return true;
        }

        public bool Send(IMessage message)
        {
            return Send(message, -1);
        }
    }
}
