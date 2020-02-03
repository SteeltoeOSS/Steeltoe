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

using Steeltoe.Common.Util;
using Steeltoe.Integration.Retry;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration
{
    public class ErrorMessageSendingRecoverer : ErrorMessagePublisher, IRecoveryCallback
    {
        public ErrorMessageSendingRecoverer(IServiceProvider serviceProvider)
        : this(serviceProvider, null)
        {
        }

        public ErrorMessageSendingRecoverer(IServiceProvider serviceProvider, IMessageChannel channel)
        : this(serviceProvider, channel, null)
        {
        }

        public ErrorMessageSendingRecoverer(IServiceProvider serviceProvider, IMessageChannel channel, IErrorMessageStrategy errorMessageStrategy)
            : base(serviceProvider)
        {
            Channel = channel;
            ErrorMessageStrategy = errorMessageStrategy ?? new DefaultErrorMessageStrategy();
        }

        public object Recover(IRetryContext context)
        {
            Publish(context.LastException, context);
            return null;
        }

        protected override Exception PayloadWhenNull(IAttributeAccessor context)
        {
            var message = (IMessage)context.GetAttribute(ErrorMessageUtils.FAILED_MESSAGE_CONTEXT_KEY);
            var description = "No retry exception available; " +
                    "this can occur, for example, if the RetryPolicy allowed zero attempts " +
                    "to execute the handler; " +
                    "RetryContext: " + context.ToString();
            return message == null
                    ? new RetryExceptionNotAvailableException(description)
                    : new RetryExceptionNotAvailableException(message, description);
        }
    }
}
