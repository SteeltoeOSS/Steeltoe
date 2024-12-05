// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Logging;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

internal sealed class TestLoggerProvider : IDynamicLoggerProvider
{
    public bool HasCalledGetLogLevels { get; private set; }
    public string? SetCategory { get; private set; }
    public LogLevel? SetMinLevel { get; private set; }

    public ILogger CreateLogger(string categoryName)
    {
        return NullLogger.Instance;
    }

    public ICollection<DynamicLoggerState> GetLogLevels()
    {
        HasCalledGetLogLevels = true;
        return [];
    }

    public void SetLogLevel(string categoryName, LogLevel? minLevel)
    {
        SetCategory = categoryName;
        SetMinLevel = minLevel;
    }

    public void RefreshConfiguration(LogLevelsConfiguration configuration)
    {
    }

    public void Dispose()
    {
    }
}
