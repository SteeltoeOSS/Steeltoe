// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging;

public class StructuredMessageProcessingLogger : MessageProcessingLogger
{
    public StructuredMessageProcessingLogger(ILogger iLogger, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
        : base(iLogger, messageProcessors)
    {
    }

    public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        var processorMessage = string.Empty;

        if (_messageProcessors != null)
        {
            foreach (var processor in _messageProcessors)
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