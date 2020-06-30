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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core
{
    public abstract class AbstractMessagingTemplate<D>
        : AbstractMessageReceivingTemplate<D>,
        IMessageRequestReplyOperations<D>
    {
        public virtual Task<T> ConvertSendAndReceiveAsync<T>(object request, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(RequiredDefaultSendDestination, request, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(destination, request, (IDictionary<string, object>)null, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, IDictionary<string, object> headers, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(destination, request, headers, null, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(object request, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(RequiredDefaultSendDestination, request, requestPostProcessor, cancellationToken);
        }

        public virtual Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default)
        {
            return ConvertSendAndReceiveAsync<T>(destination, request, null, requestPostProcessor, cancellationToken);
        }

        public virtual async Task<T> ConvertSendAndReceiveAsync<T>(D destination, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default)
        {
            var requestMessage = DoConvert(request, headers, requestPostProcessor);
            var replyMessage = await SendAndReceiveAsync(destination, requestMessage);
            if (replyMessage != null)
            {
                return DoConvert<T>(replyMessage);
            }

            return default(T);
        }

        public virtual Task<IMessage> SendAndReceiveAsync(IMessage requestMessage, CancellationToken cancellationToken = default)
        {
            return SendAndReceiveAsync(RequiredDefaultSendDestination, requestMessage, cancellationToken);
        }

        public virtual Task<IMessage> SendAndReceiveAsync(D destination, IMessage requestMessage, CancellationToken cancellationToken = default)
        {
            return DoSendAndReceiveAsync(destination, requestMessage, cancellationToken);
        }

        public virtual T ConvertSendAndReceive<T>(object request)
        {
            return ConvertSendAndReceive<T>(RequiredDefaultSendDestination, request);
        }

        public virtual T ConvertSendAndReceive<T>(D destination, object request)
        {
            return ConvertSendAndReceive<T>(destination, request, (IDictionary<string, object>)null);
        }

        public virtual T ConvertSendAndReceive<T>(D destination, object request, IDictionary<string, object> headers)
        {
            return ConvertSendAndReceive<T>(destination, request, headers, null);
        }

        public virtual T ConvertSendAndReceive<T>(object request, IMessagePostProcessor requestPostProcessor)
        {
            return ConvertSendAndReceive<T>(RequiredDefaultSendDestination, request, requestPostProcessor);
        }

        public virtual T ConvertSendAndReceive<T>(D destination, object request, IMessagePostProcessor requestPostProcessor)
        {
            return ConvertSendAndReceive<T>(destination, request, null, requestPostProcessor);
        }

        public virtual T ConvertSendAndReceive<T>(D destination, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor)
        {
            var requestMessage = DoConvert(request, headers, requestPostProcessor);
            var replyMessage = SendAndReceive(destination, requestMessage);
            if (replyMessage != null)
            {
                return DoConvert<T>(replyMessage);
            }

            return default;
        }

        public virtual IMessage SendAndReceive(IMessage requestMessage)
        {
            return SendAndReceive(RequiredDefaultSendDestination, requestMessage);
        }

        public virtual IMessage SendAndReceive(D destination, IMessage requestMessage)
        {
            return DoSendAndReceive(destination, requestMessage);
        }

        protected abstract IMessage DoSendAndReceive(D destination, IMessage requestMessage);

        protected abstract Task<IMessage> DoSendAndReceiveAsync(D destination, IMessage requestMessage, CancellationToken cancellationToken = default);
    }
}
