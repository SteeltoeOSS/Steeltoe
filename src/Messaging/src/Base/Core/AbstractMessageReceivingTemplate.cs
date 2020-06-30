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

using Steeltoe.Messaging.Converter;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core
{
    public abstract class AbstractMessageReceivingTemplate<D> : AbstractMessageSendingTemplate<D>, IMessageReceivingOperations<D>
    {
        private D _defaultReceiveDestination;

        public virtual D DefaultReceiveDestination
        {
            get
            {
                return _defaultReceiveDestination;
            }

            set
            {
                _defaultReceiveDestination = value;
            }
        }

        public virtual bool ThrowReceivedExceptions { get; set; } = false;

        public virtual Task<IMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return DoReceiveAsync(RequiredDefaultReceiveDestination, cancellationToken);
        }

        public virtual Task<IMessage> ReceiveAsync(D destination, CancellationToken cancellationToken = default)
        {
            return DoReceiveAsync(destination, cancellationToken);
        }

        public virtual Task<T> ReceiveAndConvertAsync<T>(CancellationToken cancellationToken = default)
        {
            return ReceiveAndConvertAsync<T>(RequiredDefaultReceiveDestination);
        }

        public virtual async Task<T> ReceiveAndConvertAsync<T>(D destination, CancellationToken cancellationToken = default)
        {
            var message = await DoReceiveAsync(destination, cancellationToken);
            if (message != null)
            {
                return DoConvert<T>(message);
            }
            else
            {
                return default(T);
            }
        }

        public virtual IMessage Receive()
        {
            return DoReceive(RequiredDefaultReceiveDestination);
        }

        public virtual IMessage Receive(D destination)
        {
            return DoReceive(destination);
        }

        public virtual T ReceiveAndConvert<T>()
        {
            return ReceiveAndConvert<T>(RequiredDefaultReceiveDestination);
        }

        public virtual T ReceiveAndConvert<T>(D destination)
        {
            var message = DoReceive(destination);
            if (message != null)
            {
                return DoConvert<T>(message);
            }
            else
            {
                return default;
            }
        }

        protected virtual D RequiredDefaultReceiveDestination
        {
            get
            {
                if (_defaultReceiveDestination == null)
                {
                    throw new InvalidOperationException("No default destination configured");
                }

                return _defaultReceiveDestination;
            }
        }

        protected abstract Task<IMessage> DoReceiveAsync(D destination, CancellationToken cancellationToken);

        protected abstract IMessage DoReceive(D destination);

        protected virtual T DoConvert<T>(IMessage message)
        {
            var messageConverter = MessageConverter;
            var value = messageConverter.FromMessage(message, typeof(T));
            if (value == null)
            {
                throw new MessageConversionException(
                    message,
                    "Unable to convert payload [" + message.Payload + "] to type [" + typeof(T) + "] using converter [" + messageConverter + "]");
            }

            if (value is Exception && ThrowReceivedExceptions)
            {
                throw (Exception)value;
            }

            return (T)value;
        }
    }
}
