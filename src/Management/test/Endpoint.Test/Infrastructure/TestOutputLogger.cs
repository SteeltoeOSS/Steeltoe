// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Infrastructure;

internal sealed class TestOutputLogger : ILogger
{
    private readonly ITestOutputHelper _output;

    public TestOutputLogger(ITestOutputHelper output)
    {
        ArgumentGuard.NotNull(output);

        _output = output;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return EmptyDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string formattedMessage = formatter(state, exception);

        _output.WriteLine(formattedMessage);

        if (exception != null)
        {
            _output.WriteLine(exception.StackTrace);
        }
    }
}
