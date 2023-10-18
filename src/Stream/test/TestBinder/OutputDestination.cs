// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Messaging;

namespace Steeltoe.Stream.TestBinder;

public sealed class OutputDestination : AbstractDestination
{
    private BlockingCollection<IMessage> _messages;

    public IMessage Receive(long timeout)
    {
        try
        {
            _messages.TryTake(out IMessage result, TimeSpan.FromMilliseconds(timeout));
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

    private sealed class MessageHandler : IMessageHandler
    {
        private readonly OutputDestination _outputDestination;

        public string ServiceName { get; set; }

        public MessageHandler(OutputDestination destination)
        {
            _outputDestination = destination;
            ServiceName = $"{GetType().Name}@{GetHashCode()}";
        }

        public void HandleMessage(IMessage message)
        {
            _outputDestination._messages.Add(message);
        }
    }
}
