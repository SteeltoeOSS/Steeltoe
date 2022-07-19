// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Filter = System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool>;

namespace Steeltoe.Extensions.Logging.DynamicSerilog;

public class SerilogDynamicProvider : DynamicLoggerProviderBase
{
    private static readonly object Sync = new ();
    private static Serilog.Core.Logger _serilogLogger;

    public SerilogDynamicProvider(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
        : base(() => GetDelegateLogger(serilogOptionsMonitor), GetInitialLevelsFromOptions(serilogOptionsMonitor), messageProcessors)
    {
    }

    /// <summary>
    /// Because of how Serilog is implemented, there is a single instance of logger for a given application, which gets wrapped by ILoggers.
    /// However while testing, we need to clear this instance between tests.
    /// Should not be required under normal usage.
    /// </summary>
    internal static void ClearLogger()
    {
        _serilogLogger = null;
    }

    private static ILoggerProvider GetDelegateLogger(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor)
    {
        var serilogOptions = serilogOptionsMonitor?.CurrentValue ?? throw new ArgumentNullException(nameof(serilogOptionsMonitor));

        lock (Sync)
        {
            _serilogLogger ??= serilogOptions.GetSerilogConfiguration().CreateLogger(); // Cannot create more than once, so protect with a lock and static property
        }

        return new Serilog.Extensions.Logging.SerilogLoggerProvider(_serilogLogger);
    }

    private static InitialLevels GetInitialLevelsFromOptions(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor)
    {
        var serilogOptions = serilogOptionsMonitor.CurrentValue;
        var defaultLevel = (LogLevel)serilogOptions.MinimumLevel.Default;
        var originalLevels = new Dictionary<string, LogLevel>();
        var runningLevelFilters = new Dictionary<string, Filter>();
        originalLevels["Default"] = defaultLevel;

        foreach (var overrideLevel in serilogOptions.MinimumLevel.Override)
        {
            var logLevel = (LogLevel)overrideLevel.Value;
            originalLevels[overrideLevel.Key] = logLevel;
            runningLevelFilters[overrideLevel.Key] = (_, level) => level >= logLevel;
        }

        return new InitialLevels
        {
            DefaultLevelFilter = (_, level) => level >= defaultLevel,
            RunningLevelFilters = runningLevelFilters,
            OriginalLevels = originalLevels
        };
    }
}
