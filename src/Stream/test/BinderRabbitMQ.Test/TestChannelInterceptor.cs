// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Stream.Binder.Rabbit;

internal sealed class TestChannelInterceptor : AbstractChannelInterceptor
{
    public Func<IMessage, IMessageChannel, IMessage> PreSendHandler { get; set; }

    public override IMessage PreSend(IMessage message, IMessageChannel channel) => PreSendHandler?.Invoke(message, channel) ?? message;
}
