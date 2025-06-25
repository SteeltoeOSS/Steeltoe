// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Logging;

[DebuggerDisplay("{DebuggerToString(),nq}")]
internal sealed class UpgradableLogger(ILogger innerLogger, string categoryName) : ILogger
{
    private volatile ILogger _innerLogger = innerLogger;

    public string CategoryName { get; } = categoryName;

    public void Upgrade(ILogger logger)
    {
        _innerLogger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return _innerLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _innerLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _innerLogger.Log(logLevel, eventId, state, exception, formatter);
    }

    private string DebuggerToString()
    {
        Type innerLoggerType = _innerLogger.GetType();

        if (innerLoggerType.Name == "Microsoft.Extensions.Logging.Logger")
        {
            MethodInfo? method = innerLoggerType.GetMethod("DebuggerToString", BindingFlags.Instance | BindingFlags.NonPublic);

            if (method?.Invoke(_innerLogger, []) is string displayText)
            {
                return displayText;
            }
        }

        return CategoryName;
    }
}
