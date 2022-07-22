// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Stream.Binder.Rabbit;

internal class TestChannelInterceptor : AbstractChannelInterceptor
{
    public Func<IMessage, IMessageChannel, IMessage> PresendHandler { get; set; }

    public override IMessage PreSend(IMessage message, IMessageChannel channel) => PresendHandler?.Invoke(message, channel) ?? message;
}