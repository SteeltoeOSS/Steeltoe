// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using LockPrimitive =
#if NET10_0_OR_GREATER
    System.Threading.Lock
#else
    object
#endif
    ;

namespace Steeltoe.Logging.DynamicSerilog;

/// <summary>
/// Implements <see cref="DynamicLoggerProvider" /> for logging using Serilog.
/// </summary>
public sealed class DynamicSerilogLoggerProvider : DynamicLoggerProvider
{
    private static readonly LockPrimitive LoggerLock = new();
    private static Logger? _serilogLogger;
    private readonly IDisposable? _optionsChangeListener;

    public DynamicSerilogLoggerProvider(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor, IEnumerable<IDynamicMessageProcessor> messageProcessors)
        : base(CreateSerilogLogger(serilogOptionsMonitor), GetMinimumLevelsFromOptionsMonitor(serilogOptionsMonitor), messageProcessors)
    {
        ArgumentNullException.ThrowIfNull(serilogOptionsMonitor);

        _optionsChangeListener = serilogOptionsMonitor.OnChange(options =>
        {
            LogLevelsConfiguration configuration = GetMinimumLevelsFromOptions(options);
            RefreshConfiguration(configuration);
        });
    }

    private static LogLevelsConfiguration GetMinimumLevelsFromOptionsMonitor(IOptionsMonitor<SerilogOptions> serilogOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(serilogOptionsMonitor);

        return GetMinimumLevelsFromOptions(serilogOptionsMonitor.CurrentValue);
    }

    /// <summary>
    /// Because of how Serilog is implemented, there is a single logger instance for a given application, which gets wrapped by ILoggers. However, while
    /// testing, we need to clear this instance between tests. Should not be required under normal usage.
    /// </summary>
    internal static void ClearLogger()
    {
        lock (LoggerLock)
        {
            _serilogLogger?.Dispose();
            _serilogLogger = null;
        }
    }

    protected override MessageProcessingLogger CreateMessageProcessingLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

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
            return new SerilogLoggerProvider(_serilogLogger);
        }
    }

    private static LogLevelsConfiguration GetMinimumLevelsFromOptions(SerilogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var minLevelsPerCategory = new Dictionary<string, LogLevel>();

        if (options.MinimumLevel != null)
        {
            var defaultMinLevel = (LogLevel)options.MinimumLevel.Default;
            minLevelsPerCategory.Add(string.Empty, defaultMinLevel);

            foreach ((string categoryName, LogEventLevel minEventLevel) in options.MinimumLevel.Override)
            {
                var ruleMinLevel = (LogLevel)minEventLevel;
                minLevelsPerCategory.Add(categoryName, ruleMinLevel);
            }
        }

        return new LogLevelsConfiguration(minLevelsPerCategory.AsReadOnly());
    }

    protected override void Dispose(bool disposing)
    {
        _optionsChangeListener?.Dispose();
        ClearLogger();
        base.Dispose(disposing);
    }
}
