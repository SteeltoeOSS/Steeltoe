// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Logging;

public class MessageProcessingLogger : ILogger
{
    private readonly IEnumerable<IDynamicMessageProcessor> _messageProcessors;

    public ILogger Delegate { get; }

    public Func<string, LogLevel, bool> Filter { get; internal set; }

    public string Name { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageProcessingLogger" /> class. Wraps an ILogger and decorates log messages via
    /// <see cref="IDynamicMessageProcessor" />.
    /// </summary>
    /// <param name="iLogger">
    /// The <see cref="ILogger" /> being wrapped.
    /// </param>
    /// <param name="messageProcessors">
    /// The list of <see cref="IDynamicMessageProcessor" />s.
    /// </param>
    public MessageProcessingLogger(ILogger iLogger, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
    {
        _messageProcessors = messageProcessors;
        Delegate = iLogger;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return Delegate.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return Filter.Invoke(Name, logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        string message = formatter(state, exception);

        if (_messageProcessors != null)
        {
            foreach (IDynamicMessageProcessor processor in _messageProcessors)
            {
                message = processor.Process(message);
            }
        }

        if (!string.IsNullOrEmpty(message) || exception != null)
        {
            WriteMessage(logLevel, Name, eventId.Id, message, exception);
        }
    }

    public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception exception)
    {
        Delegate.Log(logLevel, eventId, exception, message);
    }
}
