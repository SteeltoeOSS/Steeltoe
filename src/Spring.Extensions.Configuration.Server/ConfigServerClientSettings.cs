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
using Spring.Extensions.Configuration.Common;

namespace Spring.Extensions.Configuration.Server
{
    /// <summary>
    /// Holds the settings used to configure the Spring Cloud Config Server provider 
    /// <see cref="ConfigServerConfigurationProvider"/>.
    /// </summary>
    public class ConfigServerClientSettings : ConfigServerClientSettingsBase
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
        /// Default certifcate validation enabled setting
        /// </summary>
        public const bool DEFAULT_CERTIFICATE_VALIDATION = true;
        
        /// <summary>
        /// Initialize Config Server client settings with defaults
        /// </summary>
        public ConfigServerClientSettings() : base()
        {
            ValidateCertificates = DEFAULT_CERTIFICATE_VALIDATION;
            FailFast = DEFAULT_FAILFAST;
            Environment = DEFAULT_ENVIRONMENT;
            Enabled = DEFAULT_PROVIDER_ENABLED;
            Uri = DEFAULT_URI;
        }

    }
}



