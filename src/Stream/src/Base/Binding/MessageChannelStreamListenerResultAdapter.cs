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

using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Stream.Binding
{
    public class MessageChannelStreamListenerResultAdapter : IStreamListenerResultAdapter
    {
        private readonly IServiceProvider _serviceProvider;

        public MessageChannelStreamListenerResultAdapter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool Supports(Type resultType, Type bindingTarget)
        {
            return typeof(IMessageChannel).IsAssignableFrom(resultType) && typeof(IMessageChannel).IsAssignableFrom(bindingTarget);
        }

        public IDisposable Adapt(IMessageChannel streamListenerResult, IMessageChannel bindingTarget)
        {
            var handler = new BridgeHandler(_serviceProvider);
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
