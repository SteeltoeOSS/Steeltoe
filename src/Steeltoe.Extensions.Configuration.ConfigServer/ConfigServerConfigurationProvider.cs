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
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;

using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Threading;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    /// <summary>
    /// A Spring Cloud Config Server based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class ConfigServerConfigurationProvider : ConfigurationProvider, IConfigurationSource
    {
        /// <summary>
        /// The prefix (<see cref="IConfigurationSection"/> under which all Spring Cloud Config Server 
        /// configuration settings (<see cref="ConfigServerClientSettings"/> are found. 
        ///   (e.g. spring:cloud:config:env, spring:cloud:config:uri, spring:cloud:config:enabled, etc.)
        /// </summary>
        public const string PREFIX = "spring:cloud:config";

        private static readonly TimeSpan DEFAULT_TIMEOUT = new TimeSpan(0, 0, 3);
        protected ConfigServerClientSettings _settings;
        protected IHostingEnvironment _environment;
        protected HttpClient _client;
        protected ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigServerConfigurationProvider"/> with default
        /// configuration settings. <see cref="ConfigServerClientSettings"/>
        /// <param name="environment">required Hosting environment, used in establishing config server profile</param>
        /// <param name="logFactory">optional logging factory</param>
        /// </summary>
        public ConfigServerConfigurationProvider(IHostingEnvironment environment, ILoggerFactory logFactory = null) :
            this(new ConfigServerClientSettings(), environment, logFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigServerConfigurationProvider"/>.
        /// </summary>
        /// <param name="settings">the configuration settings the provider uses when accessing the server.</param>
        /// <param name="environment">required Hosting environment, used in establishing config server profile</param>
        /// <param name="logFactory">optional logging factory</param>
        /// </summary>
        public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, IHostingEnvironment environment, ILoggerFactory logFactory = null) 
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _logger = logFactory?.CreateLogger<ConfigServerConfigurationProvider>();
            _settings = settings;
            _client = null;
            _environment = environment;
           
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ConfigServerConfigurationProvider"/>.
        /// </summary>
        /// <param name="settings">the configuration settings the provider uses when
        /// accessing the server.</param>
        /// <param name="httpClient">a HttpClient the provider uses to make requests of
        /// the server.</param>
        /// <param name="environment">required Hosting environment, used in establishing config server profile</param>
        /// <param name="logFactory">optional logging factory</param>
        /// </summary>
        public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, HttpClient httpClient, IHostingEnvironment environment, ILoggerFactory logFactory = null) 
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _logger = logFactory?.CreateLogger<ConfigServerConfigurationProvider>();
            _settings = settings;
            _client = httpClient;
            _environment = environment;
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

        /// <summary>
        /// Loads configuration data from the Spring Cloud Configuration Server as specified by
        /// the <see cref="Settings"/> 
        /// </summary>
        public override void Load()
        {
            // Adds client settings (e.g spring:cloud:config:uri, etc) to the Data dictionary
            AddConfigServerClientSettings();

            if (_settings.RetryEnabled && _settings.FailFast)
            {
                var attempts = 0;
                var backOff = _settings.RetryInitialInterval;
                do
                {
                    _logger?.LogInformation("Fetching config from server at: {0}", _settings.Uri);
                    try
                    {
                        DoLoad();
                        return;
                    }
                    catch (ConfigServerException e)
                    {
                        _logger?.LogInformation("Failed fetching config from server at: {0}, Exception: {1}", _settings.Uri, e);
                        attempts++;
                        if (attempts < _settings.RetryAttempts)
                        {
                            Thread.CurrentThread.Join(backOff);
                            var nextBackoff = (int)(backOff * _settings.RetryMultiplier);
                            backOff = Math.Min(nextBackoff, _settings.RetryMaxInterval);
                        }
                        else
                        {
                            throw;
                        }
                    }

                } while (true);

            } else
            {
                _logger?.LogInformation("Fetching config from server at: {0}", _settings.Uri);
                DoLoad();
            }

        }
        internal void DoLoad()
        {
            Exception error = null;
            try
            {
                foreach (string label in GetLabels())
                {
                    // Make Config Server URI from settings
                    var path = GetConfigServerUri(label);

                    // Invoke config server, and wait for results
                    Task<ConfigEnvironment> task = RemoteLoadAsync(path);
                    task.Wait();
                    ConfigEnvironment env = task.Result;

                    // Update config Data dictionary with any results
                    if (env != null)
                    {
                        _logger?.LogInformation("Located environment: {0}, {1}, {2}, {3}", env.Name, env.Profiles, env.Label, env.Version);
                        var sources = env.PropertySources;
                        if (sources != null)
                        {
                            int index = sources.Count - 1;
                            for( ; index>=0; index--)
                            {
                                AddPropertySource(sources[index]);
                            }
                        }
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            _logger?.LogWarning("Could not locate PropertySource: " + error?.ToString());

            if (_settings.FailFast)
            {
                throw new ConfigServerException("Could not locate PropertySource, fail fast property is set, failing", error);
            }
        }

        /// <summary>
        /// Create the HttpRequestMessage that will be used in accessing the Spring Cloud Configuration server
        /// </summary>
        /// <param name="requestUri">the Uri used when accessing the server</param>
        /// <returns>The HttpRequestMessage built from the path</returns>
        internal protected virtual HttpRequestMessage GetRequestMessage(string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            if (!string.IsNullOrEmpty(_settings.Password))
            {
                AuthenticationHeaderValue auth = new AuthenticationHeaderValue("Basic",
                    GetEncoded(_settings.Username, _settings.Password));
                request.Headers.Authorization = auth;
            }

            return request;
        }

        /// <summary>
        /// Adds the client settings for the Configuration Server to the data dictionary
        /// </summary>
        internal protected virtual void AddConfigServerClientSettings()
        {
            Data["spring:cloud:config:enabled"] = _settings.Enabled.ToString();
            Data["spring:cloud:config:failFast"] = _settings.FailFast.ToString();
            Data["spring:cloud:config:env"] = _settings.Environment;
            Data["spring:cloud:config:label"] = _settings.Label;
            Data["spring:cloud:config:name"] = _settings.Name;
            Data["spring:cloud:config:password"] = _settings.Password;
            Data["spring:cloud:config:uri"] = _settings.Uri;
            Data["spring:cloud:config:username"] = _settings.Username;
            Data["spring:cloud:config:validate_certificates"] = _settings.ValidateCertificates.ToString();
            Data["spring:cloud:config:retry:enabled"] = _settings.RetryEnabled.ToString();
            Data["spring:cloud:config:retry:maxAttempts"] = _settings.RetryAttempts.ToString();
            Data["spring:cloud:config:retry:initialInterval"] = _settings.RetryInitialInterval.ToString();
            Data["spring:cloud:config:retry:maxInterval"] = _settings.RetryMaxInterval.ToString();
            Data["spring:cloud:config:retry:multiplier"] = _settings.RetryMultiplier.ToString();
        }

        /// <summary>
        /// Asynchronously calls the Spring Cloud Configuration Server using the provided Uri and returning a 
        /// a task that can be used to obtain the results
        /// </summary>
        /// <param name="requestUri">the Uri used in accessing the Spring Cloud Configuration Server</param>
        /// <returns>The task object representing the asynchronous operation</returns>
        internal protected virtual async Task<ConfigEnvironment> RemoteLoadAsync(string requestUri)
        {
            // Get client if not already set
            if (_client == null)
            {
                _client = GetHttpClient(_settings);
            }

            // Get the request message 
            var request = GetRequestMessage(requestUri);

#if NET46
            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            if (!_settings.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif

            // Invoke config server
            try
            {
                using (HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                            return null;

                        // Log status
                        var message = string.Format("Config Server returned status: {0} invoking path: {1}",
                            response.StatusCode, requestUri);

                        _logger?.LogInformation(message);

                        // Throw if status >= 400 
                        if (response.StatusCode >= HttpStatusCode.BadRequest)
                            throw new HttpRequestException(message);
                        else
                            return null;
                    }

                    Stream stream = await response.Content.ReadAsStreamAsync();
                    return Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                // Log and rethrow
                _logger?.LogError("Config Server exception: {0}, path: {1}", e, requestUri);
                throw;
            }
#if NET46
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
            }
#endif

        }

        /// <summary>
        /// Deserialize the response from the Configuration Server
        /// </summary>
        /// <param name="stream">the stream representing the response from the Configuration Server</param>
        /// <returns>The ConfigEnvironment object representing the response from the server</returns>
        internal protected virtual ConfigEnvironment Deserialize(Stream stream)
        {
            try
            {
                using (JsonReader reader = new JsonTextReader(new StreamReader(stream)))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    return (ConfigEnvironment)serializer.Deserialize(reader, typeof(ConfigEnvironment));
                }
            }
            catch (Exception e)
            {
                _logger?.LogError("Config Server serialization exception: {0}", e);
            }
            return null;
        }

        /// <summary>
        /// Create the Uri that will be used in accessing the Configuration Server
        /// </summary>
        /// <param name="label">a label to add</param>
        /// <returns>The request URI for the Configuration Server</returns>
        internal protected virtual string GetConfigServerUri(string label)
        {

            var path = _settings.Name + "/" + _settings.Environment;
            if (!string.IsNullOrWhiteSpace(label))
                path = path + "/" + label;

            if (!_settings.RawUri.EndsWith("/")) 
               path = "/" + path;
       
            return _settings.RawUri + path;

        }

        /// <summary>
        /// Adds values from a PropertySource to the Configurtation Data dictionary managed
        /// by this provider
        /// </summary>
        /// <param name="source">a property source to add</param>

        internal protected virtual void AddPropertySource(PropertySource source)
        {
            if (source == null || source.Source == null)
                return;

            foreach (KeyValuePair<string, object> kvp in source.Source)
            {
                try
                {
                    string key = kvp.Key.Replace(".", ConfigurationPath.KeyDelimiter);
                    string value = kvp.Value.ToString();
                    Data[key] = value;
                }
                catch (Exception e)
                {
                    _logger?.LogError("Config Server exception, property: {0}={1}", kvp.Key, kvp.Value.GetType(), e);
                }

            }
        }
        /// <summary>
        /// Encode the username password for a http request
        /// </summary>
        /// <param name="user">the username</param>
        /// <param name="password">the password</param>
        /// <returns></returns>
        internal protected string GetEncoded(string user, string password)
        {
            if (user == null)
                user = string.Empty;
            if (password == null)
                password = string.Empty;
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + password));
        }


        /// <summary>
        /// Creates an appropriatly configured HttpClient that will be used in communicating with the
        /// Spring Cloud Configuration Server
        /// </summary>
        /// <param name="settings">the settings used in configuring the HttpClient</param>
        /// <returns>The HttpClient used by the provider</returns>
        protected static HttpClient GetHttpClient(ConfigServerClientSettings settings)
        {
            HttpClient client = null;
#if NET46
            client = new HttpClient();
#else
            if (settings != null && !settings.ValidateCertificates)
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                client = new HttpClient(handler);
            } else
            {
                client = new HttpClient();
            }
#endif
            client.Timeout = DEFAULT_TIMEOUT;
            return client;
        }

        internal string[] GetLabels()
        {
            if (string.IsNullOrWhiteSpace(_settings.Label))
            {
                return EMPTY_LABELS;
            }

            return _settings.Label.Split(COMMA_DELIMIT, StringSplitOptions.RemoveEmptyEntries);
        }

        public virtual IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            ConfigurationBuilder config = new ConfigurationBuilder();
            foreach (IConfigurationSource s in builder.Sources)
            {
                if (s == this)
                {
                    break;
                }
                config.Add(s);
            }
            IConfigurationRoot existing = config.Build();
            ConfigurationSettingsHelper.Initialize(PREFIX, _settings, _environment, existing);
            return this;

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

        private static readonly char[] COMMA_DELIMIT = new char[] { ',' };
        private static readonly string[] EMPTY_LABELS = new string[] { "" };
    }
}
