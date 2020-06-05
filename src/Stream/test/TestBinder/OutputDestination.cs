// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
