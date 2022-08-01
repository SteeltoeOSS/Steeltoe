// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;

namespace Steeltoe.Integration;

public class ErrorMessageSendingRecoverer : ErrorMessagePublisher, IRecoveryCallback
{
    public ErrorMessageSendingRecoverer(IApplicationContext context)
        : this(context, null)
    {
    }

    public ErrorMessageSendingRecoverer(IApplicationContext context, IMessageChannel channel)
        : this(context, channel, null)
    {
    }

    public ErrorMessageSendingRecoverer(IApplicationContext context, IMessageChannel channel, IErrorMessageStrategy errorMessageStrategy)
        : base(context)
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
        var message = (IMessage)context.GetAttribute(ErrorMessageUtils.FailedMessageContextKey);
        var description =
            $"No retry exception available; this can occur, for example, if the RetryPolicy allowed zero attempts to execute the handler; RetryContext: {context}";
        return message == null
            ? new RetryExceptionNotAvailableException(description)
            : new RetryExceptionNotAvailableException(message, description);
    }
}
