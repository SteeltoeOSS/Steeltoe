// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Logging;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

internal sealed class TestLogProvider : IDynamicLoggerProvider
{
    public string? Category { get; private set; }
    public LogLevel MinLevel { get; private set; }
    public bool GetLoggerConfigurationsCalled { get; private set; }

    public ILogger CreateLogger(string categoryName)
    {
        return NullLogger.Instance;
    }

    public void Dispose()
    {
    }

    public ICollection<DynamicLoggerConfiguration> GetLoggerConfigurations()
    {
        GetLoggerConfigurationsCalled = true;
        return new List<DynamicLoggerConfiguration>();
    }

    public void SetLogLevel(string categoryName, LogLevel? minLevel)
    {
        Category = categoryName;
        MinLevel = minLevel ?? LogLevel.None;
    }
}
