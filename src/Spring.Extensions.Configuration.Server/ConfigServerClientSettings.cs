//
// Copyright 2015 the original author or authors.
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
//

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Spring.Extensions.Configuration.Server
{
    /// <summary>
    /// Holds the settings used to configure the Spring Cloud Config Server provider 
    /// <see cref="ConfigServerConfigurationProvider"/>.
    /// </summary>
    public class ConfigServerClientSettings
    {
        /// <summary>
        /// The prefix (<see cref="IConfigurationSection"/> under which all Spring Cloud Config Server 
        /// configuration settings are found. (e.g. spring:cloud:config:uri, spring:cloud:config:enabled, etc.)
        /// </summary>
        public const string PREFIX = "spring:cloud:config";

        /// <summary>
        /// The Config Servers address (defaults: https://localhost:8888)
        /// </summary>
        public string Uri { get; set; } = "http://localhost:8888";

        /// <summary>
        /// Enables/Disables the Config Server provider (defaults: true)
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The environment used when accessing configuration data (defaults: Development)
        /// </summary>
        public string Environment { get; set; } = "Development";

        /// <summary>
        /// The application name used when accessing configuration data (defaults: tbd)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The label used when accessing configuration data (defaults: null)
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The username used when accessing the Config Server (defaults: null)
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password used when accessing the Config Server (defaults: null)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Enables/Disables failfast behavior (defaults: false)
        /// </summary>
        public bool FailFast { get; set; } = false;

        private ILogger _logger;

        /// <summary>
        /// Initialize Config Server client settings with defaults
        /// </summary>
        public ConfigServerClientSettings(ILoggerFactory logFactory = null)
        {
            _logger = logFactory?.CreateLogger<ConfigServerClientSettings>();
        }

        internal ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        internal ConfigServerClientSettings(IEnumerable<IConfigurationProvider> providers, ILoggerFactory logFactory = null)
        {

            _logger = logFactory?.CreateLogger<ConfigServerClientSettings>();
            if (providers == null)
                return;

            Initialize(new List<IConfigurationProvider>(providers));
        }

        private void Initialize(List<IConfigurationProvider> list)
        {
            ConfigurationRoot root = new ConfigurationRoot(list);

            var section = root.GetSection(PREFIX);
            Name = section["name"];
            Label = section["label"];
            Username = section["username"];
            Password = section["password"];

            var env = section["environment"];
            if (!string.IsNullOrEmpty(env))
            {
                Environment = env;
            }

            var uri = section["uri"];
            if (!string.IsNullOrEmpty(uri))
            {
                Uri = uri;
            }

            var enabled = section["enabled"];
            if (!string.IsNullOrEmpty(enabled))
            {
                bool result;
                if (Boolean.TryParse(enabled, out result))
                    Enabled = result;
            }

            var failFast = section["failFast"];
            if (!string.IsNullOrEmpty(failFast))
            {
                bool result;
                if (Boolean.TryParse(failFast, out result))
                    FailFast = result;
            }

        }
    }
}

