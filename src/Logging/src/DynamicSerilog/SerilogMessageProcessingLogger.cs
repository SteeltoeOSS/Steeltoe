// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Logging.DynamicSerilog;

public sealed class SerilogMessageProcessingLogger : MessageProcessingLogger
{
    public SerilogMessageProcessingLogger(ILogger innerLogger, LoggerFilter filter, IEnumerable<IDynamicMessageProcessor> messageProcessors)
        : base(innerLogger, filter, messageProcessors)
    {
    }

    /// <inheritdoc />
    public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        // Serilog has its own way of formatting structured messages, therefore it never evaluates the formatter callback.
        // If we'd do that here and pass the formatted message down to Serilog, we'd lose the structure in logs.
        // The next best thing to ensure output from message processors appears in the logs, is to emit it separately in a scope.
        // A consequence of this is that message processors won't see the original message, but they can add extra information.

        string scopeMessage = string.Empty;

        foreach (IDynamicMessageProcessor processor in MessageProcessors)
        {
            scopeMessage = processor.Process(scopeMessage);
        }

        if (!string.IsNullOrEmpty(scopeMessage))
        {
            using (InnerLogger.BeginScope(scopeMessage))
            {
                InnerLogger.Log(logLevel, eventId, state, exception, formatter);
            }
        }
        else
        {
            InnerLogger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
