// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Xunit.Sdk;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Enables capturing log output in failing tests. Call <see cref="Register" /> or use <see cref="LoggerFactory" /> to hook up. When an assertion fails,
/// the log output is included in the exception message.
/// </summary>
public sealed class TestFailureTracer : IDisposable
{
    private readonly CapturingLoggerProvider _capturingLoggerProvider;
    private readonly Lazy<LoggerFactory> _loggerFactoryLazy;

    public ILoggerFactory LoggerFactory => _loggerFactoryLazy.Value;

    private TestFailureTracer()
    {
        _capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Steeltoe.", StringComparison.Ordinal))
        {
            IncludeStackTraces = true
        };

        _loggerFactoryLazy = new Lazy<LoggerFactory>(() => new LoggerFactory([_capturingLoggerProvider], new LoggerFilterOptions
        {
            MinLevel = LogLevel.Trace
        }));
    }

    public void Register(ILoggingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.SetMinimumLevel(LogLevel.Trace).AddProvider(_capturingLoggerProvider);
    }

    public static async Task CaptureAsync(Func<TestFailureTracer, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var tracer = new TestFailureTracer();

        try
        {
            await action(tracer);
        }
        catch (XunitException exception)
        {
            throw IncludeLogOutputInExceptionMessage(exception, tracer);
        }
    }

    private static XunitException IncludeLogOutputInExceptionMessage(XunitException exception, TestFailureTracer tracer)
    {
        string message = $"""
            {exception.Message}

            Captured logs:
            {tracer._capturingLoggerProvider.GetAsText()}

            """;

        return new XunitException(message, exception);
    }

    public void Dispose()
    {
        _capturingLoggerProvider.Dispose();

        if (_loggerFactoryLazy.IsValueCreated)
        {
            _loggerFactoryLazy.Value.Dispose();
        }
    }
}
