// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Steeltoe.Common.TestResources;

public sealed class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _output;

    public XunitLogger(ITestOutputHelper output)
    {
        _output = output;
        LoggerFactory.Create(builder => builder.AddConsole());
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return EmptyDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _output.WriteLine(formatter(state, exception));
    }
}
