// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Infrastructure;

/// <summary>
/// Enables us to write logging messages to XUnit output.
/// </summary>
internal sealed class TestOutputLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;
    private readonly ConcurrentDictionary<string, TestOutputLogger> _loggers = new ();

    public TestOutputLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, _ => new TestOutputLogger(_output, categoryName));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}