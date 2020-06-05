// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
