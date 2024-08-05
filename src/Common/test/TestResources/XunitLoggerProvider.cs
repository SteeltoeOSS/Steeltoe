// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Writes log messages to xUnit output.
/// </summary>
public sealed class XunitLoggerProvider : ILoggerProvider
{
    private static readonly Func<string, LogLevel, bool> DefaultFilter = (_, _) => true;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Func<string, LogLevel, bool> _filter;

    public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
        : this(testOutputHelper, DefaultFilter)
    {
    }

    public XunitLoggerProvider(ITestOutputHelper testOutputHelper, Func<string, bool> filter)
        : this(testOutputHelper, (category, _) => filter(category))
    {
        ArgumentNullException.ThrowIfNull(filter);
    }

    public XunitLoggerProvider(ITestOutputHelper testOutputHelper, Func<string, LogLevel, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(testOutputHelper);
        ArgumentNullException.ThrowIfNull(filter);

        _testOutputHelper = testOutputHelper;
        _filter = filter;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(_testOutputHelper, categoryName, _filter);
    }

    public void Dispose()
    {
    }

    private sealed class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly string _categoryName;
        private readonly Func<string, LogLevel, bool> _filter;

        public XunitLogger(ITestOutputHelper output, string categoryName, Func<string, LogLevel, bool> filter)
        {
            ArgumentNullException.ThrowIfNull(output);
            ArgumentException.ThrowIfNullOrEmpty(categoryName);
            ArgumentNullException.ThrowIfNull(filter);

            _output = output;
            _categoryName = categoryName;
            _filter = filter;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _filter(_categoryName, logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                string message = $"{FormatLevel(logLevel)} {_categoryName}: {formatter(state, exception)}";
                _output.WriteLine(message);
            }
        }

        private static string FormatLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "TRCE",
                LogLevel.Debug => "DBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "FAIL",
                LogLevel.Critical => "CRIT",
                LogLevel.None => "NONE",
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
