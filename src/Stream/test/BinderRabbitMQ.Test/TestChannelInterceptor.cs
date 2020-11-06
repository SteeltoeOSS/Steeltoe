using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Stream.Binder.Rabbit
{
    internal class TestChannelInterceptor : AbstractChannelInterceptor
    {
        public Func<IMessage, IMessageChannel, IMessage> PresendHandler { get; set; }

        public override IMessage PreSend(IMessage message, IMessageChannel channel) => PresendHandler?.Invoke(message, channel) ?? message;
    }
}