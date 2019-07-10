// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Extensions.Logging
{
    [ProviderAlias("Dynamic")]
    public class DynamicLoggerProvider : IDynamicLoggerProvider
    {
        // private static readonly Func<string, LogLevel, bool> _trueFilter = (cat, level) => true;
        private static readonly Func<string, LogLevel, bool> _falseFilter = (cat, level) => false;
        private readonly ConcurrentDictionary<string, Func<string, LogLevel, bool>> _runningFilters = new ConcurrentDictionary<string, Func<string, LogLevel, bool>>();

        private readonly IOptionsMonitor<LoggerFilterOptions> _filterOptions;
        private readonly IEnumerable<IDynamicMessageProcessor> _messageProcessors;

        private Func<string, LogLevel, bool> _filter = _falseFilter;
        private ConcurrentDictionary<string, DynamicConsoleLogger> _loggers = new ConcurrentDictionary<string, DynamicConsoleLogger>();
        private ConsoleLoggerProvider _delegate;
        private IConsoleLoggerSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicLoggerProvider"/> class.
        /// </summary>
        /// <param name="settings">Logging Settings</param>
        public DynamicLoggerProvider(IConsoleLoggerSettings settings)
        {
            _delegate = new ConsoleLoggerProvider(settings);
            _settings = settings;
            SetFiltersFromSettings();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicLoggerProvider"/> class.
        /// </summary>
        /// <param name="filter">Default log level filter</param>
        /// <param name="includeScopes">Enable log scoping</param>
        public DynamicLoggerProvider(Func<string, LogLevel, bool> filter, bool includeScopes)
        {
            _delegate = new ConsoleLoggerProvider(filter, includeScopes);
            _filter = filter ?? _falseFilter;
            _settings = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicLoggerProvider"/> class.
        /// </summary>
        /// <param name="options">Pass-through to ConsoleLoggerProvider constructor</param>
        /// <param name="filterOptions">Logger filters</param>
        /// <param name="messageProcessors">message processors to apply to message</param>
        public DynamicLoggerProvider(IOptionsMonitor<ConsoleLoggerOptions> options, IOptionsMonitor<LoggerFilterOptions> filterOptions, IEnumerable<IDynamicMessageProcessor> messageProcessors = null)
        {
            _filterOptions = filterOptions;
            SetFiltersFromOptions();
            _delegate = new ConsoleLoggerProvider(options);
            _messageProcessors = messageProcessors;
        }

        /// <summary>
        /// Create or retrieve an instance of an ILogger
        /// </summary>
        /// <param name="name">Class name that will be using the logger</param>
        /// <returns>A logger with level filtering for a given class</returns>
        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, CreateLoggerImplementation);
        }

        public void Dispose()
        {
            _delegate?.Dispose();
            _delegate = null;
            _settings = null;
            _loggers = null;
        }

        /// <summary>
        /// Get a list of logger configurations
        /// </summary>
        /// <returns>Namespaces and loggers with minimum log levels</returns>
        public ICollection<ILoggerConfiguration> GetLoggerConfigurations()
        {
            Dictionary<string, ILoggerConfiguration> results = new Dictionary<string, ILoggerConfiguration>();

            // get the default first
            LogLevel configuredDefault = GetConfiguredLevel("Default") ?? LogLevel.None;
            LogLevel effectiveDefault = GetLogLevelFromFilter("Default", _filter);
            results.Add("Default", new LoggerConfiguration("Default", configuredDefault, effectiveDefault));

            // then get all running loggers
            foreach (var logger in _loggers)
            {
                foreach (var prefix in GetKeyPrefixes(logger.Value.Name))
                {
                    if (prefix != "Default")
                    {
                        var name = prefix;
                        LogLevel? configured = GetConfiguredLevel(name);
                        LogLevel effective = GetEffectiveLevel(name);
                        var config = new LoggerConfiguration(name, configured, effective);
                        if (results.ContainsKey(name))
                        {
                            if (!results[name].Equals(config))
                            {
                                throw new InvalidProgramException("Shouldn't happen");
                            }
                        }

                        results[name] = config;
                    }
                }
            }

            return results.Values;
        }

        /// <summary>
        /// Sets minimum log level for a given category and its decendants - resets to configured value if level is null
        /// </summary>
        /// <param name="category">Namespace/qualified class name</param>
        /// <param name="level">Minimum level to log, pass null to reset</param>
        public void SetLogLevel(string category, LogLevel? level)
        {
            Func<string, LogLevel, bool> filter = null;
            if (level != null)
            {
                filter = (cat, lvl) => lvl >= level;
            }

            // update the default filter for new instances
            if (category == "Default")
            {
                _filter = filter ?? ((cat, lvl) => lvl >= GetConfiguredLevel("Default"));
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
                    foreach (var runningFilter in _runningFilters.Where(entry => entry.Key.StartsWith(category)))
                    {
                        if (filter != null)
                        {
                            _runningFilters.TryUpdate(runningFilter.Key, filter, runningFilter.Value);
                        }
                        else
                        {
                            _runningFilters.TryRemove(runningFilter.Key, out Func<string, LogLevel, bool> oldVal);
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
                foreach (var l in _loggers.Where(s => s.Key.StartsWith(category)))
                {
                    l.Value.Filter = filter ?? GetFilter(category);
                }
            }
        }

        private void SetFiltersFromOptions()
        {
            foreach (var rule in _filterOptions.CurrentValue.Rules.Where(p => string.IsNullOrEmpty(p.ProviderName) || p.ProviderName == "Console"))
            {
                if (rule.CategoryName == "Default" || string.IsNullOrEmpty(rule.CategoryName))
                {
                    _filter = (category, level) => level >= rule.LogLevel;
                }
                else
                {
                    _runningFilters.TryAdd(rule.CategoryName, (category, level) => level >= rule.LogLevel);
                }
            }
        }

        private void SetFiltersFromSettings()
        {
            foreach (var setting in (_settings as ConsoleLoggerSettings).Switches)
            {
                if (setting.Key == "Default")
                {
                    _filter = (category, level) => level >= setting.Value;
                }
                else
                {
                    _runningFilters.TryAdd(setting.Key, (category, level) => level >= setting.Value);
                }
            }

            if (_filter == null)
            {
                _filter = _falseFilter;
            }
        }

        private DynamicConsoleLogger CreateLoggerImplementation(string name)
        {
            var logger = _delegate.CreateLogger(name) as ConsoleLogger;
            logger.Filter = GetFilter(name);
            return new DynamicConsoleLogger(logger, _messageProcessors);
        }

        /// <summary>
        /// Get or create the most applicable logging filter
        /// </summary>
        /// <param name="name">Fully qualified logger name</param>
        /// <returns>A filter function for log level</returns>
        private Func<string, LogLevel, bool> GetFilter(string name)
        {
            var prefixes = GetKeyPrefixes(name);

            // check if there are any applicable filters
            if (_runningFilters.Any() || _filter != null)
            {
                foreach (var prefix in prefixes)
                {
                    if (_runningFilters.ContainsKey(prefix))
                    {
                        return _runningFilters.First(f => f.Key == prefix).Value;
                    }
                }

                return _filter;
            }

            // check if there are any applicable settings
            if (_settings != null)
            {
                foreach (var prefix in prefixes)
                {
                    if (_settings.TryGetSwitch(prefix, out LogLevel level))
                    {
                        return (n, l) => l >= level;
                    }
                }
            }

            return _falseFilter;
        }

        /// <summary>
        /// Determine the level a logger is set to log at right now
        /// </summary>
        /// <param name="name">Namespace/qualified class name</param>
        /// <returns>Minimum logging level</returns>
        private LogLevel GetEffectiveLevel(string name)
        {
            var prefixes = GetKeyPrefixes(name);

            // check the dictionary
            foreach (var prefix in prefixes)
            {
                if (_runningFilters.Any(n => n.Key.Equals(prefix, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var filter = _runningFilters.First(n => n.Key.Equals(prefix, StringComparison.InvariantCultureIgnoreCase)).Value;
                    return GetLogLevelFromFilter(name, filter);
                }
            }

            // fall back to settings
            if (_settings != null)
            {
                foreach (var prefix in prefixes)
                {
                    if (_settings.TryGetSwitch(prefix, out LogLevel level))
                    {
                        return level;
                    }
                }
            }

            return LogLevel.None;
        }

        /// <summary>
        /// Converts a filter function to a LogLevel
        /// </summary>
        /// <param name="category">Namespace/class to check</param>
        /// <param name="filter">Function to evaluate</param>
        /// <returns>Minimum log level to be logged by this category of logger</returns>
        private LogLevel GetLogLevelFromFilter(string category, Func<string, LogLevel, bool> filter)
        {
            for (var i = 0; i < 6; i++)
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
        /// Get parent namespaces
        /// </summary>
        /// <param name="name">Fully namespaced class name</param>
        /// <returns>List of parental namespaces</returns>
        private IEnumerable<string> GetKeyPrefixes(string name)
        {
            while (!string.IsNullOrEmpty(name))
            {
                yield return name;
                var lastIndexOfDot = name.LastIndexOf('.');
                if (lastIndexOfDot == -1)
                {
                    yield return "Default";
                    break;
                }

                name = name.Substring(0, lastIndexOfDot);
            }
        }

        /// <summary>
        /// Get the log level that was configured when the app started
        /// </summary>
        /// <param name="name">Namespace/qualified class name</param>
        /// <returns>Log level from default filter, value from settings or else null</returns>
        private LogLevel? GetConfiguredLevel(string name)
        {
            if (_settings != null)
            {
                if (_settings.TryGetSwitch(name, out LogLevel level))
                {
                    return level;
                }
            }

            return null;
        }
    }
}