// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

/// <summary>
/// Wraps <see cref="LoggerFactory" /> to only track loggers with category names starting with "Fake".
/// </summary>
internal sealed class OnlyTrackFakeCategoryLoggerFactory : ILoggerFactory
{
    private readonly LoggerFactory _innerFactory;

    public OnlyTrackFakeCategoryLoggerFactory(IEnumerable<ILoggerProvider> providers, IOptionsMonitor<LoggerFilterOptions> filterOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(filterOptionsMonitor);

        _innerFactory = new LoggerFactory(providers, filterOptionsMonitor);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _innerFactory.AddProvider(provider);
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (!categoryName.StartsWith("Fake", StringComparison.Ordinal))
        {
            // Only track a subset of loggers to make test outputs deterministic.
            return NullLogger.Instance;
        }

        return _innerFactory.CreateLogger(categoryName);
    }

    public void Dispose()
    {
        _innerFactory.Dispose();
    }
}
