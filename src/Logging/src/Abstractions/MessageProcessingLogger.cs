// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Logging;

/// <summary>
/// Wraps an <see cref="ILogger" /> with the ability to change its minimum log level at runtime. Decorates log messages using
/// <see cref="IDynamicMessageProcessor" />.
/// </summary>
public class MessageProcessingLogger : ILogger
{
    private LoggerFilter _filter;

    protected ICollection<IDynamicMessageProcessor> MessageProcessors { get; }

    protected internal ILogger InnerLogger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageProcessingLogger" /> class.
    /// </summary>
    /// <param name="innerLogger">
    /// The <see cref="ILogger" /> to wrap.
    /// </param>
    /// <param name="filter">
    /// The filter, which determines whether logging is enabled.
    /// </param>
    /// <param name="messageProcessors">
    /// The message processors to decorate log messages with.
    /// </param>
    public MessageProcessingLogger(ILogger innerLogger, LoggerFilter filter, IEnumerable<IDynamicMessageProcessor> messageProcessors)
    {
        ArgumentNullException.ThrowIfNull(innerLogger);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(messageProcessors);

        IDynamicMessageProcessor[] messageProcessorArray = messageProcessors.ToArray();
        ArgumentGuard.ElementsNotNull(messageProcessorArray);

        InnerLogger = innerLogger;
        _filter = filter;
        MessageProcessors = messageProcessorArray;
    }

    /// <summary>
    /// Changes the log level filter at runtime.
    /// </summary>
    /// <param name="filter">
    /// The updated filter, which determines whether logging is enabled.
    /// </param>
    public void ChangeFilter(LoggerFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _filter = filter;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return InnerLogger.BeginScope(state);
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel == LogLevel.None)
        {
            return false;
        }

        return _filter.Invoke(logLevel);
    }

    /// <inheritdoc />
    public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        Func<TState, Exception?, string> compositeFormatter = (innerState, innerException) =>
            ApplyMessageProcessors(innerState, innerException, formatter, MessageProcessors);

        InnerLogger.Log(logLevel, eventId, state, exception, compositeFormatter);
    }

    private static string ApplyMessageProcessors<TState>(TState state, Exception? exception, Func<TState, Exception?, string> formatter,
        IEnumerable<IDynamicMessageProcessor> processors)
    {
        string message = formatter(state, exception);

        foreach (IDynamicMessageProcessor processor in processors)
        {
            message = processor.Process(message);
        }

        return message;
    }
}
