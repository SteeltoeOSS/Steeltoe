// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Logging;

/// <summary>
/// Provides access to <see cref="ILogger" /> categories and their minimum log levels and enables decorating log messages.
/// </summary>
public abstract class DynamicLoggerProvider : IDynamicLoggerProvider
{
    // This is the ultimate default minimum level used by .NET if nothing is configured.
    private const LogLevel DefaultLogLevel = LogLevel.Information;

    // Cached delegates to reduce allocations and avoid closures.
    private static readonly LoggerFilter FilterTraceOrHigher = level => level >= LogLevel.Trace;
    private static readonly LoggerFilter FilterDebugOrHigher = level => level >= LogLevel.Debug;
    private static readonly LoggerFilter FilterInformationOrHigher = level => level >= LogLevel.Information;
    private static readonly LoggerFilter FilterWarningOrHigher = level => level >= LogLevel.Warning;
    private static readonly LoggerFilter FilterErrorOrHigher = level => level >= LogLevel.Error;
    private static readonly LoggerFilter FilterCriticalOrHigher = level => level >= LogLevel.Critical;
    private static readonly LoggerFilter FilterNone = _ => false;

    // Guards the three dictionaries below. It's essential that levels are properly updated when diagnosing issues.
    private readonly ReaderWriterLockSlim _lock = new();

    // Contains solely the overridden log levels (not including parent/child categories). These entries can be reset. Typically small.
    private readonly Dictionary<string, LogLevel> _overriddenMinLevelsPerCategory = new();

    // Contains all active ILogger instances. Potentially large.
    private readonly Dictionary<string, MessageProcessingLogger> _activeLoggersPerCategory = new();

    // Contains solely the log levels in IConfiguration (not including parent/child categories). Typically small.
    private IReadOnlyDictionary<string, LogLevel> _configurationMinLevelsPerCategory;

    // The underlying provider to create new ILogger instances from.
    protected ILoggerProvider InnerLoggerProvider { get; }

    // Passed downstream to support other Steeltoe features; this provider has no interest in it.
    protected IReadOnlyCollection<IDynamicMessageProcessor> MessageProcessors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicLoggerProvider" /> class.
    /// </summary>
    /// <param name="innerLoggerProvider">
    /// The <see cref="ILoggerProvider" /> to wrap. Used to create new <see cref="ILogger" /> instances.
    /// </param>
    /// <param name="logLevelsConfiguration">
    /// The minimum log levels per logger category, originating from configuration.
    /// </param>
    /// <param name="messageProcessors">
    /// The processors to decorate log messages with.
    /// </param>
    protected DynamicLoggerProvider(ILoggerProvider innerLoggerProvider, LogLevelsConfiguration logLevelsConfiguration,
        IEnumerable<IDynamicMessageProcessor> messageProcessors)
    {
        ArgumentNullException.ThrowIfNull(innerLoggerProvider);
        ArgumentNullException.ThrowIfNull(logLevelsConfiguration);
        ArgumentNullException.ThrowIfNull(messageProcessors);

        IDynamicMessageProcessor[] messageProcessorArray = messageProcessors.ToArray();
        ArgumentGuard.ElementsNotNull(messageProcessorArray);

        InnerLoggerProvider = innerLoggerProvider;
        _configurationMinLevelsPerCategory = logLevelsConfiguration.MinLevelsPerCategory;
        MessageProcessors = messageProcessorArray.AsReadOnly();
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

        // Taking an upgradable lock (which blocks others from concurrently obtaining an upgradable lock),
        // because creating a new logger is far more common than returning an existing one.
        _lock.EnterUpgradeableReadLock();

        try
        {
            if (_activeLoggersPerCategory.TryGetValue(categoryName, out MessageProcessingLogger? logger))
            {
                return logger;
            }

            _lock.EnterWriteLock();

            try
            {
                // This provider is optimized for efficiently creating loggers.
                // - It calculates the parent categories, which is fast because it is a small set.
                // - It loops over the dictionaries for effective/configuration levels, both of which are small.
                // - Cached delegates for the logger filters are used to minimize allocations.

                logger = CreateMessageProcessingLogger(categoryName);
                _activeLoggersPerCategory.Add(categoryName, logger);

                return logger;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    /// <inheritdoc />
    public ICollection<DynamicLoggerState> GetLogLevels()
    {
        var loggerStates = new Dictionary<string, DynamicLoggerState>();

        _lock.EnterReadLock();

        try
        {
            foreach (string categoryName in GetCategoryNameWithDescendants(string.Empty))
            {
                DynamicLoggerState loggerState = GetLoggerState(categoryName);
                loggerStates.Add(categoryName, loggerState);
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return loggerStates.Values;
    }

    /// <summary>
    /// Returns the incoming category, including all descendant categories. This is potentially an expensive operation.
    /// </summary>
    /// <param name="categoryName">
    /// The logger category name to descend from, or an empty string for all categories.
    /// </param>
    /// <remarks>
    /// Note this typically includes categories not in use. For example, if only logger "A.B.C" exists, ["A.B.C", "A.B", "A"] will be returned.
    /// </remarks>
    private HashSet<string> GetCategoryNameWithDescendants(string categoryName)
    {
        HashSet<string> allCategoryNames = [categoryName];

        foreach (string nextCategoryName in _activeLoggersPerCategory.Keys.SelectMany(GetCategoryNameWithParents))
        {
            if (categoryName.Length == 0 || IsCategoryOrDescendant(nextCategoryName, categoryName))
            {
                allCategoryNames.Add(nextCategoryName);
            }
        }

        return allCategoryNames;
    }

    private DynamicLoggerState GetLoggerState(string categoryName)
    {
        LogLevel effectiveMinLevel = GetEffectiveMinLevel(categoryName);

        // Only set configurationMinLevel when this category can be reset (could be the same as effective level).
        LogLevel? configurationMinLevel = null;

        if (_overriddenMinLevelsPerCategory.ContainsKey(categoryName))
        {
            configurationMinLevel = GetConfigurationMinLevel(categoryName);
        }

        return new DynamicLoggerState(categoryName, configurationMinLevel, effectiveMinLevel);
    }

    /// <inheritdoc />
    public void SetLogLevel(string categoryName, LogLevel? minLevel)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

        _lock.EnterWriteLock();

        try
        {
            UpdateOverriddenLevels(categoryName, minLevel);
            UpdateActiveLoggers(categoryName, minLevel != null);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void UpdateOverriddenLevels(string categoryName, LogLevel? minLevel)
    {
        if (minLevel == null)
        {
            _overriddenMinLevelsPerCategory.Remove(categoryName);
        }
        else
        {
            _overriddenMinLevelsPerCategory[categoryName] = minLevel.Value;
        }

        // Set or Reset implicitly resets all descendants.
        HashSet<string> descendantCategoryNames = GetCategoryNameWithDescendants(categoryName);
        descendantCategoryNames.Remove(categoryName);

        foreach (string descendantCategoryName in descendantCategoryNames)
        {
            _overriddenMinLevelsPerCategory.Remove(descendantCategoryName);
        }
    }

    private void UpdateActiveLoggers(string categoryName, bool isOverride)
    {
        LoggerFilter? overrideFilter = null;

        if (isOverride)
        {
            // Fast path: All descendant loggers get the same filter, ignoring minimum levels from configuration.
            overrideFilter = GetFilter(categoryName);
        }

        foreach ((string nextCategoryName, MessageProcessingLogger nextLogger) in _activeLoggersPerCategory)
        {
            if (categoryName.Length == 0 || IsCategoryOrDescendant(nextCategoryName, categoryName))
            {
                LoggerFilter filter = overrideFilter ?? GetFilter(nextCategoryName);
                nextLogger.ChangeFilter(filter);
            }
        }
    }

    /// <inheritdoc />
    public void RefreshConfiguration(LogLevelsConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _lock.EnterWriteLock();

        try
        {
            _configurationMinLevelsPerCategory = configuration.MinLevelsPerCategory;
            UpdateActiveLoggers(string.Empty, false);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private static bool IsCategoryOrDescendant(string fullCategoryName, string baseCategoryName)
    {
        if (baseCategoryName.Length == 0)
        {
            return true;
        }

        // Category names are case-sensitive.

        if (fullCategoryName.Length == baseCategoryName.Length)
        {
            return fullCategoryName == baseCategoryName;
        }

        if (fullCategoryName.Length > baseCategoryName.Length)
        {
            return fullCategoryName.StartsWith(baseCategoryName + '.', StringComparison.Ordinal);
        }

        return false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            InnerLoggerProvider.Dispose();
            _overriddenMinLevelsPerCategory.Clear();
            _activeLoggersPerCategory.Clear();
            _lock.Dispose();
        }
    }

    protected virtual MessageProcessingLogger CreateMessageProcessingLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

        ILogger logger = InnerLoggerProvider.CreateLogger(categoryName);
        LoggerFilter filter = GetFilter(categoryName);

        return new MessageProcessingLogger(logger, filter, MessageProcessors);
    }

    protected LoggerFilter GetFilter(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

        LogLevel minLevel = GetEffectiveMinLevel(categoryName);

        return minLevel switch
        {
            LogLevel.Trace => FilterTraceOrHigher,
            LogLevel.Debug => FilterDebugOrHigher,
            LogLevel.Information => FilterInformationOrHigher,
            LogLevel.Warning => FilterWarningOrHigher,
            LogLevel.Error => FilterErrorOrHigher,
            LogLevel.Critical => FilterCriticalOrHigher,
            _ => FilterNone
        };
    }

    private LogLevel GetEffectiveMinLevel(string categoryName)
    {
        string[] categoryNames = GetCategoryNameWithParents(categoryName).ToArray();

        foreach (string nextCategoryName in categoryNames)
        {
            if (_overriddenMinLevelsPerCategory.TryGetValue(nextCategoryName, out LogLevel minLevel))
            {
                return minLevel;
            }
        }

        // PERF: Code duplicated from GetConfigurationMinLevel, to avoid building categoryNames twice.
        // GetConfigurationMinLevel is much less likely to execute, which justifies the rebuild there.
        foreach (string nextCategoryName in categoryNames)
        {
            if (_configurationMinLevelsPerCategory.TryGetValue(nextCategoryName, out LogLevel minLevel))
            {
                return minLevel;
            }
        }

        return DefaultLogLevel;
    }

    private static IEnumerable<string> GetCategoryNameWithParents(string categoryName)
    {
        while (categoryName.Length > 0)
        {
            yield return categoryName;
            int lastIndexOfDot = categoryName.LastIndexOf('.');

            if (lastIndexOfDot == -1)
            {
                break;
            }

            categoryName = categoryName[..lastIndexOfDot];
        }

        yield return string.Empty;
    }

    private LogLevel GetConfigurationMinLevel(string categoryName)
    {
        string[] categoryNames = GetCategoryNameWithParents(categoryName).ToArray();

        foreach (string nextCategoryName in categoryNames)
        {
            if (_configurationMinLevelsPerCategory.TryGetValue(nextCategoryName, out LogLevel minLevel))
            {
                return minLevel;
            }
        }

        return DefaultLogLevel;
    }
}
