// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Logging;

/// <summary>
/// Provides access to <see cref="ILogger" /> categories and their minimum log levels and enables to decorate log messages.
/// </summary>
public abstract class DynamicLoggerProvider : IDynamicLoggerProvider
{
    protected const string DefaultCategoryName = "Default";
    protected const LogLevel DefaultLogLevel = LogLevel.Information;

    private readonly IReadOnlyDictionary<string, LogLevel> _configurationMinLevels;
    private readonly ConcurrentDictionary<string, LoggerFilter> _effectiveFiltersPerCategory;
    private readonly ConcurrentDictionary<string, MessageProcessingLogger> _activeLoggersPerCategory = new();
    private LoggerFilter _defaultFilter;

    protected ILoggerProvider InnerLoggerProvider { get; }
    protected IEnumerable<IDynamicMessageProcessor> MessageProcessors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicLoggerProvider" /> class.
    /// </summary>
    /// <param name="innerLoggerProvider">
    /// The <see cref="ILoggerProvider" /> to wrap.
    /// </param>
    /// <param name="loggerFilterConfiguration">
    /// The minimum log levels and filters per category at application startup.
    /// </param>
    /// <param name="messageProcessors">
    /// The processors to decorate log messages with.
    /// </param>
    protected DynamicLoggerProvider(ILoggerProvider innerLoggerProvider, LoggerFilterConfiguration loggerFilterConfiguration,
        IEnumerable<IDynamicMessageProcessor> messageProcessors)
    {
        ArgumentGuard.NotNull(innerLoggerProvider);
        ArgumentGuard.NotNull(loggerFilterConfiguration);
        ArgumentGuard.NotNull(messageProcessors);

        InnerLoggerProvider = innerLoggerProvider;
        _configurationMinLevels = loggerFilterConfiguration.ConfigurationMinLevels;
        _effectiveFiltersPerCategory = new ConcurrentDictionary<string, LoggerFilter>(loggerFilterConfiguration.EffectiveFilters, StringComparer.Ordinal);
        _defaultFilter = loggerFilterConfiguration.DefaultFilter;
        MessageProcessors = messageProcessors;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        ArgumentGuard.NotNullOrEmpty(categoryName);

        return _activeLoggersPerCategory.GetOrAdd(categoryName, CreateMessageProcessingLogger(categoryName));
    }

    /// <inheritdoc />
    public ICollection<DynamicLoggerConfiguration> GetLoggerConfigurations()
    {
        LogLevel defaultConfigurationMinLevel = GetConfigurationMinLevel(DefaultCategoryName) ?? DefaultLogLevel;
        LogLevel defaultEffectiveMinLevel = GetMinLevelFromFilter(_defaultFilter);

        var configurations = new Dictionary<string, DynamicLoggerConfiguration>
        {
            [DefaultCategoryName] = new(DefaultCategoryName, defaultConfigurationMinLevel, defaultEffectiveMinLevel)
        };

        foreach (string categoryName in _activeLoggersPerCategory.Keys.Where(name => name != DefaultCategoryName).SelectMany(GetCategoryNameWithParents))
        {
            if (!configurations.ContainsKey(categoryName))
            {
                LogLevel? configurationMinLevel = GetConfigurationMinLevel(categoryName);
                LogLevel effectiveMinLevel = GetEffectiveMinLevel(categoryName) ?? defaultEffectiveMinLevel;

                configurations[categoryName] = new DynamicLoggerConfiguration(categoryName, configurationMinLevel, effectiveMinLevel);
            }
        }

        return configurations.Values;
    }

    /// <inheritdoc />
    public void SetLogLevel(string categoryName, LogLevel? minLevel)
    {
        ArgumentGuard.NotNullOrEmpty(categoryName);

        LoggerFilter? filter = minLevel != null ? level => level >= minLevel : null;

        if (categoryName == DefaultCategoryName)
        {
            // Update the default filter for new logger instances.
            _defaultFilter = filter ?? (logLevel => logLevel >= GetConfigurationMinLevel(DefaultCategoryName));
        }
        else
        {
            // Update the filter dictionary first, so that loggers can inherit changes when we reset.
            UpdateEffectiveFilters(categoryName, filter);

            // Update existing loggers under this category, or reset them to what they inherit.
            UpdateActiveLoggers(categoryName, filter);
        }
    }

    private void UpdateEffectiveFilters(string categoryName, LoggerFilter? filter)
    {
        if (filter != null && !_effectiveFiltersPerCategory.ContainsKey(categoryName))
        {
            _effectiveFiltersPerCategory.TryAdd(categoryName, filter);
        }

        foreach ((string nextName, LoggerFilter nextFilter) in _effectiveFiltersPerCategory.Where(pair => IsCategoryOrDescendant(pair.Key, categoryName)))
        {
            if (filter != null)
            {
                _effectiveFiltersPerCategory.TryUpdate(nextName, filter, nextFilter);
            }
            else
            {
                _effectiveFiltersPerCategory.TryRemove(nextName, out _);
            }
        }
    }

    private void UpdateActiveLoggers(string categoryName, LoggerFilter? filter)
    {
        LoggerFilter newFilter = filter ?? GetFilter(categoryName);

        foreach ((_, MessageProcessingLogger logger) in _activeLoggersPerCategory.Where(pair => IsCategoryOrDescendant(pair.Key, categoryName)))
        {
            logger.ChangeFilter(newFilter);
        }
    }

    private static bool IsCategoryOrDescendant(string fullCategoryName, string baseCategoryName)
    {
        // Category names are case-sensitive.

        if (fullCategoryName.Length == baseCategoryName.Length)
        {
            return fullCategoryName == baseCategoryName;
        }

        if (fullCategoryName.Length > baseCategoryName.Length)
        {
            return fullCategoryName.StartsWith(baseCategoryName + ".", StringComparison.Ordinal);
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
            _effectiveFiltersPerCategory.Clear();
            _activeLoggersPerCategory.Clear();
        }
    }

    protected virtual MessageProcessingLogger CreateMessageProcessingLogger(string categoryName)
    {
        ArgumentGuard.NotNullOrEmpty(categoryName);

        ILogger logger = InnerLoggerProvider.CreateLogger(categoryName);
        LoggerFilter filter = GetFilter(categoryName);

        return new MessageProcessingLogger(logger, filter, MessageProcessors);
    }

    protected LoggerFilter GetFilter(string categoryName)
    {
        ArgumentGuard.NotNullOrEmpty(categoryName);

        foreach (string name in GetCategoryNameWithParents(categoryName))
        {
            if (_effectiveFiltersPerCategory.TryGetValue(name, out LoggerFilter? filter))
            {
                return filter;
            }
        }

        return _defaultFilter;
    }

    private LogLevel? GetEffectiveMinLevel(string categoryName)
    {
        foreach (string name in GetCategoryNameWithParents(categoryName))
        {
            if (_effectiveFiltersPerCategory.TryGetValue(name, out LoggerFilter? filter))
            {
                return GetMinLevelFromFilter(filter);
            }
        }

        return null;
    }

    private static LogLevel GetMinLevelFromFilter(LoggerFilter filter)
    {
        foreach (LogLevel level in Enum.GetValues<LogLevel>())
        {
            if (filter.Invoke(level))
            {
                return level;
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
                yield return DefaultCategoryName;
                break;
            }

            categoryName = categoryName[..lastIndexOfDot];
        }
    }

    private LogLevel? GetConfigurationMinLevel(string name)
    {
        return _configurationMinLevels.TryGetValue(name, out LogLevel level) ? level : null;
    }
}
