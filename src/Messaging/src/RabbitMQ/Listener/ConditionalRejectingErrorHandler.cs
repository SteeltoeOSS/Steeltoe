// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class ConditionalRejectingErrorHandler : IErrorHandler
{
    public const string DefaultServiceName = nameof(ConditionalRejectingErrorHandler);

    private readonly ILogger _logger;
    private readonly IFatalExceptionStrategy _exceptionStrategy;

    public virtual bool DiscardFatalErrorsWithXDeath { get; set; } = true;

    public virtual bool RejectManual { get; set; } = true;

    public string ServiceName { get; set; } = DefaultServiceName;

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

    public virtual bool HandleError(Exception exception)
    {
        _logger?.LogWarning(exception, "Execution of Rabbit message listener failed.");

        if (!CauseChainContainsRabbitRejectAndDoNotRequeueException(exception) && _exceptionStrategy.IsFatal(exception))
        {
            if (DiscardFatalErrorsWithXDeath && exception is ListenerExecutionFailedException listenerException)
            {
                IMessage failed = listenerException.FailedMessage;

                if (failed != null)
                {
                    RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(failed);
                    List<Dictionary<string, object>> xDeath = accessor.GetXDeathHeader();

                    if (xDeath != null && xDeath.Count > 0)
                    {
                        _logger?.LogError(
                            "x-death header detected on a message with a fatal exception; " + "perhaps requeued from a DLQ? - discarding: {failedMessage} ",
                            failed);

                        throw new ImmediateAcknowledgeException("Fatal and x-death present");
                    }
                }
            }

            throw new RabbitRejectAndDoNotRequeueException("Error Handler converted exception to fatal", RejectManual, exception);
        }

        return true;
    }

    protected virtual bool CauseChainContainsRabbitRejectAndDoNotRequeueException(Exception exception)
    {
        Exception cause = exception.InnerException;

        while (cause != null)
        {
            if (cause is RabbitRejectAndDoNotRequeueException)
            {
                return true;
            }

            cause = cause.InnerException;
        }

        return false;
    }
}
