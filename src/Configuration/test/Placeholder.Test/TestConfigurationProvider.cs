// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Configuration.Placeholder.Test;

internal sealed partial class TestConfigurationProvider(string name, ILogger<TestConfigurationProvider> logger) : ConfigurationProvider, IDisposable
{
    private readonly string _name = name;
    private readonly ILogger<TestConfigurationProvider> _logger = logger;
    private long _loadCount;
    private long _disposeCount;

    public Guid Id { get; } = Guid.NewGuid();
    public long LoadCount => Interlocked.Read(ref _loadCount);
    public long DisposeCount => Interlocked.Read(ref _disposeCount);

    public override void Load()
    {
        LogLoad(_name);
        Interlocked.Increment(ref _loadCount);
    }

    public void Dispose()
    {
        LogDispose(_name);
        Interlocked.Increment(ref _disposeCount);
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Load ({Name}).")]
    private partial void LogLoad(string name);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Dispose ({Name}).")]
    private partial void LogDispose(string name);
}
