// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Infrastructure;

/// <summary>
/// Enables us to write logging messages to XUnit output
/// </summary>
internal class TestOutputLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;
    private readonly ConcurrentDictionary<string, TestOutputLogger> _loggers = new ();

    public TestOutputLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new TestOutputLogger(_output, categoryName));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

internal class TestOutputLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _category;

    public TestOutputLogger(ITestOutputHelper output, string category)
    {
        _output = output;
        _category = category;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var formattetMessage = formatter(state, exception);

        _output?.WriteLine(formattetMessage);
        if (exception != null)
        {
            _output?.WriteLine(exception.StackTrace);
        }
    }
}