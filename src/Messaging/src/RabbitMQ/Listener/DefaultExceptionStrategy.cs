// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class DefaultExceptionStrategy : IFatalExceptionStrategy
{
    private readonly ILogger _logger;

    public DefaultExceptionStrategy(ILogger logger = null)
    {
        _logger = logger;
    }

    public bool IsFatal(Exception exception)
    {
        Exception cause = exception.InnerException;

        while (cause is MessagingException && cause is not MessageConversionException && cause is not MethodArgumentResolutionException)
        {
            cause = cause.InnerException;
        }

        switch (exception)
        {
            case ListenerExecutionFailedException listenerExecution when IsCauseFatal(cause):
                _logger?.LogWarning("Fatal message conversion error; message rejected; " +
                    "it will be dropped or routed to a dead letter exchange, if so configured: " + listenerExecution.FailedMessage);

                return true;
            default:
                return false;
        }
    }

    protected virtual bool IsUserCauseFatal(Exception cause)
    {
        return false;
    }

    private bool IsCauseFatal(Exception exception)
    {
        return exception switch
        {
            MessageConversionException => true,
            MethodArgumentResolutionException => true,
            MissingMethodException => true,
            InvalidCastException => true,
            _ => IsUserCauseFatal(exception)
        };
    }
}
