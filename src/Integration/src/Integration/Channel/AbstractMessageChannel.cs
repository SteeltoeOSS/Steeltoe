// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Order;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Channel;

public abstract class AbstractMessageChannel : Channel<IMessage>, IMessageChannel, IChannelInterceptorAware
{
    protected const int IndefiniteTimeout = -1;

    protected readonly ILogger Logger;
    private IIntegrationServices _integrationServices;
    private IMessageConverter _messageConverter;

    internal ChannelInterceptorList Interceptors { get; set; }

    public IApplicationContext ApplicationContext { get; }

    public IIntegrationServices IntegrationServices
    {
        get
        {
            _integrationServices ??= IntegrationServicesUtils.GetIntegrationServices(ApplicationContext);
            return _integrationServices;
        }
    }

    public virtual string ComponentType { get; } = "channel";

    public virtual string ServiceName { get; set; }

    public virtual string ComponentName { get; set; }

    public virtual List<Type> DataTypes { get; set; } = new();

    public virtual IMessageConverter MessageConverter
    {
        get
        {
            _messageConverter ??= ApplicationContext.GetService<DefaultDataTypeChannelMessageConverter>();
            return _messageConverter;
        }

        set => _messageConverter = value;
    }

    public virtual List<IChannelInterceptor> ChannelInterceptors
    {
        get => Interceptors.Interceptors;

        set
        {
            value.Sort(new OrderComparer());
            Interceptors.Set(value);
        }
    }

    protected AbstractMessageChannel(IApplicationContext context, ILogger logger = null)
        : this(context, null, logger)
    {
    }

    protected AbstractMessageChannel(IApplicationContext context, string name, ILogger logger = null)
    {
        ApplicationContext = context;
        Logger = logger;
        ServiceName = name ?? $"{GetType().Name}@{GetHashCode()}";
        Interceptors = new ChannelInterceptorList(logger);
    }

    public virtual void AddInterceptor(IChannelInterceptor interceptor)
    {
        Interceptors.Add(interceptor);
    }

    public virtual void AddInterceptor(int index, IChannelInterceptor interceptor)
    {
        Interceptors.Add(index, interceptor);
    }

    public virtual bool RemoveInterceptor(IChannelInterceptor interceptor)
    {
        return Interceptors.Remove(interceptor);
    }

    public virtual IChannelInterceptor RemoveInterceptor(int index)
    {
        return Interceptors.Remove(index);
    }

    public virtual bool Send(IMessage message)
    {
        return Send(message, -1);
    }

    public virtual bool Send(IMessage message, int timeout)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (message.Payload == null)
        {
            throw new ArgumentNullException(nameof(message), "Message payload is null!");
        }

        return DoSend(message, timeout);
    }

    public ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        return new ValueTask<bool>(DoSend(message, cancellationToken));
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

    protected bool DoSend(IMessage message, CancellationToken cancellationToken)
    {
        Stack<IChannelInterceptor> interceptorStack = null;
        bool sent = false;

        try
        {
            if (DataTypes.Count > 0)
            {
                message = ConvertPayloadIfNecessary(message);
            }

            Logger?.LogDebug("PreSend on channel '" + ServiceName + "', message: " + message);

            if (Interceptors.Count > 0)
            {
                interceptorStack = new Stack<IChannelInterceptor>();
                message = Interceptors.PreSend(message, this, interceptorStack);

                if (message == null)
                {
                    return false;
                }
            }

            sent = DoSendInternal(message, cancellationToken);

            Logger?.LogDebug("PostSend (sent=" + sent + ") on channel '" + ServiceName + "', message: " + message);

            if (interceptorStack != null)
            {
                Interceptors.PostSend(message, this, sent);
                Interceptors.AfterSendCompletion(message, this, sent, null, interceptorStack);
            }

            return sent;
        }
        catch (Exception e)
        {
            if (interceptorStack != null)
            {
                Interceptors.AfterSendCompletion(message, this, sent, e, interceptorStack);
            }

            Exception wrapped = IntegrationUtils.WrapInDeliveryExceptionIfNecessary(message, $"failed to send Message to channel '{ServiceName}'", e);

            if (wrapped != e)
            {
                throw wrapped;
            }

            throw;
        }
    }

    protected abstract bool DoSendInternal(IMessage message, CancellationToken cancellationToken);

    private IMessage ConvertPayloadIfNecessary(IMessage message)
    {
        // first pass checks if the payload type already matches any of the data types
        foreach (Type dataType in DataTypes)
        {
            if (dataType.IsInstanceOfType(message.Payload))
            {
                return message;
            }
        }

        if (MessageConverter != null)
        {
            // second pass applies conversion if possible, attempting data types in order
            foreach (Type dataType in DataTypes)
            {
                object converted = MessageConverter.FromMessage(message, dataType);

                if (converted != null)
                {
                    return converted as IMessage ?? IntegrationServices.MessageBuilderFactory.WithPayload(converted).CopyHeaders(message.Headers).Build();
                }
            }
        }

        throw new MessageDeliveryException(message,
            $"Channel '{ServiceName}' expected one of the following data types [{string.Join(",", DataTypes)}], but received [{message.Payload.GetType()}]");
    }

    internal sealed class ChannelInterceptorList
    {
        private readonly object _lock = new();
        private readonly ILogger _logger;
        private IChannelInterceptor[] _interceptors = Array.Empty<IChannelInterceptor>();

        public int Count { get; private set; }

        public List<IChannelInterceptor> Interceptors
        {
            get
            {
                lock (_lock)
                {
                    return new List<IChannelInterceptor>(_interceptors);
                }
            }
        }

        public ChannelInterceptorList(ILogger logger)
        {
            _logger = logger;
        }

        public bool Set(IList<IChannelInterceptor> interceptors)
        {
            lock (_lock)
            {
                _interceptors = interceptors.ToArray();
                return true;
            }
        }

        public bool Add(IChannelInterceptor interceptor)
        {
            lock (_lock)
            {
                var interceptors = new List<IChannelInterceptor>(_interceptors)
                {
                    interceptor
                };

                _interceptors = interceptors.ToArray();
                Count = _interceptors.Length;
                return true;
            }
        }

        public void Add(int index, IChannelInterceptor interceptor)
        {
            lock (_lock)
            {
                var interceptors = new List<IChannelInterceptor>(_interceptors);
                interceptors.Insert(index, interceptor);
                _interceptors = interceptors.ToArray();
                Count = _interceptors.Length;
            }
        }

        public bool Remove(IChannelInterceptor interceptor)
        {
            lock (_lock)
            {
                var interceptors = new List<IChannelInterceptor>(_interceptors);
                bool result = interceptors.Remove(interceptor);
                _interceptors = interceptors.ToArray();
                Count = _interceptors.Length;
                return result;
            }
        }

        public IChannelInterceptor Remove(int index)
        {
            lock (_lock)
            {
                if (index < 0 || index >= _interceptors.Length)
                {
                    return null;
                }

                var interceptors = new List<IChannelInterceptor>(_interceptors);
                IChannelInterceptor current = interceptors[index];
                interceptors.RemoveAt(index);
                _interceptors = interceptors.ToArray();
                Count = _interceptors.Length;
                return current;
            }
        }

        public IMessage PreSend(IMessage messageArg, IMessageChannel channel, Stack<IChannelInterceptor> interceptorStack)
        {
            IMessage message = messageArg;

            IChannelInterceptor[] interceptors = _interceptors;

            foreach (IChannelInterceptor interceptor in interceptors)
            {
                IMessage previous = message;
                message = interceptor.PreSend(message, channel);

                if (message == null)
                {
                    AfterSendCompletion(previous, channel, false, null, interceptorStack);
                    return null;
                }

                interceptorStack.Push(interceptor);
            }

            return message;
        }

        public void PostSend(IMessage message, IMessageChannel channel, bool sent)
        {
            IChannelInterceptor[] interceptors = _interceptors;

            foreach (IChannelInterceptor interceptor in interceptors)
            {
                interceptor.PostSend(message, channel, sent);
            }
        }

        public void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception ex, Stack<IChannelInterceptor> interceptorStack)
        {
            if (interceptorStack == null)
            {
                return;
            }

            foreach (IChannelInterceptor interceptor in interceptorStack)
            {
                try
                {
                    interceptor.AfterSendCompletion(message, channel, sent, ex);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, e.Message);
                }
            }
        }

        public bool PreReceive(IMessageChannel channel, Stack<IChannelInterceptor> interceptorStack)
        {
            IChannelInterceptor[] interceptors = _interceptors;

            foreach (IChannelInterceptor interceptor in interceptors)
            {
                if (!interceptor.PreReceive(channel))
                {
                    AfterReceiveCompletion(null, channel, null, interceptorStack);
                    return false;
                }

                interceptorStack.Push(interceptor);
            }

            return true;
        }

        public IMessage PostReceive(IMessage messageArg, IMessageChannel channel)
        {
            IMessage message = messageArg;
            IChannelInterceptor[] interceptors = _interceptors;

            foreach (IChannelInterceptor interceptor in interceptors)
            {
                message = interceptor.PostReceive(message, channel);

                if (message == null)
                {
                    return null;
                }
            }

            return message;
        }

        public void AfterReceiveCompletion(IMessage message, IMessageChannel channel, Exception ex, Stack<IChannelInterceptor> interceptorStack)
        {
            if (interceptorStack == null)
            {
                return;
            }

            foreach (IChannelInterceptor interceptor in interceptorStack)
            {
                try
                {
                    interceptor.AfterReceiveCompletion(message, channel, ex);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, e.Message);
                }
            }
        }
    }
}
