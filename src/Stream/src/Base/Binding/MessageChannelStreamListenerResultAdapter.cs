// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Stream.Binding
{
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
            var handler = new BridgeHandler(_context);
            handler.OutputChannel = bindingTarget;

            ((ISubscribableChannel)streamListenerResult).Subscribe(handler);

            return new NoOpDisposable();
        }

        public IDisposable Adapt(object streamListenerResult, object bindingTarget)
        {
            if (streamListenerResult is IMessageChannel && bindingTarget is IMessageChannel)
            {
                return Adapt((IMessageChannel)streamListenerResult, (IMessageChannel)bindingTarget);
            }

            throw new ArgumentException("Invalid arguments, IMessageChannel required");
        }

#pragma warning disable S3881 // "IDisposable" should be implemented correctly
        public class NoOpDisposable : IDisposable
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
        {
            public void Dispose()
            {
                // TODO: Figure out disposable usage in streams
            }
        }
    }
}
