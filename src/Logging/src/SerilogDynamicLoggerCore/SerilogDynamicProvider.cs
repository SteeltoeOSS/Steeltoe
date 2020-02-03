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
        private bool disposed = false;

        private IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogDynamicProvider"/> class.
        /// Any Serilog settings can be passed in the IConfiguration as needed.
        /// </summary>
        /// <param name="configuration">Serilog readable <see cref="IConfiguration"/></param>
        /// <param name="options">Subset of Serilog options managed by wrapper<see cref="ISerilogOptions"/></param>
        public SerilogDynamicProvider(IConfiguration configuration, ISerilogOptions options = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;

            _serilogOptions = options ?? new SerilogOptions(configuration);

            SetFiltersFromOptions();

            // Add a level switch that controls the "Default" level at the root
            _defaultLevel = _serilogOptions.MinimumLevel.Default;
            var levelSwitch = new LoggingLevelSwitch(_defaultLevel.Value);
            _loggerSwitches.GetOrAdd("Default", levelSwitch);

            // Add a global logger that will be the root of all other added loggers
            _globalLogger = new Serilog.LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogDynamicProvider"/> class.
        /// Any Serilog settings can be passed in the IConfiguration as needed.
        /// </summary>
        /// <param name="configuration">Serilog readable <see cref="IConfiguration"/></param>
        /// <param name="logger">Serilog logger<see cref="Serilog.Core.Logger"/></param>
        /// <param name="loggingLevelSwitch">Serilog global log level switch<see cref="Serilog.Core.LoggingLevelSwitch"/></param>
        /// <param name="options">Subset of Serilog options managed by wrapper<see cref="ISerilogOptions"/></param>
        public SerilogDynamicProvider(IConfiguration configuration, Logger logger, LoggingLevelSwitch loggingLevelSwitch, ISerilogOptions options = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;

            _serilogOptions = options ?? new SerilogOptions(configuration);

            SetFiltersFromOptions();

            // Add a level switch that controls the "Default" level at the root
            _defaultLevel = loggingLevelSwitch.MinimumLevel;
            _loggerSwitches.GetOrAdd("Default", loggingLevelSwitch);

            // Add a global logger that will be the root of all other added loggers
            _globalLogger = logger;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var eventLevel = GetLevel(categoryName);
            var levelSwitch = new LoggingLevelSwitch(eventLevel);
            _loggerSwitches.GetOrAdd(categoryName, levelSwitch);

            var seriloggerConf = new Serilog.LoggerConfiguration();
            if (_configuration != null && _configuration.GetSection("Serilog:Destructure").Exists())
            {
                var confBuilder = new ConfigurationBuilder();
                confBuilder.AddInMemoryCollection(_configuration.GetSection("Serilog")
                    .AsEnumerable().Where((kv) => kv.Key.StartsWith("Serilog:Destructure") || kv.Key.StartsWith("Serilog:Using")));
                seriloggerConf = seriloggerConf.ReadFrom.Configuration(confBuilder.Build());
            }

            var serilogger = seriloggerConf.MinimumLevel.ControlledBy(levelSwitch)
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
            if (!disposed)
            {
                if (disposing)
                {
                    // Cleanup
                    _globalLogger.Dispose();
                    _loggerSwitches = null;
                    _loggers = null;
                }

                disposed = true;
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
