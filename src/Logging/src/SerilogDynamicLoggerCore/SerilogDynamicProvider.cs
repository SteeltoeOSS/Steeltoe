// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.AspNetCore;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static Serilog.ConfigurationLoggerConfigurationExtensions;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger
{
    /// <summary>
    /// A wrapper for the <see cref="Serilog.Core.Logger"/> to dynamically set log levels
    /// </summary>
    public class SerilogDynamicProvider : IDynamicLoggerProvider
    {
        private Logger _globalLogger;
        private ISerilogOptions _serilogOptions;

        private ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();
        private ConcurrentDictionary<string, LoggingLevelSwitch> _loggerSwitches = new ConcurrentDictionary<string, LoggingLevelSwitch>();
        private ConcurrentDictionary<string, LogEventLevel> _runningLevels = new ConcurrentDictionary<string, LogEventLevel>();
        private LogEventLevel? _defaultLevel = null;
        private bool _disposed = false;
        private IConfiguration _subLoggerConfiguration;

        [Obsolete("Will be removed in a future release; Use SerilogDynamicProvider(IConfiguration, ISerilogOptions, Logger, LoggingLevelSwitch) instead")]
        public SerilogDynamicProvider(IConfiguration configuration, Logger logger, LoggingLevelSwitch loggingLevelSwitch, ISerilogOptions options = null)
            : this(configuration, options, logger, loggingLevelSwitch)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogDynamicProvider"/> class.
        /// Any Serilog settings can be passed in the IConfiguration as needed.
        /// </summary>
        /// <param name="configuration">Serilog readable <see cref="IConfiguration"/></param>
        /// <param name="logger">Serilog logger<see cref="Serilog.Core.Logger"/></param>
        /// <param name="loggingLevelSwitch">Serilog global log level switch<see cref="Serilog.Core.LoggingLevelSwitch"/></param>
        /// <param name="options">Subset of Serilog options managed by wrapper<see cref="ISerilogOptions"/></param>
        public SerilogDynamicProvider(IConfiguration configuration, ISerilogOptions options = null, Logger logger = null, LoggingLevelSwitch loggingLevelSwitch = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _serilogOptions = options ?? new SerilogOptions(configuration);

            _subLoggerConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(configuration.GetSection(_serilogOptions.ConfigPath)
                .AsEnumerable().Where((kv) => !_serilogOptions.FullnameExclusions.Any(key => kv.Key.StartsWith(key))))
                .Build();

            SetFiltersFromOptions();

            // Add a level switch that controls the "Default" level at the root
            if (loggingLevelSwitch == null)
            {
                _defaultLevel = _serilogOptions.MinimumLevel.Default;
                loggingLevelSwitch = new LoggingLevelSwitch(_defaultLevel.Value);
            }
            else
            {
                _defaultLevel = loggingLevelSwitch.MinimumLevel;
            }

            _loggerSwitches.GetOrAdd("Default", loggingLevelSwitch);

            // Add a global logger that will be the root of all other added loggers
            _globalLogger = logger ?? new Serilog.LoggerConfiguration()
                .MinimumLevel.ControlledBy(loggingLevelSwitch)
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        public ILogger CreateLogger(string categoryName)
        {
            var eventLevel = GetLevel(categoryName);
            var levelSwitch = new LoggingLevelSwitch(eventLevel);
            _loggerSwitches.GetOrAdd(categoryName, levelSwitch);

            var serilogger = new Serilog.LoggerConfiguration()
                .ReadFrom.Configuration(_subLoggerConfiguration)
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Logger(_globalLogger)
                .CreateLogger();
            var factory = new SerilogLoggerFactory(serilogger, true);
            return _loggers.GetOrAdd(categoryName, factory.CreateLogger(categoryName));
        }

        public ICollection<ILoggerConfiguration> GetLoggerConfigurations()
        {
            var results = new Dictionary<string, ILoggerConfiguration>();

            // get the default first
            LogLevel configuredDefault = GetConfiguredLevel("Default") ?? LogLevel.None;
            LogLevel effectiveDefault = GetEffectiveLevel("Default");
            results.Add("Default", new DynamicLoggerConfiguration("Default", configuredDefault, effectiveDefault));

            // then get all running loggers
            foreach (var logger in _loggers)
            {
                foreach (var name in GetKeyPrefixes(logger.Key))
                {
                    if (name != "Default")
                    {
                        LogLevel? configured = GetConfiguredLevel(name);
                        LogLevel effective = GetEffectiveLevel(name);
                        var config = new DynamicLoggerConfiguration(name, configured, effective);
                        if (results.ContainsKey(name) && !results[name].Equals(config))
                        {
                            Console.WriteLine(
                                $"Attempted to add duplicate Key {name} with value {config} clashes with {results[name]}");
                        }
                        else
                        {
                            results[name] = config;
                        }
                    }
                }
            }

            return results.Values;
        }

        public void SetLogLevel(string category, LogLevel? level)
        {
            var defaultLevel = _defaultLevel ?? _serilogOptions.MinimumLevel.Default;
            var serilogLevel = ToSerilogLevel(level ?? GetConfiguredLevel(category) ?? (LogLevel)defaultLevel);

            if (category == "Default")
            {
                if (level.HasValue)
                {
                    _defaultLevel = serilogLevel;
                }

                if (_loggerSwitches.TryGetValue(category, out var levelSwitch))
                {
                    levelSwitch.MinimumLevel = serilogLevel;
                }
            }
            else
            {
                // update the filter dictionary first so that loggers can inherit changes when we reset
                if (_runningLevels.Any(entry => entry.Key.StartsWith(category)))
                {
                    foreach (var runningSwitch in _runningLevels.Where(entry => entry.Key.StartsWith(category)))
                    {
                        if (level.HasValue)
                        {
                            _runningLevels.TryUpdate(runningSwitch.Key, serilogLevel, runningSwitch.Value);
                        }
                        else
                        {
                            _runningLevels.TryRemove(runningSwitch.Key, out _);
                        }
                    }
                }

                // if setting filter level on a namespace (not actual logger) that hasn't previously been configured
                if (!_runningLevels.Any(entry => entry.Key.Equals(category)) && level.HasValue)
                {
                    _runningLevels.TryAdd(category, serilogLevel);
                }

                // update existing loggers under this category, or reset them to what they inherit
                foreach (var l in _loggerSwitches.Where(s => s.Key.StartsWith(category)))
                {
                    l.Value.MinimumLevel = serilogLevel;
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
            if (!_disposed)
            {
                if (disposing)
                {
                    // Cleanup
                    _globalLogger.Dispose();
                    _loggerSwitches = null;
                    _loggers = null;
                }

                _disposed = true;
            }
        }

        ~SerilogDynamicProvider()
        {
            Dispose(false);
        }

        private LogEventLevel GetLevel(string name)
        {
            var prefixes = GetKeyPrefixes(name);
            LogEventLevel eventLevel = _serilogOptions.MinimumLevel.Default;

            if (_defaultLevel.HasValue)
            {
                eventLevel = _defaultLevel.Value;
            }

            // check if there are any applicable filters
            if (_runningLevels.Any())
            {
                foreach (var prefix in prefixes)
                {
                    if (_runningLevels.ContainsKey(prefix))
                    {
                        return _runningLevels.First(f => f.Key == prefix).Value;
                    }
                }
            }

            // check if there are any applicable settings
            foreach (var overrideOption in _serilogOptions.MinimumLevel.Override)
            {
                if (name.StartsWith(overrideOption.Key))
                {
                    return overrideOption.Value;
                }
            }

            return eventLevel;
        }

        private void SetFiltersFromOptions()
        {
            if (_serilogOptions != null && _serilogOptions.MinimumLevel != null)
            {
                foreach (var overrideLevel in _serilogOptions.MinimumLevel.Override)
                {
                    _runningLevels.TryAdd(overrideLevel.Key, overrideLevel.Value);
                }
            }
        }

        private LogLevel? GetConfiguredLevel(string name)
        {
            LogLevel? returnValue = null;
            if (name == "Default")
            {
                returnValue = (LogLevel)_serilogOptions.MinimumLevel.Default;
            }
            else
            {
                var overrides = _serilogOptions.MinimumLevel.Override;
                if (overrides != null
                    && overrides.ContainsKey(name)
                    && overrides.TryGetValue(name, out LogEventLevel configuredLevel))
                {
                    returnValue = (LogLevel)configuredLevel;
                }
            }

            return returnValue;
        }

        private LogLevel GetEffectiveLevel(string name)
        {
            var prefixes = GetKeyPrefixes(name);

            foreach (var prefix in prefixes)
            {
                if (_loggerSwitches.TryGetValue(prefix, out LoggingLevelSwitch levelSwitch))
                {
                    return (LogLevel)levelSwitch.MinimumLevel;
                }

                if (_runningLevels.TryGetValue(prefix, out LogEventLevel level))
                {
                    return (LogLevel)level;
                }
            }

            return LogLevel.None;
        }

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

        private LogEventLevel ToSerilogLevel(LogLevel? level)
        {
            if (level == null || level == LogLevel.None)
            {
                return LogEventLevel.Fatal;
            }

            return (LogEventLevel)level;
        }
    }
}
