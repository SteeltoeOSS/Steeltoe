// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Enables capturing log messages in tests.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private static readonly Func<string, LogLevel, bool> DefaultFilter = (_, _) => true;
    private readonly Func<string, LogLevel, bool> _filter;

#if NET9_0_OR_GREATER
    private readonly Lock _lockObject = new();
#else
    private readonly object _lockObject = new();
#endif

    private readonly List<string> _messages = [];

    public bool IncludeStackTraces { get; init; }

    public CapturingLoggerProvider()
        : this(DefaultFilter)
    {
    }

    public CapturingLoggerProvider(Func<string, bool> filter)
        : this((category, _) => filter(category))
    {
    }

    public CapturingLoggerProvider(Func<string, LogLevel, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _filter = filter;
    }

    public ILogger CreateLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

        return new CapturingLogger(this, categoryName, _filter);
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            _messages.Clear();
        }
    }

    public IList<string> GetAll()
    {
        lock (_lockObject)
        {
            return _messages.ToArray();
        }
    }

    public string GetAsText()
    {
        lock (_lockObject)
        {
            return string.Join(Environment.NewLine, _messages);
        }
    }

    private void Add(string message)
    {
        lock (_lockObject)
        {
            _messages.Add(message);
        }
    }

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(CapturingLoggerProvider owner, string categoryName, Func<string, LogLevel, bool> filter) : ILogger
    {
        private readonly CapturingLoggerProvider _owner = owner;
        private readonly string _categoryName = categoryName;
        private readonly Func<string, LogLevel, bool> _filter = filter;

        public bool IsEnabled(LogLevel logLevel)
        {
            return _filter(_categoryName, logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                string message = _owner.IncludeStackTraces && exception != null
                    ? $"{FormatLevel(logLevel)} {_categoryName}: {formatter(state, exception)}{Environment.NewLine} {exception}"
                    : $"{FormatLevel(logLevel)} {_categoryName}: {formatter(state, exception)}";

                _owner.Add(message);
            }
        }

        private static string FormatLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                // ReSharper disable StringLiteralTypo
                LogLevel.Trace => "TRCE",
                LogLevel.Debug => "DBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "FAIL",
                LogLevel.Critical => "CRIT",
                LogLevel.None => "NONE",
                // ReSharper restore StringLiteralTypo
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return EmptyDisposable.Instance;
        }
    }
}
