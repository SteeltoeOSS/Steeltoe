// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Integration.Channel;

public class MessagePublishingErrorHandler : ErrorMessagePublisher, IErrorHandler
{
    public const string DefaultServiceName = nameof(MessagePublishingErrorHandler);
    private const int DefaultSendTimeout = 1000;

    private static IErrorMessageStrategy DefaultErrorMessageStrategyInstance { get; } = new DefaultErrorMessageStrategy();

    public MessagePublishingErrorHandler(IApplicationContext context, ILogger logger = null)
        : base(context, logger)
    {
        ErrorMessageStrategy = DefaultErrorMessageStrategyInstance;
        SendTimeout = DefaultSendTimeout;
    }

    public string ServiceName { get; set; } = DefaultServiceName;

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
                Logger?.LogWarning("Error message was not delivered.", errorDeliveryError);
            }
        }

        if (!sent)
        {
            var failedMessage = exception is MessagingException ex ? ex.FailedMessage : null;
            if (failedMessage != null)
            {
                Logger?.LogError("failure occurred in messaging task with message: " + failedMessage, exception);
            }
            else
            {
                Logger?.LogError("failure occurred in messaging task", exception);
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

        var failedMessage = actualThrowable is MessagingException ex ? ex.FailedMessage : null;
        if (DefaultErrorChannel == null && ChannelResolver != null)
        {
            Channel = ChannelResolver.ResolveDestination(IntegrationContextUtils.ErrorChannelBeanName);
        }

        if (failedMessage == null || failedMessage.Headers.ErrorChannel == null)
        {
            return DefaultErrorChannel;
        }

        var errorChannelHeader = failedMessage.Headers.ErrorChannel;
        if (errorChannelHeader is IMessageChannel channel)
        {
            return channel;
        }

        if (errorChannelHeader is not string header)
        {
            throw new ArgumentException($"Unsupported error channel header type. Expected IMessageChannel or String, but actual type is [{errorChannelHeader.GetType()}]");
        }

        if (ChannelResolver != null)
        {
            return ChannelResolver.ResolveDestination(header);
        }
        else
        {
            return null;
        }
    }

    private sealed class DefaultErrorMessageStrategy : IErrorMessageStrategy
    {
        public ErrorMessage BuildErrorMessage(Exception exception, IAttributeAccessor attributeAccessor)
        {
            return exception is MessagingExceptionWrapperException wrapperException
                ? new ErrorMessage(exception.InnerException, wrapperException.FailedMessage)
                : new ErrorMessage(exception);
        }
    }
}
