// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Order;

namespace Steeltoe.Messaging.Support;

public abstract class AbstractMessageChannel : Channel<IMessage>, IMessageChannel, IInterceptableChannel
{
    public const int IndefiniteTimeout = -1;

    private readonly object _lock = new();
    private List<IChannelInterceptor> _interceptors = new();

    public virtual string ServiceName { get; set; }

    public ILogger Logger { get; set; }

    protected AbstractMessageChannel(ILogger logger = null)
    {
        ServiceName = $"{GetType().Name}@{GetHashCode()}";
        Logger = logger;
    }

#pragma warning disable S3956 // "Generic.List" instances should not be part of public APIs
    public virtual void SetInterceptors(List<IChannelInterceptor> interceptors)
#pragma warning restore S3956 // "Generic.List" instances should not be part of public APIs
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
            var interceptors = new List<IChannelInterceptor>(_interceptors)
            {
                interceptor
            };

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

    public virtual IList<IChannelInterceptor> GetInterceptors()
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
            bool result = interceptors.Remove(interceptor);
            _interceptors = interceptors;
            return result;
        }
    }

    public virtual IChannelInterceptor RemoveInterceptor(int index)
    {
        lock (_lock)
        {
            var interceptors = new List<IChannelInterceptor>(_interceptors);
            IChannelInterceptor existing = interceptors[index];
            interceptors.RemoveAt(index);
            _interceptors = interceptors;
            return existing;
        }
    }

    public virtual bool Send(IMessage message)
    {
        return Send(message, IndefiniteTimeout);
    }

    public virtual bool Send(IMessage message, int timeout)
    {
        ArgumentGuard.NotNull(message);

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

        using var source = new CancellationTokenSource();
        source.CancelAfter(timeout);
        return DoSend(message, source.Token);
    }

    protected virtual bool DoSend(IMessage message, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(message);

        IMessage messageToUse = message;
        List<IChannelInterceptor> interceptors = _interceptors;
        ChannelInterceptorChain chain = null;

        if (interceptors.Count > 0)
        {
            chain = new ChannelInterceptorChain(this);
        }

        bool sent = false;

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

            throw new MessageDeliveryException(messageToUse, $"Failed to send message to {ServiceName}", ex);
        }
    }

    protected abstract bool DoSendInternal(IMessage message, CancellationToken cancellationToken);

    protected class ChannelInterceptorChain
    {
        private readonly AbstractMessageChannel _channel;
        private readonly List<IChannelInterceptor> _interceptors;
        private int _sendInterceptorIndex;

        private int _receiveInterceptorIndex;

        public ChannelInterceptorChain(AbstractMessageChannel channel)
        {
            _channel = channel;
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

            IMessage messageToUse = message;

            foreach (IChannelInterceptor interceptor in _interceptors)
            {
                IMessage resolvedMessage = interceptor.PreSend(messageToUse, channel);

                if (resolvedMessage == null)
                {
                    string name = interceptor.GetType().Name;
                    _channel.Logger?.LogDebug("{name} returned null from PreSend, i.e. precluding the send.", name);
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

            foreach (IChannelInterceptor interceptor in _interceptors)
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

            for (int i = _sendInterceptorIndex; i >= 0; i--)
            {
                IChannelInterceptor interceptor = _interceptors[i];

                try
                {
                    interceptor.AfterSendCompletion(message, channel, sent, ex);
                }
                catch (Exception ex2)
                {
                    _channel.Logger?.LogError(ex2, "Exception from afterSendCompletion in {interceptor} ", interceptor);
                }
            }
        }

        public bool ApplyPreReceive(IMessageChannel channel)
        {
            if (_interceptors.Count == 0)
            {
                return true;
            }

            foreach (IChannelInterceptor interceptor in _interceptors)
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

            IMessage messageToUse = message;

            foreach (IChannelInterceptor interceptor in _interceptors)
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

            for (int i = _receiveInterceptorIndex; i >= 0; i--)
            {
                IChannelInterceptor interceptor = _interceptors[i];

                try
                {
                    interceptor.AfterReceiveCompletion(message, channel, ex);
                }
                catch (Exception ex2)
                {
                    _channel.Logger?.LogError(ex2, "Exception from afterReceiveCompletion in: {interceptor} ", interceptor);
                }
            }
        }
    }
}
