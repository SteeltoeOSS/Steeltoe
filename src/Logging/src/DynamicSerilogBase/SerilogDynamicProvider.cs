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
    private static readonly object _sync = new ();
    private static Serilog.Core.Logger _serilogger;

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
        _serilogger = null;
    }

    protected override MessageProcessingLogger CreateLoggerImplementation(string name)
    {
        var logger = _delegate.CreateLogger(name);
        return new StructuredMessageProcessingLogger(logger, _messageProcessors) { Filter = GetFilter(name), Name = name };
    }

    private static ILoggerProvider GetDelegateLogger(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor)
    {
        var serilogOptions = serilogOptionsMonitor?.CurrentValue ?? throw new ArgumentNullException(nameof(serilogOptionsMonitor));

        lock (_sync)
        {
            _serilogger ??= serilogOptions.GetSerilogConfiguration().CreateLogger(); // Cannot create more than once, so protect with a lock and static property
        }

        return new Serilog.Extensions.Logging.SerilogLoggerProvider(_serilogger);
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
            runningLevelFilters[overrideLevel.Key] = (category, level) => level >= logLevel;
        }

        return new InitialLevels
        {
            DefaultLevelFilter = (category, level) => level >= defaultLevel,
            RunningLevelFilters = runningLevelFilters,
            OriginalLevels = originalLevels
        };
    }
}