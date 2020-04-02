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
