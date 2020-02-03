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

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Integration
{
    public class ErrorMessagePublisher
    {
        protected readonly MessagingTemplate _messagingTemplate;
        protected readonly ILogger _logger;

        private readonly IServiceProvider _serviceProvider;
        private IDestinationResolver<IMessageChannel> _channelResolver;
        private IMessageChannel _channel;
        private IErrorMessageStrategy _errorMessageStrategy = new DefaultErrorMessageStrategy();

        public ErrorMessagePublisher(IServiceProvider serviceProvider, ILogger logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _messagingTemplate = new MessagingTemplate(serviceProvider);
        }

        public virtual IErrorMessageStrategy ErrorMessageStrategy
        {
            get
            {
                return _errorMessageStrategy;
            }

            set
            {
                _errorMessageStrategy = value ?? throw new ArgumentNullException("errorMessageStrategy must not be null");
            }
        }

        public virtual IMessageChannel Channel
        {
            get
            {
                PopulateChannel();

                return _channel;
            }

            set
            {
                _channel = value;
            }
        }

        public virtual string ChannelName { get; set; }

        public virtual int SendTimeout
        {
            get
            {
                return _messagingTemplate.SendTimeout;
            }

            set
            {
                _messagingTemplate.SendTimeout = value;
            }
        }

        public virtual IDestinationResolver<IMessageChannel> ChannelResolver
        {
            get
            {
                if (_channelResolver == null)
                {
                    _channelResolver = (IDestinationResolver<IMessageChannel>)_serviceProvider.GetService(typeof(IDestinationResolver<IMessageChannel>));
                }

                return _channelResolver;
            }

            set
            {
                _channelResolver = value ?? throw new ArgumentNullException("channelResolver must not be null");
            }
        }

        protected virtual MessagingTemplate MessagingTemplate
        {
            get { return _messagingTemplate; }
        }

        public virtual void Publish(MessagingException exception)
        {
            Publish(null, exception.FailedMessage, exception);
        }

        public virtual void Publish(IMessage failedMessage, Exception throwable)
        {
            Publish(null, failedMessage, throwable);
        }

        public virtual void Publish(IMessage inputMessage, MessagingException exception)
        {
            Publish(inputMessage, exception.FailedMessage, exception);
        }

        public virtual void Publish(IMessage inputMessage, IMessage failedMessage, Exception exception)
        {
            Publish(exception, ErrorMessageUtils.GetAttributeAccessor(inputMessage, failedMessage));
        }

        public virtual void Publish(Exception exception, IAttributeAccessor context)
        {
            PopulateChannel();

            var payload = DeterminePayload(exception, context);
            var errorMessage = _errorMessageStrategy.BuildErrorMessage(payload, context);

            _messagingTemplate.Send(errorMessage);
        }

        protected virtual Exception DeterminePayload(Exception exception, IAttributeAccessor context)
        {
            var lastThrowable = exception;
            if (lastThrowable == null)
            {
                lastThrowable = PayloadWhenNull(context);
            }
            else if (!(lastThrowable is MessagingException))
            {
                var message = (IMessage)context.GetAttribute(ErrorMessageUtils.FAILED_MESSAGE_CONTEXT_KEY);
                lastThrowable = message == null
                    ? new MessagingException(lastThrowable.Message, lastThrowable)
                    : new MessagingException(message, lastThrowable.Message, lastThrowable);
            }

            return lastThrowable;
        }

        protected virtual Exception PayloadWhenNull(IAttributeAccessor context)
        {
            var message = (IMessage)context.GetAttribute(ErrorMessageUtils.FAILED_MESSAGE_CONTEXT_KEY);
            return message == null
                    ? new MessagingException("No root cause exception available")
                    : new MessagingException(message, "No root cause exception available");
        }

        private void PopulateChannel()
        {
            if (_messagingTemplate.DefaultDestination == null)
            {
                if (_channel == null)
                {
                    var recoveryChannelName = ChannelName;
                    if (recoveryChannelName == null)
                    {
                        recoveryChannelName = IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME;
                    }

                    if (_channelResolver != null)
                    {
                        _channel = _channelResolver.ResolveDestination(recoveryChannelName);
                    }
                }

                _messagingTemplate.DefaultDestination = _channel;
            }
        }
    }
}
