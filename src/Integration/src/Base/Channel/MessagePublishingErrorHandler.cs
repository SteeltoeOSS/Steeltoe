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
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Integration.Channel
{
    public class MessagePublishingErrorHandler : ErrorMessagePublisher, IErrorHandler
    {
        private const int DEFAULT_SEND_TIMEOUT = 1000;

        private static IErrorMessageStrategy DEFAULT_ERROR_MESSAGE_STRATEGY { get; } = new DefaultErrorMessageStrategy();

        public MessagePublishingErrorHandler(IServiceProvider serviceProvider, ILogger logger = null)
            : base(serviceProvider, logger)
        {
            ErrorMessageStrategy = DEFAULT_ERROR_MESSAGE_STRATEGY;
            SendTimeout = DEFAULT_SEND_TIMEOUT;
        }

        public IMessageChannel DefaultErrorChannel
        {
            get { return Channel; }
            set { Channel = value; }
        }

        public string DefaultErrorChannelName
        {
            get { return ChannelName; }
            set { ChannelName = value; }
        }

        public bool HandleError(Exception exception)
        {
            var errorChannel = ResolveErrorChannel(exception);
            var sent = false;
            if (errorChannel != null)
            {
                try
                {
                    MessagingTemplate.Send(errorChannel, ErrorMessageStrategy.BuildErrorMessage(exception, null));
                    sent = true;
                }
                catch (Exception errorDeliveryError)
                {
                    _logger?.LogWarning("Error message was not delivered.", errorDeliveryError);
                }
            }

            if (!sent)
            {
                var failedMessage = (exception is MessagingException) ? ((MessagingException)exception).FailedMessage : null;
                if (failedMessage != null)
                {
                    _logger?.LogError("failure occurred in messaging task with message: " + failedMessage, exception);
                }
                else
                {
                    _logger?.LogError("failure occurred in messaging task", exception);
                }
            }

            return sent;
        }

        private IMessageChannel ResolveErrorChannel(Exception exception)
        {
            var actualThrowable = exception;
            if (exception is MessagingExceptionWrapperException)
            {
                actualThrowable = exception.InnerException;
            }

            var failedMessage = (actualThrowable is MessagingException) ? ((MessagingException)actualThrowable).FailedMessage : null;
            if (DefaultErrorChannel == null && ChannelResolver != null)
            {
                Channel = ChannelResolver.ResolveDestination(IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME);
            }

            if (failedMessage == null || failedMessage.Headers.ErrorChannel == null)
            {
                return DefaultErrorChannel;
            }

            var errorChannelHeader = failedMessage.Headers.ErrorChannel;
            if (errorChannelHeader is IMessageChannel)
            {
                return (IMessageChannel)errorChannelHeader;
            }

            if (!(errorChannelHeader is string))
            {
                throw new ArgumentException("Unsupported error channel header type. Expected IMessageChannel or String, but actual type is [" + errorChannelHeader.GetType() + "]");
            }

            if (ChannelResolver != null)
            {
                return ChannelResolver.ResolveDestination((string)errorChannelHeader); // NOSONAR not null
            }
            else
            {
                return null;
            }
        }

        private class DefaultErrorMessageStrategy : IErrorMessageStrategy
        {
            public ErrorMessage BuildErrorMessage(Exception payload, IAttributeAccessor attributes)
            {
                return payload is MessagingExceptionWrapperException
                    ? new ErrorMessage(payload.InnerException, ((MessagingExceptionWrapperException)payload).FailedMessage)
                    : new ErrorMessage(payload);
            }
        }
    }
}
