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

using Steeltoe.Common.Contexts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core
{
    public abstract class AbstractDestinationResolvingMessagingTemplate<D>
        : AbstractMessagingTemplate<D>,
        IDestinationResolvingMessageSendingOperations<D>,
        IDestinationResolvingMessageReceivingOperations<D>,
        IDestinationResolvingMessageRequestReplyOperations<D>
    {
        private readonly IApplicationContext _context;
        private IDestinationResolver<D> _destinationResolver;

        public AbstractDestinationResolvingMessagingTemplate(IApplicationContext context)
        {
            _context = context;
        }

        public virtual IApplicationContext ApplicationContext
        {
            get
            {
                return _context;
            }
        }

        public IDestinationResolver<D> DestinationResolver
        {
            get
            {
                if (_destinationResolver == null)
                {
                    _destinationResolver = (IDestinationResolver<D>)ApplicationContext?.GetService(typeof(IDestinationResolver<D>));
                }

                return _destinationResolver;
            }

            set
            {
                _destinationResolver = value;
            }
        }

        public virtual Task ConvertAndSendAsync(string destinationName, object payload, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(destinationName, payload, null, null, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(string destinationName, object payload, IDictionary<string, object> headers, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(destinationName, payload, headers, null, cancellationToken);
        }

        public virtual Task ConvertAndSendAsync(string destinationName, object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default)
        {
            return ConvertAndSendAsync(destinationName, payload, null, postProcessor, cancellationToken);
        }

        public virtual async Task ConvertAndSendAsync(string destinationName, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default)
        {
            var destination = ResolveDestination(destinationName);
            await ConvertAndSendAsync(destination, payload, headers, postProcessor, cancellationToken);
        }

        public virtual async Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, CancellationToken cancellationToken = default)
        {
            var destination = ResolveDestination(destinationName);
            return await ConvertSendAndReceiveAsync<T>(destination, request, cancellationToken);
        }

        public virtual async Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, IDictionary<string, object> headers, CancellationToken cancellationToken = default)
        {
            var destination = ResolveDestination(destinationName);
            return await ConvertSendAndReceiveAsync<T>(destination, request, headers, cancellationToken);
        }

        public virtual async Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default)
        {
            var destination = ResolveDestination(destinationName);
            return await ConvertSendAndReceiveAsync<T>(destination, request, requestPostProcessor, cancellationToken);
        }

        public virtual async Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default)
        {
            var destination = ResolveDestination(destinationName);
            return await ConvertSendAndReceiveAsync<T>(destination, request, headers, requestPostProcessor, cancellationToken);
        }

        public virtual async Task<IMessage> ReceiveAsync(string destinationName, CancellationToken cancellationToken = default)
        {
            var destination = ResolveDestination(destinationName);
            return await ReceiveAsync(destination, cancellationToken);
        }

        public virtual async Task<T> ReceiveAndConvertAsync<T>(string destinationName, CancellationToken cancellationToken = default)
        {
            var destination = ResolveDestination(destinationName);
            return await ReceiveAndConvertAsync<T>(destination, cancellationToken);
        }

        public virtual async Task SendAsync(string destinationName, IMessage message, CancellationToken cancellationToken = default)
        {
            var destination = ResolveDestination(destinationName);
            await SendAsync(destination, message, cancellationToken);
        }

        public virtual async Task<IMessage> SendAndReceiveAsync(string destinationName, IMessage requestMessage, CancellationToken cancellationToken = default)
        {
            var destination = ResolveDestination(destinationName);
            return await SendAndReceiveAsync(destination, requestMessage, cancellationToken);
        }

        public virtual void ConvertAndSend(string destinationName, object payload)
        {
            ConvertAndSend(destinationName, payload, null, null);
        }

        public virtual void ConvertAndSend(string destinationName, object payload, IDictionary<string, object> headers)
        {
            ConvertAndSend(destinationName, payload, headers, null);
        }

        public virtual void ConvertAndSend(string destinationName, object payload, IMessagePostProcessor postProcessor)
        {
            ConvertAndSend(destinationName, payload, null, postProcessor);
        }

        public virtual void ConvertAndSend(string destinationName, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor)
        {
            var destination = ResolveDestination(destinationName);
            ConvertAndSend(destination, payload, headers, postProcessor);
        }

        public virtual T ConvertSendAndReceive<T>(string destinationName, object request)
        {
            var destination = ResolveDestination(destinationName);
            return ConvertSendAndReceive<T>(destination, request);
        }

        public virtual T ConvertSendAndReceive<T>(string destinationName, object request, IDictionary<string, object> headers)
        {
            var destination = ResolveDestination(destinationName);
            return ConvertSendAndReceive<T>(destination, request, headers);
        }

        public virtual T ConvertSendAndReceive<T>(string destinationName, object request, IMessagePostProcessor requestPostProcessor)
        {
            var destination = ResolveDestination(destinationName);
            return ConvertSendAndReceive<T>(destination, request, requestPostProcessor);
        }

        public virtual T ConvertSendAndReceive<T>(string destinationName, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor)
        {
            var destination = ResolveDestination(destinationName);
            return ConvertSendAndReceive<T>(destination, request, headers, requestPostProcessor);
        }

        public virtual IMessage Receive(string destinationName)
        {
            var destination = ResolveDestination(destinationName);
            return Receive(destination);
        }

        public virtual T ReceiveAndConvert<T>(string destinationName)
        {
            var destination = ResolveDestination(destinationName);
            return ReceiveAndConvert<T>(destination);
        }

        public virtual void Send(string destinationName, IMessage message)
        {
            var destination = ResolveDestination(destinationName);
            Send(destination, message);
        }

        public virtual IMessage SendAndReceive(string destinationName, IMessage requestMessage)
        {
            var destination = ResolveDestination(destinationName);
            return SendAndReceive(destination, requestMessage);
        }

        protected D ResolveDestination(string destinationName)
        {
            if (DestinationResolver == null)
            {
                throw new InvalidOperationException("DestinationResolver is required to resolve destination names");
            }

            return DestinationResolver.ResolveDestination(destinationName);
        }
    }
}
