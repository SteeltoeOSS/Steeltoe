// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core
{
    public abstract class AbstractMessageReceivingTemplate<D> : AbstractMessageSendingTemplate<D>, IMessageReceivingOperations<D>
    {
        public virtual Task<IMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return DoReceiveAsync(RequiredDefaultDestination, cancellationToken);
        }

        public virtual Task<IMessage> ReceiveAsync(D destination, CancellationToken cancellationToken = default)
        {
            return DoReceiveAsync(destination, cancellationToken);
        }

        public virtual Task<T> ReceiveAndConvertAsync<T>(CancellationToken cancellationToken = default)
        {
            return ReceiveAndConvertAsync<T>(RequiredDefaultDestination);
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
                var result = default(T);
                return await Task.FromResult(result);
            }
        }

        public virtual IMessage Receive()
        {
            return DoReceive(RequiredDefaultDestination);
        }

        public virtual IMessage Receive(D destination)
        {
            return DoReceive(destination);
        }

        public virtual T ReceiveAndConvert<T>()
        {
            return ReceiveAndConvert<T>(RequiredDefaultDestination);
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

        protected abstract Task<IMessage> DoReceiveAsync(D destination, CancellationToken cancellationToken);

        protected abstract IMessage DoReceive(D destination);

        protected virtual T DoConvert<T>(IMessage message)
        {
            var messageConverter = MessageConverter;
            var value = messageConverter.FromMessage<T>(message);
            if (value == null)
            {
                throw new MessageConversionException(
                    message,
                    "Unable to convert payload [" + message.Payload + "] to type [" + typeof(T) + "] using converter [" + messageConverter + "]");
            }

            return value;
        }
    }
}
