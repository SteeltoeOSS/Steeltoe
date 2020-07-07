// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Rabbit.Listener.Exceptions;
using System;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class DefaultExceptionStrategy : IFatalExceptionStrategy
    {
        private readonly ILogger _logger;

        public DefaultExceptionStrategy(ILogger logger = null)
        {
            _logger = logger;
        }

        public bool IsFatal(Exception exception)
        {
            var cause = exception.InnerException;
            while (cause is MessagingException && !(cause is MessageConversionException) && !(cause is MethodArgumentResolutionException))
            {
                cause = cause.InnerException;
            }

            if (exception is ListenerExecutionFailedException && IsCauseFatal(cause))
            {
                _logger?.LogWarning(
                        "Fatal message conversion error; message rejected; "
                                + "it will be dropped or routed to a dead letter exchange, if so configured: "
                                + ((ListenerExecutionFailedException)exception).FailedMessage);

                return true;
            }

            return false;
        }

        protected virtual bool IsUserCauseFatal(Exception cause)
        {
            return false;
        }

        private bool IsCauseFatal(Exception cause)
        {
            return cause is MessageConversionException
                    || cause is MethodArgumentResolutionException
                    || cause is MissingMethodException
                    || cause is InvalidCastException

                    || IsUserCauseFatal(cause);
        }
    }
}
