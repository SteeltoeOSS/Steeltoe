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
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNet.Hosting;

namespace Spring.Extensions.Configuration.Server
{
    /// <summary>
    /// A Spring Cloud Config Server based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class ConfigServerConfigurationProvider : ConfigurationProvider
    {

        private static readonly TimeSpan DEFAULT_TIMEOUT = new TimeSpan(0,0,5);
        private ConfigServerClientSettings _settings;
        private HttpClient _client;
        private ILogger _logger;
        

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigServerConfigurationProvider"/> with default
        /// configuration settings. <see cref="ConfigServerClientSettings"/>
        /// <param name="logFactory">optional logging factory</param>
        /// </summary>
        public ConfigServerConfigurationProvider(ILoggerFactory logFactory = null) :
            this(new ConfigServerClientSettings(), logFactory)
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
            this(settings, new HttpClient(), logFactory)
        {
            _client.Timeout = DEFAULT_TIMEOUT;
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
        public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, HttpClient httpClient, ILoggerFactory logFactory = null)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            _logger = logFactory?.CreateLogger<ConfigServerConfigurationProvider>();
            _settings = settings;
            _client = httpClient;
        }

        /// <summary>
        /// The configuration settings the provider uses when accessing the server.
        /// </summary>
        public ConfigServerClientSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        /// <summary>
        /// Loads configuration data from the Spring Cloud Configuration Server as specified by
        /// the <see cref="Settings"/> 
        /// </summary>
        public override void Load()
        {
            // Adds client settings (e.g spring:cloud:config:uri) to the Data dictionary
            AddConfigServerClientSettings(_settings);

            var path = GetConfigServerUri();
            Task<Environment> task = RemoteLoadAsync(path);
            task.Wait();
            Environment env = task.Result;
            if (env != null)
            {
                _logger?.LogInformation("Located environment: {0}, {1}, {2}, {3}", env.Name, env.Profiles, env.Label, env.Version);
                var sources = env.PropertySources;
                if (sources != null)
                {

                    foreach (PropertySource source in sources)
                    {
                        AddPropertySource(source);
                    }
                }
            }
        }


        internal IDictionary<string, string> Properties
        {
            get
            {
                return Data;
            }
        }

        internal ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        internal async Task<Environment> RemoteLoadAsync(string path)
        {
            try {
                using (HttpResponseMessage response = await _client.GetAsync(path))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        _logger?.LogInformation("Config Server returned status: {0} invoking path: {1}", response.StatusCode, path);
                        return null;
                    }

                    Stream stream = await response.Content.ReadAsStreamAsync();
                    return Deserialize(stream);
                }
            } catch (Exception e)
            {
                _logger?.LogError("Config Server exception, path: {0}", path, e);
            }
            return null;

        }

        internal Environment Deserialize(Stream stream)
        {
            try {
                using (JsonReader reader = new JsonTextReader(new StreamReader(stream)))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    return (Environment)serializer.Deserialize(reader, typeof(Environment));
                }
            } catch (Exception e)
            {
                _logger?.LogError("Config Server serialization exception", e);
            }
            return null;
        }

        internal string GetConfigServerUri()
        {
            var path = "/" + _settings.Name + "/" + _settings.Environment;
            if (!string.IsNullOrWhiteSpace(_settings.Label))
                path = path + "/" + _settings.Label;

            return _settings.Uri + path;
        }

        internal void AddPropertySource(PropertySource source)
        {
            if (source == null || source.Source == null)
                return;
    
            foreach(KeyValuePair<string,object> kvp in source.Source)
            {
                try {
                    string key = kvp.Key.Replace(".", Constants.KeyDelimiter);
                    string value = kvp.Value.ToString();
                    Data[key] = value;
                } catch (Exception e)
                {
                    _logger?.LogError("Config Server exception, property: {0}={1}", kvp.Key, kvp.Value.GetType(), e);
                }

            }
        }

        internal void AddConfigServerClientSettings(ConfigServerClientSettings settings)
        {
            Data["spring:cloud:config:enabled"] = settings.Enabled.ToString();
            Data["spring:cloud:config:failFast"] = settings.FailFast.ToString();
            Data["spring:cloud:config:env"] = settings.Environment;
            Data["spring:cloud:config:label"] = settings.Label;
            Data["spring:cloud:config:name"] = settings.Name;
            Data["spring:cloud:config:password"] = settings.Password;
            Data["spring:cloud:config:uri"] = settings.Uri;
            Data["spring:cloud:config:username"] = settings.Username;
            Data["spring:cloud:config:access_token_uri"] = settings.AccessTokenUri;
            Data["spring:cloud:config:client_secret"] = settings.ClientSecret;
            Data["spring:cloud:config:client_id"] = settings.ClientId;
        }
    }
}
