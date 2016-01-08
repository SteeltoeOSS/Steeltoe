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
        /// Default Config Server address used by provider
        /// </summary>
        public const string DEFAULT_URI = "http://localhost:8888";

        /// <summary>
        /// Default enironment used when accessing configuration data
        /// </summary>
        public const string DEFAULT_ENVIRONMENT = "Production";

        /// <summary>
        /// Default fail fast setting
        /// </summary>
        public const bool DEFAULT_FAILFAST = false;

        /// <summary>
        /// Default Config Server provider enabled setting
        /// </summary>
        public const bool DEFAULT_PROVIDER_ENABLED = true;

        /// <summary>
        /// The Config Server address (defaults: DEFAULT_URI)
        /// </summary>
        public string Uri { get; set; } = DEFAULT_URI;

        /// <summary>
        /// Enables/Disables the Config Server provider (defaults: DEFAULT_PROVIDER_ENABLED)
        /// </summary>
        public bool Enabled { get; set; } = DEFAULT_PROVIDER_ENABLED;

        /// <summary>
        /// The environment used when accessing configuration data (defaults: DEFAULT_ENVIRONMENT)
        /// </summary>
        public string Environment { get; set; } = DEFAULT_ENVIRONMENT;

        /// <summary>
        /// The application name used when accessing configuration data (defaults: TODO:)
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
        /// Enables/Disables failfast behavior (defaults: DEFAULT_FAILFAST)
        /// </summary>
        public bool FailFast { get; set; } = DEFAULT_FAILFAST;

        /// <summary>
        /// Initialize Config Server client settings with defaults
        /// </summary>
        public ConfigServerClientSettings()
        {
        }

    }
}

