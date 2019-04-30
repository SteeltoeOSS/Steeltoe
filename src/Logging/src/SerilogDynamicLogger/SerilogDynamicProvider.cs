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
    /// A wrapper for the <see cref="Serilog.Logger"/> to dynamically set log levels
    /// </summary>
    public class SerilogDynamicProvider : IDynamicLoggerProvider
    {
        private Logger _globalLogger;
        private ISerilogOptions _serilogOptions;

        private ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();
        private ConcurrentDictionary<string, LoggingLevelSwitch> _loggerSwitches = new ConcurrentDictionary<string, LoggingLevelSwitch>();

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

            _serilogOptions = options ?? new SerilogOptions(configuration);

            // Add a level switch that controls the "Default" level at the root
            var levelSwitch = new LoggingLevelSwitch(_serilogOptions.MinimumLevel.Default);
            _loggerSwitches.GetOrAdd("Default", levelSwitch);

            // Add a global logger that will be the root of all other added loggers
            _globalLogger = new Serilog.LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        public ILogger CreateLogger(string categoryName)
        {
            LogEventLevel eventLevel = _serilogOptions.MinimumLevel.Default;

            foreach (var overrideOption in _serilogOptions.MinimumLevel.Override)
            {
               if (categoryName.StartsWith(overrideOption.Key))
                {
                    eventLevel = overrideOption.Value;
                }
            }

            // Chain new loggers to the global loggers with its own switch
            // taking into accound any "Overrides"
            var levelSwitch = new LoggingLevelSwitch(eventLevel);
            _loggerSwitches.GetOrAdd(categoryName, levelSwitch);
            var serilogger = new Serilog.LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Logger(_globalLogger)
                .CreateLogger();
            var factory = new SerilogLoggerFactory(serilogger, true);

            return _loggers.GetOrAdd(categoryName, factory.CreateLogger(categoryName));
        }

        public void Dispose()
        {
            _globalLogger.Dispose();
            _loggerSwitches = null;
            _loggers = null;
        }

        public ICollection<ILoggerConfiguration> GetLoggerConfigurations()
        {
            var results = new Dictionary<string, ILoggerConfiguration>();

            // get the default first
            LogLevel configuredDefault = GetConfiguredLevel("Default") ?? LogLevel.None;
            LogLevel effectiveDefault = GetEffectiveLevel("Default");
            results.Add("Default", new LoggerConfiguration("Default", configuredDefault, effectiveDefault));

            // then get all running loggers
            foreach (var logger in _loggers)
            {
                foreach (var name in GetKeyPrefixes(logger.Key))
                {
                    if (name != "Default")
                    {
                        LogLevel? configured = GetConfiguredLevel(name);
                        LogLevel effective = GetEffectiveLevel(logger.Key);
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

        public void SetLogLevel(string category, LogLevel? level)
        {
            var filteredPairs = _loggerSwitches.Where(kvp => kvp.Key.StartsWith(category));
            foreach (var kvp in filteredPairs)
            {
                var currentLevel = level ?? GetConfiguredLevel(kvp.Key);
                if (currentLevel != null)
                {
                    kvp.Value.MinimumLevel = ToSerilogLevel(currentLevel);
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
            LoggingLevelSwitch levelSwitch;
            _loggerSwitches.TryGetValue(name, out levelSwitch);
            return (LogLevel)levelSwitch.MinimumLevel;
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