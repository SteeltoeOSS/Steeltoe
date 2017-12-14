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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

        public const string TOKEN_HEADER = "X-Config-Token";
        public const string STATE_HEADER = "X-Config-State";

        protected ConfigServerClientSettings _settings;
        protected HttpClient _client;
        protected ILogger _logger;

        private const string ArrayPattern = @"(\[[0-9]+\])*$";
        private static readonly char[] COMMA_DELIMIT = new char[] { ',' };
        private static readonly string[] EMPTY_LABELS = new string[] { string.Empty };

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider"/> class with default
        /// configuration settings. <see cref="ConfigServerClientSettings"/>
        /// </summary>
        /// <param name="logFactory">optional logging factory</param>
        public ConfigServerConfigurationProvider(ILoggerFactory logFactory = null)
            : this(new ConfigServerClientSettings(), logFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider"/> class.
        /// </summary>
        /// <param name="settings">the configuration settings the provider uses when accessing the server.</param>
        /// <param name="logFactory">optional logging factory</param>
        public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, ILoggerFactory logFactory = null)
        {
            _logger = logFactory?.CreateLogger<ConfigServerConfigurationProvider>();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _client = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider"/> class.
        /// </summary>
        /// <param name="settings">the configuration settings the provider uses when accessing the server.</param>
        /// <param name="httpClient">a HttpClient the provider uses to make requests of the server.</param>
        /// <param name="logFactory">optional logging factory</param>
        public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, HttpClient httpClient, ILoggerFactory logFactory = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logFactory?.CreateLogger<ConfigServerConfigurationProvider>();
        }

        /// <summary>
        /// Gets the configuration settings the provider uses when accessing the server.
        /// </summary>
        public virtual ConfigServerClientSettings Settings => _settings as ConfigServerClientSettings;

        internal IDictionary<string, string> Properties => Data;

        internal ILogger Logger => _logger;

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
                }
                while (true);
            }
            else
            {
                _logger?.LogInformation("Fetching config from server at: {0}", _settings.Uri);
                DoLoad();
            }
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

            IConfiguration existing = config.Build();
            ConfigurationSettingsHelper.Initialize(PREFIX, _settings, existing);
            return this;
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
                            for (; index >= 0; index--)
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

        internal string[] GetLabels()
        {
            if (string.IsNullOrWhiteSpace(_settings.Label))
            {
                return EMPTY_LABELS;
            }

            return _settings.Label.Split(COMMA_DELIMIT, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Create the HttpRequestMessage that will be used in accessing the Spring Cloud Configuration server
        /// </summary>
        /// <param name="requestUri">the Uri used when accessing the server</param>
        /// <returns>The HttpRequestMessage built from the path</returns>
        protected internal virtual HttpRequestMessage GetRequestMessage(string requestUri)
        {
            var request = HttpClientHelper.GetRequestMessage(HttpMethod.Get, requestUri, _settings.Username, _settings.Password);

            if (!string.IsNullOrEmpty(_settings.Token))
            {
                RenewToken(_settings.Token);
                request.Headers.Add(TOKEN_HEADER, _settings.Token);
            }

            return request;
        }

        /// <summary>
        /// Adds the client settings for the Configuration Server to the data dictionary
        /// </summary>
        protected internal virtual void AddConfigServerClientSettings()
        {
            Data["spring:cloud:config:enabled"] = _settings.Enabled.ToString();
            Data["spring:cloud:config:failFast"] = _settings.FailFast.ToString();
            Data["spring:cloud:config:env"] = _settings.Environment;
            Data["spring:cloud:config:label"] = _settings.Label;
            Data["spring:cloud:config:name"] = _settings.Name;
            Data["spring:cloud:config:password"] = _settings.Password;
            Data["spring:cloud:config:uri"] = _settings.Uri;
            Data["spring:cloud:config:username"] = _settings.Username;
            Data["spring:cloud:config:token"] = _settings.Token;
            Data["spring:cloud:config:timeout"] = _settings.Timeout.ToString();
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
        protected internal virtual async Task<ConfigEnvironment> RemoteLoadAsync(string requestUri)
        {
            // Get client if not already set
            if (_client == null)
            {
                _client = GetHttpClient(_settings);
            }

            // Get the request message
            var request = GetRequestMessage(requestUri);

            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
            HttpClientHelper.ConfigureCertificateValidatation(_settings.ValidateCertificates, out prevProtocols, out prevValidator);

            // Invoke config server
            try
            {
                using (HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            return null;
                        }

                        // Log status
                        var message = $"Config Server returned status: {response.StatusCode} invoking path: {requestUri}";

                        _logger?.LogInformation(message);

                        // Throw if status >= 400
                        if (response.StatusCode >= HttpStatusCode.BadRequest)
                        {
                            throw new HttpRequestException(message);
                        }
                        else
                        {
                            return null;
                        }
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
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_settings.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        /// <summary>
        /// Deserialize the response from the Configuration Server
        /// </summary>
        /// <param name="stream">the stream representing the response from the Configuration Server</param>
        /// <returns>The ConfigEnvironment object representing the response from the server</returns>
        protected internal virtual ConfigEnvironment Deserialize(Stream stream)
        {
            return SerializationHelper.Deserialize<ConfigEnvironment>(stream, _logger);
        }

        /// <summary>
        /// Create the Uri that will be used in accessing the Configuration Server
        /// </summary>
        /// <param name="label">a label to add</param>
        /// <returns>The request URI for the Configuration Server</returns>
        protected internal virtual string GetConfigServerUri(string label)
        {
            var path = _settings.Name + "/" + _settings.Environment;
            if (!string.IsNullOrWhiteSpace(label))
            {
                path = path + "/" + label;
            }

            if (!_settings.RawUri.EndsWith("/"))
            {
                path = "/" + path;
            }

            return _settings.RawUri + path;
        }

        /// <summary>
        /// Adds values from a PropertySource to the Configurtation Data dictionary managed
        /// by this provider
        /// </summary>
        /// <param name="source">a property source to add</param>
        protected internal virtual void AddPropertySource(PropertySource source)
        {
            if (source == null || source.Source == null)
            {
                return;
            }

            foreach (KeyValuePair<string, object> kvp in source.Source)
            {
                try
                {
                    string key = ConvertKey(kvp.Key);
                    string value = ConvertValue(kvp.Value);
                    Data[key] = value;
                }
                catch (Exception e)
                {
                    _logger?.LogError("Config Server exception, property: {0}={1}", kvp.Key, kvp.Value.GetType(), e);
                }
            }
        }

        protected internal virtual string ConvertKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return key;
            }

            string[] split = key.Split('.');
            StringBuilder sb = new StringBuilder();
            foreach (var part in split)
            {
                string keyPart = ConvertArrayKey(part);
                sb.Append(keyPart);
                sb.Append(ConfigurationPath.KeyDelimiter);
            }

            return sb.ToString(0, sb.Length - 1);
        }

        protected internal virtual string ConvertArrayKey(string key)
        {
            return Regex.Replace(key, ArrayPattern, (match) =>
            {
                string result = match.Value.Replace("[", ":").Replace("]", string.Empty);
                return result;
            });
        }

        protected internal virtual string ConvertValue(object value)
        {
            return value.ToString();
        }

        /// <summary>
        /// Encode the username password for a http request
        /// </summary>
        /// <param name="user">the username</param>
        /// <param name="password">the password</param>
        /// <returns>Encoded user + password</returns>
        protected internal string GetEncoded(string user, string password)
        {
            return HttpClientHelper.GetEncodedUserPassword(user, password);
        }

        protected internal virtual void RenewToken(string token)
        {
        }

        /// <summary>
        /// Creates an appropriatly configured HttpClient that will be used in communicating with the
        /// Spring Cloud Configuration Server
        /// </summary>
        /// <param name="settings">the settings used in configuring the HttpClient</param>
        /// <returns>The HttpClient used by the provider</returns>
        protected static HttpClient GetHttpClient(ConfigServerClientSettings settings)
        {
            return HttpClientHelper.GetHttpClient(settings.ValidateCertificates, settings.Timeout);
        }
    }
}
