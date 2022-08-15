// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration;

public class ErrorMessagePublisher
{
    private readonly IApplicationContext _context;
    protected readonly MessagingTemplate InnerMessagingTemplate;
    protected readonly ILogger Logger;
    private IIntegrationServices _integrationServices;
    private IDestinationResolver<IMessageChannel> _channelResolver;
    private IMessageChannel _channel;
    private IErrorMessageStrategy _errorMessageStrategy = new DefaultErrorMessageStrategy();

    protected virtual MessagingTemplate MessagingTemplate => InnerMessagingTemplate;

    public IIntegrationServices IntegrationServices
    {
        get
        {
            _integrationServices ??= IntegrationServicesUtils.GetIntegrationServices(_context);
            return _integrationServices;
        }
    }

    public virtual IErrorMessageStrategy ErrorMessageStrategy
    {
        get => _errorMessageStrategy;
        set
        {
            ArgumentGuard.NotNull(value);

            _errorMessageStrategy = value;
        }
    }

    public virtual IMessageChannel Channel
    {
        get
        {
            PopulateChannel();

            return _channel;
        }
        set => _channel = value;
    }

    public virtual string ChannelName { get; set; }

    public virtual int SendTimeout
    {
        get => InnerMessagingTemplate.SendTimeout;
        set => InnerMessagingTemplate.SendTimeout = value;
    }

    public virtual IDestinationResolver<IMessageChannel> ChannelResolver
    {
        get
        {
            _channelResolver ??= IntegrationServices.ChannelResolver;
            return _channelResolver;
        }
        set
        {
            ArgumentGuard.NotNull(value);

            _channelResolver = value;
        }
    }

    public ErrorMessagePublisher(IApplicationContext context, ILogger logger = null)
    {
        _context = context;
        Logger = logger;
        InnerMessagingTemplate = new MessagingTemplate(context);
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

        Exception payload = DeterminePayload(exception, context);
        ErrorMessage errorMessage = _errorMessageStrategy.BuildErrorMessage(payload, context);

        InnerMessagingTemplate.Send(errorMessage);
    }

    protected virtual Exception DeterminePayload(Exception exception, IAttributeAccessor context)
    {
        Exception lastThrowable = exception;

        if (lastThrowable == null)
        {
            lastThrowable = PayloadWhenNull(context);
        }
        else if (lastThrowable is not MessagingException)
        {
            var message = (IMessage)context.GetAttribute(ErrorMessageUtils.FailedMessageContextKey);

            lastThrowable = message == null
                ? new MessagingException(lastThrowable.Message, lastThrowable)
                : new MessagingException(message, lastThrowable.Message, lastThrowable);
        }

        return lastThrowable;
    }

    protected virtual Exception PayloadWhenNull(IAttributeAccessor context)
    {
        var message = (IMessage)context.GetAttribute(ErrorMessageUtils.FailedMessageContextKey);

        return message == null
            ? new MessagingException("No root cause exception available")
            : new MessagingException(message, "No root cause exception available");
    }

    private void PopulateChannel()
    {
        if (InnerMessagingTemplate.DefaultDestination == null)
        {
            if (_channel == null)
            {
                string recoveryChannelName = ChannelName ?? IntegrationContextUtils.ErrorChannelBeanName;

                if (_channelResolver != null)
                {
                    _channel = _channelResolver.ResolveDestination(recoveryChannelName);
                }
            }

            InnerMessagingTemplate.DefaultDestination = _channel;
        }
    }
}
