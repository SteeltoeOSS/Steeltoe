// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Logging;

public class StructuredMessageProcessingLogger : MessageProcessingLogger
{
    public StructuredMessageProcessingLogger(ILogger logger)
        : base(logger, messageProcessors: null)
    {
    }
    public StructuredMessageProcessingLogger(ILogger logger, IEnumerable<IDynamicMessageProcessor> messageProcessors)
        :base(logger, messageProcessors)
    {

    }

    public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string processorMessage = string.Empty;

        if (MessageProcessors != null)
        {
            foreach (IDynamicMessageProcessor processor in MessageProcessors)
            {
                processorMessage = processor.Process(processorMessage);
            }
        }

        if (!string.IsNullOrEmpty(processorMessage))
        {
            using (Delegate.BeginScope(processorMessage))
            {
                Delegate.Log(logLevel, eventId, state, exception, formatter);
            }
        }
        else
        {
            Delegate.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
