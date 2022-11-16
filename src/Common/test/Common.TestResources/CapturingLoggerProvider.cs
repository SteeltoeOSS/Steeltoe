// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.TestResources;

public sealed class CapturingLoggerProvider : ILoggerProvider, ILogger
{
    private readonly ConcurrentBag<string> _messages = new();

    public ILogger CreateLogger(string categoryName)
    {
        return this;
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
        string message = $"{logLevel.ToString().ToUpperInvariant()}: {formatter(state, exception)}";
        _messages.Add(message);
    }

    public IEnumerable<string> GetMessages()
    {
        return _messages.ToList();
    }

    public void Dispose()
    {
        // Intentionally left empty.
    }
}
