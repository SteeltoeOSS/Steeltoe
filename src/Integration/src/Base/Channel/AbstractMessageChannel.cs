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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Order;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Channel
{
    public abstract class AbstractMessageChannel : Channel<IMessage>, IMessageChannel, IChannelInterceptorAware
    {
        protected const int INDEFINITE_TIMEOUT = -1;
        private readonly Lazy<IIntegrationServices> _integrationServices;
        private readonly IApplicationContext _context;
        private Lazy<IMessageConverter> _messageConverter;

        protected AbstractMessageChannel(IApplicationContext context, ILogger logger = null)
            : this(context, null, logger)
        {
        }

        protected AbstractMessageChannel(IApplicationContext context, string name, ILogger logger = null)
        {
            _context = context;
            _integrationServices = new Lazy<IIntegrationServices>(() =>
                _context.GetService<IIntegrationServices>());
            _messageConverter = new Lazy<IMessageConverter>(() => (IMessageConverter)_context.GetService(typeof(DefaultDatatypeChannelMessageConverter)));
            Logger = logger;
            ServiceName = name ?? GetType().Name + "@" + GetHashCode();
        }

        public IIntegrationServices IntegrationServices => _integrationServices.Value;

        public virtual string ComponentType { get; } = "channel";

        public virtual string ServiceName { get; set; }

        public virtual string ComponentName { get; set; }

        public virtual List<Type> DataTypes { get; set; } = new List<Type>();

        public virtual IMessageConverter MessageConverter
        {
            get { return _messageConverter.Value; }
            set { _messageConverter = new Lazy<IMessageConverter>(value); }
        }

        public virtual List<IChannelInterceptor> ChannelInterceptors
        {
            get
            {
                return Interceptors.Interceptors;
            }

            set
            {
                value.Sort(new OrderComparer());
                Interceptors.Set(value);
            }
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
                throw new ArgumentNullException("Message payload is null!");
            }

            return DoSend(message, timeout);
        }

        public ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            return new ValueTask<bool>(DoSend(message, cancellationToken));
        }

        internal ChannelInterceptorList Interceptors { get; set; } = new ChannelInterceptorList();

        protected ILogger Logger { get; }

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

        protected bool DoSend(IMessage message, CancellationToken cancellationToken)
        {
            Stack<IChannelInterceptor> interceptorStack = null;
            var sent = false;

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

                var wrapped = IntegrationUtils.WrapInDeliveryExceptionIfNecessary(message, "failed to send Message to channel '" + ServiceName + "'", e);
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
            // first pass checks if the payload type already matches any of the datatypes
            foreach (var datatype in DataTypes)
            {
                if (datatype.IsAssignableFrom(message.Payload.GetType()))
                {
                    return message;
                }
            }

            if (MessageConverter != null)
            {
                // second pass applies conversion if possible, attempting datatypes in order
                foreach (var datatype in DataTypes)
                {
                    var converted = MessageConverter.FromMessage(message, datatype);
                    if (converted != null)
                    {
                        return converted is IMessage
                            ? (IMessage)converted
                            : IntegrationServices
                                .MessageBuilderFactory
                                .WithPayload(converted)
                                .CopyHeaders(message.Headers)
                                .Build();
                    }
                }
            }

            throw new MessageDeliveryException(
                message,
                "Channel '" + ServiceName + "' expected one of the following datataypes [" + string.Join(",", DataTypes) + "], but received [" + message.Payload.GetType() + "]");
        }

        internal class ChannelInterceptorList
        {
            private readonly object _lock = new object();
            private IChannelInterceptor[] _interceptors = new IChannelInterceptor[0];

            public ChannelInterceptorList()
            {
            }

            public bool Set(IList<IChannelInterceptor> interceptors)
            {
                lock (_lock)
                {
                    _interceptors = interceptors.ToArray();
                    return true;
                }
            }

            public int Count { get; private set; } = 0;

            public bool Add(IChannelInterceptor interceptor)
            {
                lock (_lock)
                {
                    var interceptors = new List<IChannelInterceptor>(_interceptors);
                    interceptors.Add(interceptor);
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
                    var result = interceptors.Remove(interceptor);
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
                    var current = interceptors[index];
                    interceptors.RemoveAt(index);
                    _interceptors = interceptors.ToArray();
                    Count = _interceptors.Length;
                    return current;
                }
            }

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

            public IMessage PreSend(IMessage messageArg, IMessageChannel channel, Stack<IChannelInterceptor> interceptorStack)
            {
                var message = messageArg;

                var interceptors = _interceptors;

                for (var i = 0; i < interceptors.Length; i++)
                {
                    var interceptor = interceptors[i];
                    var previous = message;
                    message = interceptor.PreSend(message, channel);
                    if (message == null)
                    {
                        AfterSendCompletion(previous, channel, false, (Exception)null, interceptorStack);
                        return null;
                    }

                    interceptorStack.Push(interceptor);
                }

                return message;
            }

            public void PostSend(IMessage message, IMessageChannel channel, bool sent)
            {
                var interceptors = _interceptors;

                for (var i = 0; i < interceptors.Length; i++)
                {
                    var interceptor = interceptors[i];
                    interceptor.PostSend(message, channel, sent);
                }
            }

            public void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception ex, Stack<IChannelInterceptor> interceptorStack)
            {
                if (interceptorStack == null)
                {
                    return;
                }

                foreach (var interceptor in interceptorStack)
                {
                    try
                    {
                        interceptor.AfterSendCompletion(message, channel, sent, ex);
                    }
                    catch (Exception)
                    {
                        // Log
                    }
                }
            }

            public bool PreReceive(IMessageChannel channel, Stack<IChannelInterceptor> interceptorStack)
            {
                var interceptors = _interceptors;

                for (var i = 0; i < interceptors.Length; i++)
                {
                    var interceptor = interceptors[i];

                    if (!interceptor.PreReceive(channel))
                    {
                        AfterReceiveCompletion((IMessage)null, channel, (Exception)null, interceptorStack);
                        return false;
                    }

                    interceptorStack.Push(interceptor);
                }

                return true;
            }

            public IMessage PostReceive(IMessage messageArg, IMessageChannel channel)
            {
                var message = messageArg;
                var interceptors = _interceptors;

                for (var i = 0; i < interceptors.Length; i++)
                {
                    var interceptor = interceptors[i];
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

                foreach (var interceptor in interceptorStack)
                {
                    try
                    {
                        interceptor.AfterReceiveCompletion(message, channel, ex);
                    }
                    catch (Exception)
                    {
                        // Log
                    }
                }
            }
        }
    }
}
