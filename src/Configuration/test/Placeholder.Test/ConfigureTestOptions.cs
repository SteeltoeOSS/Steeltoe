// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Configuration.Placeholder.Test;

internal sealed partial class ConfigureTestOptions(ILogger<ConfigureTestOptions> logger) : IConfigureOptions<TestOptions>
{
    private readonly ILogger<ConfigureTestOptions> _logger = logger;
    private long _configureCount;

    public long ConfigureCount => Interlocked.Read(ref _configureCount);

    public void Configure(TestOptions options)
    {
        LogConfigure(options.Value);
        Interlocked.Increment(ref _configureCount);
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Configure with value '{Value}'.")]
    private partial void LogConfigure(string? value);
}
