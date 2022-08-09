// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;

namespace Steeltoe.Stream.Binding;

public class MessageChannelStreamListenerResultAdapter : IStreamListenerResultAdapter
{
    private readonly IApplicationContext _context;

    public MessageChannelStreamListenerResultAdapter(IApplicationContext context)
    {
        _context = context;
    }

    public bool Supports(Type resultType, Type bindingTarget)
    {
        return typeof(IMessageChannel).IsAssignableFrom(resultType) && typeof(IMessageChannel).IsAssignableFrom(bindingTarget);
    }

    public IDisposable Adapt(IMessageChannel streamListenerResult, IMessageChannel bindingTarget)
    {
        var handler = new BridgeHandler(_context)
        {
            OutputChannel = bindingTarget
        };

        ((ISubscribableChannel)streamListenerResult).Subscribe(handler);

        return new NoOpDisposable();
    }

    public IDisposable Adapt(object streamListenerResult, object bindingTarget)
    {
        if (streamListenerResult is not IMessageChannel listenerResult)
        {
            throw new ArgumentException($"Value must be of type {nameof(IMessageChannel)}.", nameof(streamListenerResult));
        }

        if (bindingTarget is not IMessageChannel target)
        {
            throw new ArgumentException($"Value must be of type {nameof(IMessageChannel)}.", nameof(bindingTarget));
        }

        return Adapt(listenerResult, target);
    }

    public sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
