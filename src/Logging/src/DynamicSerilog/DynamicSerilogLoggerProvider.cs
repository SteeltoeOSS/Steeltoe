// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Steeltoe.Logging.DynamicSerilog;

/// <summary>
/// Implements <see cref="DynamicLoggerProvider" /> for logging using Serilog.
/// </summary>
public sealed class DynamicSerilogLoggerProvider(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor, IEnumerable<IDynamicMessageProcessor> messageProcessors)
    : DynamicLoggerProvider(CreateSerilogLogger(serilogOptionsMonitor), GetMinimumLevelsFromOptions(serilogOptionsMonitor), messageProcessors)
{
    private static readonly object LoggerLock = new();
    private static Logger? _serilogLogger;

    /// <summary>
    /// Because of how Serilog is implemented, there is a single logger instance for a given application, which gets wrapped by ILoggers. However, while
    /// testing, we need to clear this instance between tests. Should not be required under normal usage.
    /// </summary>
    internal static void ClearLogger()
    {
        lock (LoggerLock)
        {
            _serilogLogger = null;
        }
    }

    protected override MessageProcessingLogger CreateMessageProcessingLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrEmpty(categoryName);

        ILogger logger = InnerLoggerProvider.CreateLogger(categoryName);
        LoggerFilter filter = GetFilter(categoryName);

        return new SerilogMessageProcessingLogger(logger, filter, MessageProcessors);
    }

    private static SerilogLoggerProvider CreateSerilogLogger(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(serilogOptionsMonitor);

        lock (LoggerLock)
        {
            _serilogLogger ??= serilogOptionsMonitor.CurrentValue.GetSerilogConfiguration().CreateLogger();
        }

        return new SerilogLoggerProvider(_serilogLogger);
    }

    private static LoggerFilterConfiguration GetMinimumLevelsFromOptions(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(serilogOptionsMonitor);

        SerilogOptions options = serilogOptionsMonitor.CurrentValue;
        var defaultMinLevel = (LogLevel)options.MinimumLevel!.Default;

        var configurationMinLevels = new Dictionary<string, LogLevel>
        {
            [DefaultCategoryName] = defaultMinLevel
        };

        var effectiveFilters = new Dictionary<string, LoggerFilter>();

        foreach ((string? categoryName, LogEventLevel minEventLevel) in options.MinimumLevel.Override)
        {
            var minLevel = (LogLevel)minEventLevel;

            configurationMinLevels[categoryName] = minLevel;
            effectiveFilters[categoryName] = level => level >= minLevel;
        }

        LoggerFilter defaultFilter = level => level >= defaultMinLevel;
        return new LoggerFilterConfiguration(configurationMinLevels, effectiveFilters, defaultFilter);
    }
}
