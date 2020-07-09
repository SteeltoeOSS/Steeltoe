// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Order;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Support
{
    public abstract class AbstractMessageChannel : Channel<IMessage>, IMessageChannel, IInterceptableChannel
    {
        public const int INDEFINITE_TIMEOUT = -1;

        private object _lock = new object();
        private List<IChannelInterceptor> _interceptors = new List<IChannelInterceptor>();

        public AbstractMessageChannel(ILogger logger = null)
        {
            ServiceName = GetType().Name + "@" + GetHashCode();
            Logger = logger;
        }

        public virtual string ServiceName { get; set; }

        public ILogger Logger { get; set; }

        public virtual void SetInterceptors(List<IChannelInterceptor> interceptors)
        {
            lock (_lock)
            {
                interceptors.Sort(new OrderComparer());
                _interceptors = interceptors;
            }
        }

        public virtual void AddInterceptor(IChannelInterceptor interceptor)
        {
            lock (_lock)
            {
                var interceptors = new List<IChannelInterceptor>(_interceptors);
                interceptors.Add(interceptor);
                _interceptors = interceptors;
            }
        }

        public virtual void AddInterceptor(int index, IChannelInterceptor interceptor)
        {
            lock (_lock)
            {
                var interceptors = new List<IChannelInterceptor>(_interceptors);
                interceptors.Insert(index, interceptor);
                _interceptors = interceptors;
            }
        }

        public virtual List<IChannelInterceptor> GetInterceptors()
        {
            lock (_lock)
            {
                return new List<IChannelInterceptor>(_interceptors);
            }
        }

        public virtual bool RemoveInterceptor(IChannelInterceptor interceptor)
        {
            lock (_lock)
            {
                var interceptors = new List<IChannelInterceptor>(_interceptors);
                var result = interceptors.Remove(interceptor);
                _interceptors = interceptors;
                return result;
            }
        }

        public virtual IChannelInterceptor RemoveInterceptor(int index)
        {
            lock (_lock)
            {
                var interceptors = new List<IChannelInterceptor>(_interceptors);
                var existing = interceptors[index];
                interceptors.RemoveAt(index);
                _interceptors = interceptors;
                return existing;
            }
        }

        public virtual bool Send(IMessage message)
        {
            return Send(message, INDEFINITE_TIMEOUT);
        }

        public virtual bool Send(IMessage message, int timeout)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return DoSend(message, timeout);
        }

        public virtual ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            return new ValueTask<bool>(DoSend(message, cancellationToken));
        }

        public override string ToString()
        {
            return ServiceName;
        }

        protected virtual bool DoSend(IMessage message, int timeout)
        {
            if (timeout <= 0)
            {
                return DoSend(message, CancellationToken.None);
            }
            else
            {
                using (var source = new CancellationTokenSource())
                {
                    source.CancelAfter(timeout);
                    return DoSend(message, source.Token);
                }
            }
        }

        protected virtual bool DoSend(IMessage message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var messageToUse = message;
            var interceptors = _interceptors;
            ChannelInterceptorChain chain = null;
            if (interceptors.Count > 0)
            {
                chain = new ChannelInterceptorChain(this);
            }

            var sent = false;
            try
            {
                if (chain != null)
                {
                    messageToUse = chain.ApplyPreSend(messageToUse, this);
                    if (messageToUse == null)
                    {
                        return false;
                    }
                }

                sent = DoSendInternal(messageToUse, cancellationToken);
                chain?.ApplyPostSend(messageToUse, this, sent);
                chain?.TriggerAfterSendCompletion(messageToUse, this, sent, null);
                return sent;
            }
            catch (Exception ex)
            {
                chain?.TriggerAfterSendCompletion(messageToUse, this, sent, ex);
                if (ex is MessagingException)
                {
                    throw;
                }

                throw new MessageDeliveryException(messageToUse, "Failed to send message to " + ServiceName, ex);
            }
        }

        protected abstract bool DoSendInternal(IMessage message, CancellationToken cancellationToken);

        protected class ChannelInterceptorChain
        {
            private AbstractMessageChannel _channel;
            private List<IChannelInterceptor> _interceptors;
            private int _sendInterceptorIndex;

            private int _receiveInterceptorIndex;

            public ChannelInterceptorChain(AbstractMessageChannel channel)
            {
                this._channel = channel;
                _interceptors = channel._interceptors;
                _sendInterceptorIndex = -1;
                _receiveInterceptorIndex = -1;
            }

            public IMessage ApplyPreSend(IMessage message, IMessageChannel channel)
            {
                if (_interceptors.Count == 0)
                {
                    return message;
                }

                var messageToUse = message;
                foreach (var interceptor in _interceptors)
                {
                    var resolvedMessage = interceptor.PreSend(messageToUse, channel);
                    if (resolvedMessage == null)
                    {
                        var name = interceptor.GetType().Name;
                        this._channel.Logger?.LogDebug("{name} returned null from PreSend, i.e. precluding the send.", name);
                        TriggerAfterSendCompletion(messageToUse, channel, false, null);
                        return null;
                    }

                    messageToUse = resolvedMessage;
                    _sendInterceptorIndex++;
                }

                return messageToUse;
            }

            public void ApplyPostSend(IMessage message, IMessageChannel channel, bool sent)
            {
                if (_interceptors.Count == 0)
                {
                    return;
                }

                foreach (var interceptor in _interceptors)
                {
                    interceptor.PostSend(message, channel, sent);
                }
            }

            public void TriggerAfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception ex)
            {
                if (_sendInterceptorIndex == -1)
                {
                    return;
                }

                for (var i = _sendInterceptorIndex; i >= 0; i--)
                {
                    var interceptor = _interceptors[i];
                    try
                    {
                        interceptor.AfterSendCompletion(message, channel, sent, ex);
                    }
                    catch (Exception ex2)
                    {
                        this._channel.Logger?.LogError(ex2, "Exception from afterSendCompletion in {interceptor} ", interceptor);
                    }
                }
            }

            public bool ApplyPreReceive(IMessageChannel channel)
            {
                if (_interceptors.Count == 0)
                {
                    return true;
                }

                foreach (var interceptor in _interceptors)
                {
                    if (!interceptor.PreReceive(channel))
                    {
                        TriggerAfterReceiveCompletion(null, channel, null);
                        return false;
                    }

                    _receiveInterceptorIndex++;
                }

                return true;
            }

            public IMessage ApplyPostReceive(IMessage message, IMessageChannel channel)
            {
                if (_interceptors.Count == 0)
                {
                    return message;
                }

                var messageToUse = message;
                foreach (var interceptor in _interceptors)
                {
                    messageToUse = interceptor.PostReceive(messageToUse, channel);
                    if (messageToUse == null)
                    {
                        return null;
                    }
                }

                return messageToUse;
            }

            public void TriggerAfterReceiveCompletion(IMessage message, IMessageChannel channel, Exception ex)
            {
                if (_receiveInterceptorIndex == -1)
                {
                    return;
                }

                for (var i = _receiveInterceptorIndex; i >= 0; i--)
                {
                    var interceptor = _interceptors[i];
                    try
                    {
                        interceptor.AfterReceiveCompletion(message, channel, ex);
                    }
                    catch (Exception ex2)
                    {
                        this._channel.Logger?.LogError(ex2, "Exception from afterReceiveCompletion in: {interceptor} ", interceptor);
                    }
                }
            }
        }
    }
}
