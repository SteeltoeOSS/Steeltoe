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
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Integration;
using Steeltoe.Integration.Acks;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Stream.Binder
{
    public class DefaultPollableMessageSource : AbstractPollableSource<IMessageHandler>, IPollableMessageSource, ILifecycle, IRetryListener
    {
        private static readonly AsyncLocal<IAttributeAccessor> _attributesHolder = new AsyncLocal<IAttributeAccessor>();

        private readonly DirectChannel _dummyChannel;
        private readonly MessagingTemplate _messagingTemplate;
        private readonly ISmartMessageConverter _messageConverter;
        private readonly List<IChannelInterceptor> _interceptors = new List<IChannelInterceptor>();
        private RetryTemplate _retryTemplate;
        private IRecoveryCallback _recoveryCallback;
        private int _running;

        public DefaultPollableMessageSource(IApplicationContext context, ISmartMessageConverter messageConverter)
        {
            _messageConverter = messageConverter;
            _messagingTemplate = new MessagingTemplate(context);
            _dummyChannel = new DirectChannel(context);
        }

        public RetryTemplate RetryTemplate
        {
            get
            {
                return _retryTemplate;
            }

            set
            {
                _retryTemplate = value;
                _retryTemplate.RegisterListener(this);
            }
        }

        public IRecoveryCallback RecoveryCallback
        {
            get
            {
                return _recoveryCallback;
            }

            set
            {
                _recoveryCallback = new RecoveryCallbackWrapper(value);
            }
        }

        public IMessageChannel ErrorChannel { get; set; }

        public IErrorMessageStrategy ErrorMessageStrategy { get; set; } = new DefaultErrorMessageStrategy();

        public Action<IAttributeAccessor, IMessage> AttributeProvider { get; set; }

        public IMessageSource Source { get; set; }

        public bool IsRunning => _running != 0;

        public void AddInterceptor(IChannelInterceptor interceptor)
        {
            _interceptors.Add(interceptor);
        }

        public void AddInterceptor(int index, IChannelInterceptor interceptor)
        {
            _interceptors.Insert(index, interceptor);
        }

        public async Task Start()
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) == 0 && Source is ILifecycle)
            {
                await ((ILifecycle)Source).Start();
            }
        }

        public async Task Stop()
        {
            if (Interlocked.CompareExchange(ref _running, 0, 1) == 1 && Source is ILifecycle)
            {
                await ((ILifecycle)Source).Stop();
            }
        }

        public override bool Poll(IMessageHandler handler)
        {
            return Poll(handler, null);
        }

        public override bool Poll(IMessageHandler handler, Type type)
        {
            var message = Receive(type);
            if (message == null)
            {
                return false;
            }

            var ackCallback = StaticMessageHeaderAccessor.GetAcknowledgmentCallback(message);
            try
            {
                if (RetryTemplate == null)
                {
                    Handle(message, handler);
                }
                else
                {
                    RetryTemplate.Execute((ctx) => Handle(message, handler), _recoveryCallback);
                }

                return true;
            }
            catch (MessagingException e)
            {
                if (RetryTemplate == null && !ShouldRequeue(e))
                {
                    _messagingTemplate.Send(ErrorChannel, ErrorMessageStrategy.BuildErrorMessage(e, _attributesHolder.Value));
                    return true;
                }
                else if (!ackCallback.IsAcknowledged && ShouldRequeue(e))
                {
                    AckUtils.Requeue(ackCallback);
                    return true;
                }
                else
                {
                    AckUtils.AutoNack(ackCallback);
                }

                if (e.FailedMessage.Equals(message))
                {
                    throw;
                }

                throw new MessageHandlingException(message, e);
            }
            catch (Exception e)
            {
                AckUtils.AutoNack(ackCallback);
                if (e is MessageHandlingException && ((MessageHandlingException)e).FailedMessage.Equals(message))
                {
                    throw;
                }

                throw new MessageHandlingException(message, e);
            }
            finally
            {
                AckUtils.AutoAck(ackCallback);
            }
        }

        public bool Open(IRetryContext context)
        {
            if (_recoveryCallback != null)
            {
                _attributesHolder.Value = context;
            }

            return true;
        }

        public void Close(IRetryContext context, Exception exception)
        {
            _attributesHolder.Value = null;
        }

        public void OnError(IRetryContext context, Exception exception)
        {
            // Ignore
        }

        protected internal static bool ShouldRequeue(Exception e)
        {
            var requeue = false;
            var t = e.InnerException;
            while (t != null && !requeue)
            {
                requeue = t is RequeueCurrentMessageException;
                t = t.InnerException;
            }

            return requeue;
        }

        private IMessage Receive(Type type)
        {
            var result = Source.Receive();
            var message = ApplyInterceptors(result);

            if (message != null && type != null && _messageConverter != null)
            {
                var targetType = type;
                var payload = _messageConverter.FromMessage(message, targetType, type);
                if (payload == null)
                {
                    throw new MessageConversionException(message, "No converter could convert Message");
                }

                // TODO: Rationalize S.M.S.MessageBuilder and S.I.S.MessageBuilder
                message = Steeltoe.Messaging.Support.MessageBuilder.WithPayload(payload).CopyHeaders(message.Headers).Build();
            }

            return message;
        }

        private void DoHandleMessage(IMessageHandler handler, IMessage message)
        {
            try
            {
                handler.HandleMessage(message);
            }
            catch (Exception t)
            {
                throw new MessageHandlingException(message, t);
            }
        }

        private IMessage ApplyInterceptors(IMessage message)
        {
            var received = message;
            foreach (var interceptor in _interceptors)
            {
                received = interceptor.PreSend(received, _dummyChannel);
                if (received == null)
                {
                    return null;
                }
            }

            return received;
        }

        private void SetAttributesIfNecessary(IMessage message)
        {
            var needHolder = ErrorChannel != null && RetryTemplate == null;
            var needAttributes = needHolder || RetryTemplate != null;
            if (needHolder)
            {
                _attributesHolder.Value = ErrorMessageUtils.GetAttributeAccessor(null, null);
            }

            if (needAttributes)
            {
                var attributes = _attributesHolder.Value;
                if (attributes != null)
                {
                    attributes.SetAttribute(ErrorMessageUtils.INPUT_MESSAGE_CONTEXT_KEY, message);
                    if (AttributeProvider != null)
                    {
                        AttributeProvider.Invoke(attributes, message);
                    }
                }
            }
        }

        private void Handle(IMessage message, IMessageHandler handler)
        {
            SetAttributesIfNecessary(message);
            DoHandleMessage(handler, message);
        }

        private class RecoveryCallbackWrapper : IRecoveryCallback
        {
            private readonly IRecoveryCallback _recoveryCallback;

            public RecoveryCallbackWrapper(IRecoveryCallback recoveryCallback)
            {
                _recoveryCallback = recoveryCallback;
            }

            public object Recover(IRetryContext context)
            {
                if (!ShouldRequeue((MessagingException)context.LastException))
                {
                    return _recoveryCallback.Recover(context);
                }

                throw (MessagingException)context.LastException;
            }
        }
    }
}
