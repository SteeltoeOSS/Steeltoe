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
using Newtonsoft.Json;
using Steeltoe.Common;
using Steeltoe.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public class LoggersEndpoint : AbstractEndpoint<Dictionary<string, object>, LoggersChangeRequest>
    {
        private static readonly List<string> Levels = new List<string>()
        {
            LoggerLevels.MapLogLevel(LogLevel.None),
            LoggerLevels.MapLogLevel(LogLevel.Critical),
            LoggerLevels.MapLogLevel(LogLevel.Error),
            LoggerLevels.MapLogLevel(LogLevel.Warning),
            LoggerLevels.MapLogLevel(LogLevel.Information),
            LoggerLevels.MapLogLevel(LogLevel.Debug),
            LoggerLevels.MapLogLevel(LogLevel.Trace)
        };

        private ILogger<LoggersEndpoint> _logger;
        private IDynamicLoggerProvider _cloudFoundryLoggerProvider;

        public LoggersEndpoint(ILoggersOptions options, IDynamicLoggerProvider cloudFoundryLoggerProvider, ILogger<LoggersEndpoint> logger = null)
            : base(options)
        {
            _cloudFoundryLoggerProvider = cloudFoundryLoggerProvider;
            _logger = logger;
        }

        protected new ILoggersOptions Options
        {
            get
            {
                return options as ILoggersOptions;
            }
        }

        public override Dictionary<string, object> Invoke(LoggersChangeRequest request)
        {
            _logger?.LogDebug("Invoke({0})", SecurityUtilities.SanitizeInput(request?.ToString()));

            return DoInvoke(_cloudFoundryLoggerProvider, request);
        }

        public virtual Dictionary<string, object> DoInvoke(IDynamicLoggerProvider provider, LoggersChangeRequest request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            if (request != null)
            {
                SetLogLevel(provider, request.Name, request.Level);
            }
            else
            {
                AddLevels(result);
                var configuration = GetLoggerConfigurations(provider);
                Dictionary<string, LoggerLevels> loggers = new Dictionary<string, LoggerLevels>();
                foreach (var c in configuration.OrderBy(entry => entry.Name))
                {
                    _logger.LogTrace("Adding " + c.ToString());
                    LoggerLevels lv = new LoggerLevels(c.ConfiguredLevel, c.EffectiveLevel);
                    loggers.Add(c.Name, lv);
                }

                result.Add("loggers", loggers);
            }

            return result;
        }

        public virtual void AddLevels(Dictionary<string, object> result)
        {
            result.Add("levels", Levels);
        }

        public virtual ICollection<ILoggerConfiguration> GetLoggerConfigurations(IDynamicLoggerProvider provider)
        {
            if (provider == null)
            {
                _logger?.LogInformation("Unable to access Cloud Foundry Logging provider, log configuration unavailable");
                return new List<ILoggerConfiguration>();
            }

            return provider.GetLoggerConfigurations();
        }

        public virtual void SetLogLevel(IDynamicLoggerProvider provider, string name, string level)
        {
            if (provider == null)
            {
                _logger?.LogInformation("Unable to access Cloud Foundry Logging provider, log level not changed");
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }

            provider.SetLogLevel(name, LoggerLevels.MapLogLevel(level));
        }

        public Dictionary<string, string> DeserializeRequest(Stream stream)
        {
            try
            {
                var serializer = new JsonSerializer();

                using (var sr = new StreamReader(stream))
                {
                    using (var jsonTextReader = new JsonTextReader(sr))
                    {
                        return serializer.Deserialize<Dictionary<string, string>>(jsonTextReader);
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError("Exception deserializing LoggersEndpoint Request: {Exception}", e);
            }

            return new Dictionary<string, string>();
        }
    }
}
