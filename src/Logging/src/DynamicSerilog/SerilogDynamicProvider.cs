// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Filter = System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool>;

namespace Steeltoe.Extensions.Logging.DynamicSerilog;

public class SerilogDynamicProvider : DynamicLoggerProviderBase
{
    private static readonly object Sync = new();
    private static Logger _serilogLogger;

    public SerilogDynamicProvider(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
        : base(() => GetDelegateLogger(serilogOptionsMonitor), GetInitialLevelsFromOptions(serilogOptionsMonitor), messageProcessors)
    {
    }

    /// <summary>
    /// Because of how Serilog is implemented, there is a single instance of logger for a given application, which gets wrapped by ILoggers. However while
    /// testing, we need to clear this instance between tests. Should not be required under normal usage.
    /// </summary>
    internal static void ClearLogger()
    {
        _serilogLogger = null;
    }

    private protected override MessageProcessingLogger CreateLoggerImplementation(string name)
    {
        ILogger logger = DelegateProvider.CreateLogger(name);

        return new StructuredMessageProcessingLogger(logger, MessageProcessors)
        {
            Filter = GetFilter(name),
            Name = name
        };
    }

    private static ILoggerProvider GetDelegateLogger(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor)
    {
        SerilogOptions serilogOptions = serilogOptionsMonitor?.CurrentValue;

        if (serilogOptions == null)
        {
            throw new InvalidOperationException($"{nameof(serilogOptionsMonitor.CurrentValue)} must not be null.");
        }

        lock (Sync)
        {
            // Cannot create more than once, so protect with a lock and static property
            _serilogLogger ??= serilogOptions.GetSerilogConfiguration().CreateLogger();
        }

        return new SerilogLoggerProvider(_serilogLogger);
    }

    private static InitialLevels GetInitialLevelsFromOptions(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor)
    {
        SerilogOptions serilogOptions = serilogOptionsMonitor.CurrentValue;
        var defaultLevel = (LogLevel)serilogOptions.MinimumLevel.Default;
        var originalLevels = new Dictionary<string, LogLevel>();
        var runningLevelFilters = new Dictionary<string, Filter>();
        originalLevels["Default"] = defaultLevel;

        foreach (KeyValuePair<string, LogEventLevel> overrideLevel in serilogOptions.MinimumLevel.Override)
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
