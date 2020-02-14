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

using Steeltoe.Messaging;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.Stream.TestBinder
{
    public class OutputDestination : AbstractDestination
    {
        private BlockingCollection<IMessage> _messages;

        public IMessage Receive(long timeout)
        {
            try
            {
                _messages.TryTake(out var result, TimeSpan.FromMilliseconds(timeout));
                return result;
            }
            catch (Exception)
            {
                // Log
            }

            return null;
        }

        public IMessage Receive()
        {
            return Receive(0);
        }

        protected internal override void AfterChannelIsSet()
        {
            _messages = new BlockingCollection<IMessage>();
            Channel.Subscribe(new MessageHandler(this));
        }

        private class MessageHandler : IMessageHandler
        {
            private readonly OutputDestination outputDestination;

            public MessageHandler(OutputDestination thiz)
            {
                outputDestination = thiz;
            }

            public void HandleMessage(IMessage message)
            {
                outputDestination._messages.Add(message);
            }
        }
    }
}
