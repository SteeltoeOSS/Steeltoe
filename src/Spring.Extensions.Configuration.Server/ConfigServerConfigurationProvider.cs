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


using System.Net.Http;
using Microsoft.Extensions.Logging;
using Spring.Extensions.Configuration.Common;

namespace Spring.Extensions.Configuration.Server
{
    /// <summary>
    /// A Spring Cloud Config Server based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class ConfigServerConfigurationProvider : ConfigServerConfigurationProviderBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ConfigServerConfigurationProvider"/> with default
        /// configuration settings. <see cref="ConfigServerClientSettings"/>
        /// <param name="logFactory">optional logging factory</param>
        /// </summary>
        public ConfigServerConfigurationProvider(ILoggerFactory logFactory = null) :
            base(new ConfigServerClientSettings(), logFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigServerConfigurationProvider"/>.
        /// </summary>
        /// <param name="settings">the configuration settings the provider uses when
        /// accessing the server.</param>
        /// <param name="logFactory">optional logging factory</param>
        /// </summary>
        public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, ILoggerFactory logFactory = null) :
            base(settings, logFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigServerConfigurationProvider"/>.
        /// </summary>
        /// <param name="settings">the configuration settings the provider uses when
        /// accessing the server.</param>
        /// <param name="httpClient">a HttpClient the provider uses to make requests of
        /// the server.</param>
        /// <param name="logFactory">optional logging factory</param>
        /// </summary>
        public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, HttpClient httpClient, ILoggerFactory logFactory = null) :
            base(settings, httpClient, logFactory)
        {
        }


        /// <summary>
        /// The configuration settings the provider uses when accessing the server.
        /// </summary>
        public virtual ConfigServerClientSettings Settings
        {
            get
            {
                return _settings as ConfigServerClientSettings;
            }
        }
    }
}
