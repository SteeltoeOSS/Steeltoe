//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging.Console;

namespace Steeltoe.Extensions.Logging.CloudFoundry
{

    public class CloudFoundryLoggerProvider :  ICloudFoundryLoggerProvider
    {

        private ConcurrentDictionary<string, ConsoleLogger> _loggers = new ConcurrentDictionary<string, ConsoleLogger>();

        private ConsoleLoggerProvider _delegate;

        private bool _filter = false;
        private ICloudFoundryLoggerSettings _settings;

        internal static CloudFoundryLoggerProvider _self;

        public static ICloudFoundryLoggerProvider Instance { get { return _self; } }

        internal static ILoggerProvider CreateSingleton(Func<string, LogLevel, bool> filter, bool includeScopes)
        {
            if (_self != null)
            {
                throw new InvalidOperationException("CloudFoundryLoggerProvider already created");
            }

            return _self = new CloudFoundryLoggerProvider(filter, includeScopes);
        }

        internal static ILoggerProvider CreateSingleton(ICloudFoundryLoggerSettings settings)
        {
            if (_self != null)
            {
                throw new InvalidOperationException("CloudFoundryLoggerProvider already created");
            }

            return _self = new CloudFoundryLoggerProvider(settings);
        }

        public CloudFoundryLoggerProvider(Func<string, LogLevel, bool> filter, bool includeScopes)
        {
            _delegate = new ConsoleLoggerProvider(filter, includeScopes);
            _filter = true;
            _settings = null;
        }

        public CloudFoundryLoggerProvider(ICloudFoundryLoggerSettings settings)
        {
            _delegate = new ConsoleLoggerProvider(settings);
            _settings = settings;
        }

        public ILogger CreateLogger(string name)
        {
            ConsoleLogger created = _delegate.CreateLogger(name) as ConsoleLogger;
            return _loggers.GetOrAdd(name, created);
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

        public void Dispose()
        {
            _delegate.Dispose();
            _delegate = null;
            _settings = null;
            _loggers = null;
            _self = null;
        }

        public ICollection<ILoggerConfiguration> GetLoggerConfigurations()
        {
            Dictionary<string, ILoggerConfiguration> results = new Dictionary<string, ILoggerConfiguration>();

            LogLevel configuredDefault = GetConfiguredLevel("Default") ?? LogLevel.None;
            LogLevel effictiveDefault = configuredDefault;

            results.Add("Default", new LoggerConfiguration("Default", configuredDefault, effictiveDefault));
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


        public void SetLogLevel(string category, LogLevel level)
        {
            if (!_filter)
            { 
                _settings.SetLogLevel(category, level);
            }

        }
        private LogLevel GetEffectiveLevel(string name)
        {
            if (_filter)
            {
                return LogLevel.None;
            }

            if (_settings != null)
            {
                foreach (var prefix in GetKeyPrefixes(name))
                {
                    LogLevel level;
                    if (_settings.TryGetSwitch(prefix, out level))
                    {
                        return level;
                    }
                }
            }

            return LogLevel.None;
        }
        private LogLevel? GetConfiguredLevel(string name)
        {
            if (_filter)
            {
                return null;
            }

            if (_settings != null)
            {
                    LogLevel level;
                if (_settings.TryGetSwitch(name, out level))
                {
                    return level;
                }
                
            }

            return null;
        }
    }
}

