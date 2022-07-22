// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using System;
using System.Threading;

namespace Steeltoe.Stream.Binder;

internal class BinderErrorChannel : PublishSubscribeChannel, ILastSubscriberAwareChannel
{
    private int _subscribers;

    private volatile ILastSubscriberMessageHandler _finalHandler;

    public BinderErrorChannel(IApplicationContext context, string name, ILogger logger)
        : base(context, name, logger)
    {
    }

    public override bool Subscribe(IMessageHandler handler)
    {
        Interlocked.Increment(ref _subscribers);
        if (handler is ILastSubscriberMessageHandler && _finalHandler != null)
        {
            throw new InvalidOperationException("Only one LastSubscriberMessageHandler is allowed");
        }

        if (_finalHandler != null)
        {
            base.Unsubscribe(_finalHandler);
        }

        var result = base.Subscribe(handler);
        if (_finalHandler != null)
        {
            base.Subscribe(_finalHandler);
        }

        if (handler is ILastSubscriberMessageHandler lastSubHandler && _finalHandler == null)
        {
            _finalHandler = lastSubHandler;
        }

        return result;
    }

    public override bool Unsubscribe(IMessageHandler handler)
    {
        Interlocked.Decrement(ref _subscribers);
        return base.Unsubscribe(handler);
    }

    public int Subscribers => _subscribers;
}