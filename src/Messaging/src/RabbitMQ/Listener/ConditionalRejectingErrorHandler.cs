// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Listener.Exceptions;
using System;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class ConditionalRejectingErrorHandler : IErrorHandler
    {
        private readonly ILogger _logger;
        private readonly IFatalExceptionStrategy _exceptionStrategy;

        public ConditionalRejectingErrorHandler(ILogger logger = null)
        {
            _logger = logger;
            _exceptionStrategy = new DefaultExceptionStrategy(logger);
        }

        public ConditionalRejectingErrorHandler(IFatalExceptionStrategy exceptionStrategy, ILogger logger = null)
        {
            _logger = logger;
            _exceptionStrategy = exceptionStrategy;
        }

        public bool DiscardFatalsWithXDeath { get; set; } = true;

        public bool RejectManual { get; set; } = true;

        public bool HandleError(Exception exception)
        {
            _logger?.LogWarning("Execution of Rabbit message listener failed.", exception);
            if (!CauseChainContainsARADRE(exception) && _exceptionStrategy.IsFatal(exception))
            {
                if (DiscardFatalsWithXDeath && exception is ListenerExecutionFailedException)
                {
                    var failed = ((ListenerExecutionFailedException)exception).FailedMessage;
                    if (failed != null)
                    {
                        var xDeath = failed.MessageProperties.GetXDeathHeader();
                        if (xDeath != null && xDeath.Count > 0)
                        {
                            _logger?.LogError("x-death header detected on a message with a fatal exception; "
                                    + "perhaps requeued from a DLQ? - discarding: " + failed);
                            throw new ImmediateAcknowledgeAmqpException("Fatal and x-death present");
                        }
                    }
                }

                throw new AmqpRejectAndDontRequeueException("Error Handler converted exception to fatal", RejectManual, exception);
            }

            return true;
        }

        protected bool CauseChainContainsARADRE(Exception exception)
        {
            var cause = exception.InnerException;
            while (cause != null)
            {
                if (cause is AmqpRejectAndDontRequeueException)
                {
                    return true;
                }

                cause = cause.InnerException;
            }

            return false;
        }
    }
}
