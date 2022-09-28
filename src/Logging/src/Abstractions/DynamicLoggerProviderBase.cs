// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Filter = System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool>;

namespace Steeltoe.Extensions.Logging;

public class DynamicLoggerProviderBase : IDynamicLoggerProvider
{
    private static readonly Filter FalseFilter = (_, _) => false;

    private readonly ConcurrentDictionary<string, LogLevel> _originalLevels;
    private readonly ConcurrentDictionary<string, Filter> _runningFilters;
    private protected readonly IEnumerable<IDynamicMessageProcessor> MessageProcessors;

    private Filter _filter;
    private ConcurrentDictionary<string, MessageProcessingLogger> _loggers = new();

    private protected ILoggerProvider DelegateProvider { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicLoggerProviderBase" /> class. Contains base functionality for DynamicLoggerProvider.
    /// </summary>
    /// <param name="getDelegateLogger">
    /// Function to a delegate Logger.
    /// </param>
    /// <param name="initialLevels">
    /// Set the initial filter levels.
    /// </param>
    /// <param name="messageProcessors">
    /// Any <see cref="IDynamicMessageProcessor" /> Message processors.
    /// </param>
    public DynamicLoggerProviderBase(Func<ILoggerProvider> getDelegateLogger, InitialLevels initialLevels,
        IEnumerable<IDynamicMessageProcessor> messageProcessors)
    {
        if (initialLevels.OriginalLevels == null)
        {
            throw new ArgumentException($"{nameof(initialLevels.OriginalLevels)} in {nameof(initialLevels)} must not be null.", nameof(initialLevels));
        }

        if (initialLevels.RunningLevelFilters == null)
        {
            throw new ArgumentException($"{nameof(initialLevels.RunningLevelFilters)} in {nameof(initialLevels)} must not be null.", nameof(initialLevels));
        }

        DelegateProvider = getDelegateLogger?.Invoke();

        if (DelegateProvider == null)
        {
            throw new ArgumentException($"Callback for {nameof(ILoggerProvider)} must not return null.", nameof(getDelegateLogger));
        }

        _originalLevels = new ConcurrentDictionary<string, LogLevel>(initialLevels.OriginalLevels);
        _runningFilters = new ConcurrentDictionary<string, Filter>(initialLevels.RunningLevelFilters);
        MessageProcessors = messageProcessors;
        _filter = initialLevels.DefaultLevelFilter ?? FalseFilter;
    }

    /// <summary>
    /// Create or retrieve an instance of an ILogger.
    /// </summary>
    /// <param name="categoryName">
    /// Class name that will be using the logger.
    /// </param>
    /// <returns>
    /// A logger with level filtering for a given class.
    /// </returns>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, CreateLoggerImplementation(categoryName));
    }

    /// <summary>
    /// Get a list of logger configurations.
    /// </summary>
    /// <returns>
    /// Namespaces and loggers with minimum log levels.
    /// </returns>
    public ICollection<ILoggerConfiguration> GetLoggerConfigurations()
    {
        var results = new Dictionary<string, ILoggerConfiguration>();

        // get the default first
        LogLevel configuredDefault = GetConfiguredLevel("Default") ?? LogLevel.None;
        LogLevel effectiveDefault = GetLogLevelFromFilter("Default", _filter);
        results.Add("Default", new DynamicLoggerConfiguration("Default", configuredDefault, effectiveDefault));

        // then get all running loggers
        foreach (KeyValuePair<string, MessageProcessingLogger> logger in _loggers)
        {
            foreach (string prefix in GetKeyPrefixes(logger.Value.Name))
            {
                if (prefix != "Default")
                {
                    string name = prefix;
                    LogLevel? configured = GetConfiguredLevel(name);
                    LogLevel effective = GetEffectiveLevel(name);
                    var configuration = new DynamicLoggerConfiguration(name, configured, effective);

                    if (results.ContainsKey(name) && !results[name].Equals(configuration))
                    {
                        throw new InvalidProgramException("Shouldn't happen");
                    }

                    results[name] = configuration;
                }
            }
        }

        return results.Values;
    }

    /// <summary>
    /// Sets minimum log level for a given category and its descendents - resets to configured value if level is null.
    /// </summary>
    /// <param name="category">
    /// Namespace/qualified class name.
    /// </param>
    /// <param name="level">
    /// Minimum level to log, pass null to reset.
    /// </param>
    public void SetLogLevel(string category, LogLevel? level)
    {
        Filter filter = null;

        if (level != null)
        {
            filter = (_, lvl) => lvl >= level;
        }

        // update the default filter for new instances
        if (category == "Default")
        {
            _filter = filter ?? ((_, lvl) => lvl >= GetConfiguredLevel("Default"));
        }
        else
        {
            // if setting filter level on a namespace (not actual logger) that hasn't previously been configured
            if (!_runningFilters.Any(entry => entry.Key.Equals(category)) && filter != null)
            {
                _runningFilters.TryAdd(category, filter);
            }

            // update the filter dictionary first so that loggers can inherit changes when we reset
            if (_runningFilters.Any(entry => entry.Key.StartsWith(category)))
            {
                foreach (KeyValuePair<string, Filter> runningFilter in _runningFilters.Where(entry => entry.Key.StartsWith(category)))
                {
                    if (filter != null)
                    {
                        _runningFilters.TryUpdate(runningFilter.Key, filter, runningFilter.Value);
                    }
                    else
                    {
                        _runningFilters.TryRemove(runningFilter.Key, out _);
                    }
                }
            }
            else
            {
                if (filter != null)
                {
                    _runningFilters.TryAdd(category, filter);
                }
            }

            // update existing loggers under this category, or reset them to what they inherit
            foreach (KeyValuePair<string, MessageProcessingLogger> l in _loggers.Where(s => s.Key.StartsWith(category)))
            {
                l.Value.Filter = filter ?? GetFilter(category);
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            DelegateProvider?.Dispose();
            DelegateProvider = null;

            _loggers = null;
        }
    }

    private protected virtual MessageProcessingLogger CreateLoggerImplementation(string name)
    {
        ILogger logger = DelegateProvider.CreateLogger(name);

        return new MessageProcessingLogger(logger, MessageProcessors)
        {
            Filter = GetFilter(name),
            Name = name
        };
    }

    /// <summary>
    /// Get or create the most applicable logging filter.
    /// </summary>
    /// <param name="name">
    /// Fully qualified logger name.
    /// </param>
    /// <returns>
    /// A filter function for log level.
    /// </returns>
    private protected Filter GetFilter(string name)
    {
        // check if there are any applicable filters
        if (_runningFilters.Any())
        {
            IEnumerable<string> prefixes = GetKeyPrefixes(name);

            foreach (string prefix in prefixes)
            {
                if (_runningFilters.ContainsKey(prefix))
                {
                    return _runningFilters.First(f => f.Key == prefix).Value;
                }
            }
        }

        if (_filter != null)
        {
            return _filter;
        }

        return FalseFilter;
    }

    /// <summary>
    /// Determine the level a logger is set to log at right now.
    /// </summary>
    /// <param name="name">
    /// Namespace/qualified class name.
    /// </param>
    /// <returns>
    /// Minimum logging level.
    /// </returns>
    private LogLevel GetEffectiveLevel(string name)
    {
        IEnumerable<string> prefixes = GetKeyPrefixes(name);

        // check the dictionary
        foreach (string prefix in prefixes)
        {
            if (_runningFilters.Any(n => n.Key.Equals(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                Filter filter = _runningFilters.First(n => n.Key.Equals(prefix, StringComparison.OrdinalIgnoreCase)).Value;
                return GetLogLevelFromFilter(name, filter);
            }
        }

        return LogLevel.None;
    }

    /// <summary>
    /// Converts a filter function to a LogLevel.
    /// </summary>
    /// <param name="category">
    /// Namespace/class to check.
    /// </param>
    /// <param name="filter">
    /// Function to evaluate.
    /// </param>
    /// <returns>
    /// Minimum log level to be logged by this category of logger.
    /// </returns>
    private LogLevel GetLogLevelFromFilter(string category, Filter filter)
    {
        for (int i = 0; i < 6; i++)
        {
            var level = (LogLevel)Enum.ToObject(typeof(LogLevel), i);

            if (filter.Invoke(category, level))
            {
                return level;
            }
        }

        return LogLevel.None;
    }

    /// <summary>
    /// Get parent namespaces.
    /// </summary>
    /// <param name="name">
    /// Fully namespaced class name.
    /// </param>
    /// <returns>
    /// List of parental namespaces.
    /// </returns>
    private IEnumerable<string> GetKeyPrefixes(string name)
    {
        while (!string.IsNullOrEmpty(name))
        {
            yield return name;
            int lastIndexOfDot = name.LastIndexOf('.');

            if (lastIndexOfDot == -1)
            {
                yield return "Default";
                break;
            }

            name = name.Substring(0, lastIndexOfDot);
        }
    }

    /// <summary>
    /// Get the log level that was configured when the app started.
    /// </summary>
    /// <param name="name">
    /// Namespace/qualified class name.
    /// </param>
    /// <returns>
    /// Log level from default filter, value from settings or else null.
    /// </returns>
    private LogLevel? GetConfiguredLevel(string name)
    {
        if (_originalLevels != null && _originalLevels.TryGetValue(name, out LogLevel level))
        {
            return level;
        }

        return null;
    }
}
