// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Http;
using Steeltoe.Common.Logging;
using Steeltoe.Discovery;
using Steeltoe.Extensions.Configuration.Placeholder;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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

        protected ConfigServerClientSettings _settings; // Current settings
        protected HttpClient _httpClient;
        protected ILogger _logger;
        protected ILoggerFactory _loggerFactory;
        protected IConfiguration _configuration;
        protected Timer _tokenRenewTimer;
        protected Timer _refreshTimer;
        protected bool _hasConfiguration;

        private const string ArrayPattern = @"(\[[0-9]+\])*$";
        private const string VAULT_RENEW_PATH = "vault/v1/auth/token/renew-self";
        private const string VAULT_TOKEN_HEADER = "X-Vault-Token";
        private const string DELIMITER_STRING = ".";
        private const char DELIMITER_CHAR = '.';
        private const char ESCAPE_CHAR = '\\';
        private const string ESCAPE_STRING = "\\";

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
            _ = settings ?? throw new ArgumentNullException(nameof(settings));
            logFactory ??= BootstrapLoggerFactory.Instance;
            Initialize(settings, logFactory: logFactory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider"/> class.
        /// </summary>
        /// <param name="settings">the configuration settings the provider uses when accessing the server.</param>
        /// <param name="httpClient">a HttpClient the provider uses to make requests of the server.</param>
        /// <param name="logFactory">optional logging factory</param>
        public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, HttpClient httpClient, ILoggerFactory logFactory = null)
        {
            _ = settings ?? throw new ArgumentNullException(nameof(settings));
            logFactory ??= BootstrapLoggerFactory.Instance;
            _ = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Initialize(settings, httpClient: httpClient, logFactory: logFactory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider"/> class from a <see cref="ConfigServerConfigurationSource"/>
        /// </summary>
        /// <param name="source">the <see cref="ConfigServerConfigurationSource"/> the provider uses when accessing the server.</param>
        public ConfigServerConfigurationProvider(ConfigServerConfigurationSource source)
        {
            _ = source.Configuration as IConfigurationRoot;
            Initialize(source);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider"/> class from a <see cref="ConfigServerConfigurationSource"/>
        /// </summary>
        /// <param name="source">the <see cref="ConfigServerConfigurationSource"/> the provider uses when accessing the server.</param>
        /// <param name="httpClient">the httpClient to use</param>
        public ConfigServerConfigurationProvider(ConfigServerConfigurationSource source, HttpClient httpClient)
        {
            _ = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Initialize(source, httpClient);
        }

        internal void Initialize(ConfigServerConfigurationSource source, HttpClient httpClient = null, ILoggerFactory logFactory = null)
        {
            var settings = source.DefaultSettings;
            var configuration = WrapWithPlaceholderResolver(source.Configuration);
            Initialize(settings, configuration, httpClient, logFactory);
        }

        internal void Initialize(ConfigServerClientSettings settings, IConfiguration configuration = null, HttpClient httpClient = null, ILoggerFactory logFactory = null)
        {
            _loggerFactory = logFactory ?? new NullLoggerFactory();
            _logger = _loggerFactory.CreateLogger<ConfigServerConfigurationProvider>();
            if (configuration != null)
            {
                _configuration = configuration;
                _hasConfiguration = true;
            }
            else
            {
                _configuration = new ConfigurationBuilder().Build();
                _hasConfiguration = false;
            }

            _settings = settings;
            _httpClient = httpClient ?? GetHttpClient(_settings);

            OnSettingsChanged();
        }

        private void OnSettingsChanged()
        {
            var existingPollingInterval = _settings.PollingInterval;
            if (_hasConfiguration)
            {
                ConfigurationSettingsHelper.Initialize(PREFIX, _settings, _configuration);
                _configuration.GetReloadToken().RegisterChangeCallback(_ => OnSettingsChanged(), null);
            }

            if (_settings.PollingInterval == TimeSpan.Zero)
            {
                _refreshTimer?.Dispose();
            }
            else if (_refreshTimer == null)
            {
                _refreshTimer = new Timer(_ => DoLoad(), null, TimeSpan.Zero, _settings.PollingInterval);
            }
            else if (existingPollingInterval != _settings.PollingInterval)
            {
                _refreshTimer.Change(TimeSpan.Zero, _settings.PollingInterval);
            }
        }

        /// <summary>
        /// Gets the configuration settings the provider uses when accessing the server.
        /// </summary>
        public virtual ConfigServerClientSettings Settings => _settings;

        internal JsonSerializerOptions SerializerOptions { get; private set; } =
            new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
            };

        internal IDictionary<string, string> Properties => Data;

        internal ILogger Logger => _logger;

        internal ConfigServerDiscoveryService _configServerDiscoveryService;

        /// <summary>
        /// Loads configuration data from the Spring Cloud Configuration Server as specified by
        /// the <see cref="Settings"/>
        /// </summary>
        public override void Load()
        {
            LoadInternal(true);
        }

        [Obsolete("Will be removed in next release, use the ConfigServerConfigurationSource")]
        public virtual IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var config = new ConfigurationBuilder();
            foreach (var s in builder.Sources)
            {
                if (s == this)
                {
                    break;
                }

                config.Add(s);
            }

            _configuration = WrapWithPlaceholderResolver(config.Build());
            ConfigurationSettingsHelper.Initialize(PREFIX, _settings, _configuration);
            return this;
        }

        internal ConfigEnvironment LoadInternal(bool updateDictionary = true)
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation("Config Server client disabled, did not fetch configuration!");
                return null;
            }

            if (IsDiscoveryFirstEnabled())
            {
                _configServerDiscoveryService ??= new ConfigServerDiscoveryService(_configuration, _settings, _loggerFactory);
                DiscoverServerInstances();
            }

            // Adds client settings (e.g spring:cloud:config:uri, etc) to the Data dictionary
            AddConfigServerClientSettings();

            if (_settings.RetryEnabled && _settings.FailFast)
            {
                var attempts = 0;
                var backOff = _settings.RetryInitialInterval;
                do
                {
                    _logger.LogInformation("Fetching config from server at: {0}", _settings.Uri);
                    try
                    {
                        return DoLoad(updateDictionary);
                    }
                    catch (ConfigServerException e)
                    {
                        _logger.LogInformation("Failed fetching config from server at: {0}, Exception: {1}", _settings.Uri, e);
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
                _logger.LogInformation("Fetching config from server at: {0}", _settings.Uri);
                return DoLoad(updateDictionary);
            }
        }

        internal ConfigEnvironment DoLoad(bool updateDictionary = true)
        {
            Exception error = null;

            // Get arrays of config server uris to check
            var uris = _settings.GetUris();

            try
            {
                foreach (var label in GetLabels())
                {
                    Task<ConfigEnvironment> task = null;

                    if (uris.Length > 1)
                    {
                        _logger.LogInformation("Multiple Config Server Uris listed.");

                        // Invoke config servers
                        task = RemoteLoadAsync(uris, label);
                    }
                    else
                    {
                        // Single, server make Config Server URI from settings
#pragma warning disable CS0618 // Type or member is obsolete
                        var path = GetConfigServerUri(label);

                        // Invoke config server
                        task = RemoteLoadAsync(path);
#pragma warning restore CS0618 // Type or member is obsolete
                    }

                    // Wait for results from server
                    var env = task.GetAwaiter().GetResult();

                    // Update config Data dictionary with any results
                    if (env != null)
                    {
                        _logger.LogInformation(
                            "Located environment: {name}, {profiles}, {label}, {version}, {state}", env.Name, env.Profiles, env.Label, env.Version, env.State);
                        if (updateDictionary)
                        {
                            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                            if (!string.IsNullOrEmpty(env.State))
                            {
                                data["spring:cloud:config:client:state"] = env.State;
                            }

                            if (!string.IsNullOrEmpty(env.Version))
                            {
                                data["spring:cloud:config:client:version"] = env.Version;
                            }

                            var sources = env.PropertySources;
                            if (sources != null)
                            {
                                var index = sources.Count - 1;
                                for (; index >= 0; index--)
                                {
                                    AddPropertySource(sources[index], data);
                                }
                            }

                            // Adds client settings (e.g spring:cloud:config:uri, etc) back to the (new) Data dictionary
                            AddConfigServerClientSettings(data);

                            static bool AreEqual<TKey, TValue>(IDictionary<TKey, TValue> dict1, IDictionary<TKey, TValue> dict2)
                            {
                                IEqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;

                                return dict1.Count == dict2.Count &&
                                        dict1.Keys.All(key => dict2.ContainsKey(key) && valueComparer.Equals(dict1[key], dict2[key]));
                            }

                            if (!AreEqual(Data, data))
                            {
                                Data = data;
                                OnReload();
                            }
                        }

                        return env;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            _logger.LogWarning("Could not locate PropertySource: " + error?.ToString());

            if (_settings.FailFast)
            {
                throw new ConfigServerException("Could not locate PropertySource, fail fast property is set, failing", error);
            }

            return null;
        }

        internal string[] GetLabels()
        {
            if (string.IsNullOrWhiteSpace(_settings.Label))
            {
                return EMPTY_LABELS;
            }

            return _settings.Label.Split(COMMA_DELIMIT, StringSplitOptions.RemoveEmptyEntries);
        }

        internal void DiscoverServerInstances()
        {
            var instances = _configServerDiscoveryService.GetConfigServerInstances();
            if (!instances.Any())
            {
                if (_settings.FailFast)
                {
                    throw new ConfigServerException("Could not locate config server via discovery, are you missing a Discovery service assembly?");
                }

                return;
            }

            UpdateSettingsFromDiscovery(instances, _settings);
        }

        internal void UpdateSettingsFromDiscovery(IEnumerable<IServiceInstance> instances, ConfigServerClientSettings settings)
        {
            var endpoints = new StringBuilder();
            foreach (var instance in instances)
            {
                var uri = instance.Uri.ToString();
                var metaData = instance.Metadata;
                if (metaData != null)
                {
                    if (metaData.TryGetValue("password", out var password))
                    {
                        metaData.TryGetValue("user", out var username);
                        username ??= "user";
                        settings.Username = username;
                        settings.Password = password;
                    }

                    if (metaData.TryGetValue("configPath", out var path))
                    {
                        if (uri.EndsWith("/") && path.StartsWith("/"))
                        {
                            uri = uri.Substring(0, uri.Length - 1);
                        }

                        uri += path;
                    }
                }

                endpoints.Append(uri);
                endpoints.Append(',');
            }

            if (endpoints.Length > 0)
            {
                var uris = endpoints.ToString(0, endpoints.Length - 1);
                settings.Uri = uris;
            }
        }

        internal async Task ProvideRuntimeReplacementsAsync(IDiscoveryClient discoveryClientFromDI, ILoggerFactory loggerFactory)
        {
            if (_configServerDiscoveryService is not null)
            {
                await _configServerDiscoveryService.ProvideRuntimeReplacementsAsync(discoveryClientFromDI, loggerFactory);
            }
        }

        internal async Task ShutdownAsync()
        {
            if (_configServerDiscoveryService is not null)
            {
                await _configServerDiscoveryService.ShutdownAsync();
            }
        }

        /// <summary>
        /// Create the HttpRequestMessage that will be used in accessing the Spring Cloud Configuration server
        /// </summary>
        /// <param name="requestUri">the Uri used when accessing the server</param>
        /// <param name="username">username to use if required</param>
        /// <param name="password">password to use if required</param>
        /// <returns>The HttpRequestMessage built from the path</returns>
        protected internal virtual HttpRequestMessage GetRequestMessage(string requestUri, string username, string password)
        {
            var request = string.IsNullOrEmpty(_settings.AccessTokenUri)
                ? HttpClientHelper.GetRequestMessage(HttpMethod.Get, requestUri, username, password)
                : HttpClientHelper.GetRequestMessage(HttpMethod.Get, requestUri, FetchAccessToken);

            if (!string.IsNullOrEmpty(_settings.Token) && !ConfigServerClientSettings.IsMultiServerConfig(_settings.Uri))
            {
                if (!_settings.DisableTokenRenewal)
                {
                    RenewToken(_settings.Token);
                }

                request.Headers.Add(TOKEN_HEADER, _settings.Token);
            }

            return request;
        }

        /// <summary>
        /// Create the HttpRequestMessage that will be used in accessing the Spring Cloud Configuration server
        /// </summary>
        /// <param name="requestUri">the Uri used when accessing the server</param>
        /// <returns>The HttpRequestMessage built from the path</returns>
        [Obsolete("Will be removed in next release. See GetRequestMessage(string, string, string)")]
        protected internal virtual HttpRequestMessage GetRequestMessage(string requestUri)
        {
            return GetRequestMessage(requestUri, _settings.Username, _settings.Password);
        }

        /// <summary>
        /// Adds the client settings for the Configuration Server to the Data dictionary
        /// </summary>
        protected internal virtual void AddConfigServerClientSettings()
        {
            var data = Data.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

            AddConfigServerClientSettings(data);

            Data = data;
        }

        /// <summary>
        /// Adds the client settings for the Configuration Server to the data dictionary
        /// </summary>
        protected internal virtual void AddConfigServerClientSettings(IDictionary<string, string> data)
        {
            var culture = CultureInfo.InvariantCulture;
            data["spring:cloud:config:enabled"] = _settings.Enabled.ToString(culture);
            data["spring:cloud:config:failFast"] = _settings.FailFast.ToString(culture);
            data["spring:cloud:config:env"] = _settings.Environment;
            data["spring:cloud:config:label"] = _settings.Label;
            data["spring:cloud:config:name"] = _settings.Name;
            data["spring:cloud:config:password"] = _settings.Password;
            data["spring:cloud:config:uri"] = _settings.Uri;
            data["spring:cloud:config:username"] = _settings.Username;
            data["spring:cloud:config:token"] = _settings.Token;
            data["spring:cloud:config:timeout"] = _settings.Timeout.ToString(culture);
            data["spring:cloud:config:validate_certificates"] = _settings.ValidateCertificates.ToString(culture);
            data["spring:cloud:config:retry:enabled"] = _settings.RetryEnabled.ToString(culture);
            data["spring:cloud:config:retry:maxAttempts"] = _settings.RetryAttempts.ToString(culture);
            data["spring:cloud:config:retry:initialInterval"] = _settings.RetryInitialInterval.ToString(culture);
            data["spring:cloud:config:retry:maxInterval"] = _settings.RetryMaxInterval.ToString(culture);
            data["spring:cloud:config:retry:multiplier"] = _settings.RetryMultiplier.ToString(culture);

            data["spring:cloud:config:access_token_uri"] = _settings.AccessTokenUri;
            data["spring:cloud:config:client_secret"] = _settings.ClientSecret;
            data["spring:cloud:config:client_id"] = _settings.ClientId;
            data["spring:cloud:config:tokenTtl"] = _settings.TokenTtl.ToString(culture);
            data["spring:cloud:config:tokenRenewRate"] = _settings.TokenRenewRate.ToString(culture);
            data["spring:cloud:config:disableTokenRenewal"] = _settings.DisableTokenRenewal.ToString(culture);

            data["spring:cloud:config:discovery:enabled"] = _settings.DiscoveryEnabled.ToString(culture);
            data["spring:cloud:config:discovery:serviceId"] = _settings.DiscoveryServiceId.ToString(culture);

            data["spring:cloud:config:health:enabled"] = _settings.HealthEnabled.ToString(culture);
            data["spring:cloud:config:health:timeToLive"] = _settings.HealthTimeToLive.ToString(culture);
        }

        protected internal async Task<ConfigEnvironment> RemoteLoadAsync(string[] requestUris, string label)
        {
            // Get client if not already set
            _httpClient ??= GetHttpClient(_settings);

            Exception error = null;
            foreach (var requestUri in requestUris)
            {
                error = null;

                // Get a config server uri and username passwords to use
                var trimUri = requestUri.Trim();
                var serverUri = _settings.GetRawUri(trimUri);
                var username = _settings.GetUserName(trimUri);
                var password = _settings.GetPassword(trimUri);

                // Make Config Server URI from settings
                var path = GetConfigServerUri(serverUri, label);

                // Get the request message
                var request = GetRequestMessage(path, username, password);

                // If certificate validation is disabled, inject a callback to handle properly
                HttpClientHelper.ConfigureCertificateValidation(_settings.ValidateCertificates, out var prevProtocols, out var prevValidator);

                // Invoke config server
                try
                {
                    using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

                    // Log status
                    var message = $"Config Server returned status: {response.StatusCode} invoking path: {requestUri}";
                    _logger.LogInformation(WebUtility.UrlEncode(message));

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            return null;
                        }

                        // Throw if status >= 400
                        if (response.StatusCode >= HttpStatusCode.BadRequest)
                        {
                            // HttpClientErrorException
                            throw new HttpRequestException(message);
                        }
                        else
                        {
                            return null;
                        }
                    }

                    return await response.Content.ReadFromJsonAsync<ConfigEnvironment>(SerializerOptions).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    error = e;
                    _logger.LogError(e, "Config Server exception, path: {requestUri}", WebUtility.UrlEncode(requestUri));
                    if (IsContinueExceptionType(e))
                    {
                        continue;
                    }

                    throw;
                }
                finally
                {
                    HttpClientHelper.RestoreCertificateValidation(_settings.ValidateCertificates, prevProtocols, prevValidator);
                }
            }

            if (error != null)
            {
                throw error;
            }

            return null;
        }

        /// <summary>
        /// Asynchronously calls the Spring Cloud Configuration Server using the provided Uri and returning a
        /// a task that can be used to obtain the results
        /// </summary>
        /// <param name="requestUri">the Uri used in accessing the Spring Cloud Configuration Server</param>
        /// <returns>The task object representing the asynchronous operation</returns>
        [Obsolete("Will be removed in next release. See RemoteLoadAsync(string[], string)")]
        protected internal virtual async Task<ConfigEnvironment> RemoteLoadAsync(string requestUri)
        {
            // Get client if not already set
            _httpClient ??= GetHttpClient(_settings);

            // Get the request message
            var request = GetRequestMessage(requestUri);

            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(_settings.ValidateCertificates, out var prevProtocols, out var prevValidator);

            // Invoke config server
            try
            {
                using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }

                    // Log status
                    var message = $"Config Server returned status: {response.StatusCode} invoking path: {requestUri}";

                    _logger.LogInformation(WebUtility.UrlEncode(message));

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

                return await response.Content.ReadFromJsonAsync<ConfigEnvironment>(SerializerOptions).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Log and rethrow
                _logger.LogError("Config Server exception: {0}, path: {1}", e, WebUtility.UrlEncode(requestUri));
                throw;
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_settings.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        /// <summary>
        /// Create the Uri that will be used in accessing the Configuration Server
        /// </summary>
        /// <param name="baseRawUri">base server uri to use</param>
        /// <param name="label">a label to add</param>
        /// <returns>The request URI for the Configuration Server</returns>
        protected internal virtual string GetConfigServerUri(string baseRawUri, string label)
        {
            if (string.IsNullOrEmpty(baseRawUri))
            {
                throw new ArgumentException(nameof(baseRawUri));
            }

            var path = $"{_settings.Name}/{_settings.Environment}";
            if (!string.IsNullOrWhiteSpace(label))
            {
                // If label contains slash, replace it
                if (label.Contains("/"))
                {
                    label = label.Replace("/", "(_)");
                }

                path = $"{path}/{label.Trim()}";
            }

            if (!baseRawUri.EndsWith("/"))
            {
                path = $"/{path}";
            }

            return baseRawUri + path;
        }

        /// <summary>
        /// Create the Uri that will be used in accessing the Configuration Server
        /// </summary>
        /// <param name="label">a label to add</param>
        /// <returns>The request URI for the Configuration Server</returns>
        [Obsolete("Will be removed in next release. See GetConfigServerUri(string, string)")]
        protected internal virtual string GetConfigServerUri(string label)
        {
            return GetConfigServerUri(_settings.RawUri, label);
        }

        /// <summary>
        /// Adds values from a PropertySource to the Configurtation Data dictionary managed
        /// by this provider
        /// </summary>
        /// <param name="source">a property source to add</param>
        [Obsolete("Will be removed in next release.")]
        protected internal virtual void AddPropertySource(PropertySource source)
        {
            AddPropertySource(source, Data);
        }

        /// <summary>
        /// Adds values from a PropertySource to the provided dictionary.
        /// </summary>
        /// <param name="source">a property source to add</param>
        /// <param name="data">the dictionary to add the property source to</param>
        protected internal void AddPropertySource(PropertySource source, IDictionary<string, string> data)
        {
            if (source == null || source.Source == null)
            {
                return;
            }

            foreach (var kvp in source.Source)
            {
                try
                {
                    var key = ConvertKey(kvp.Key);
                    var value = ConvertValue(kvp.Value);
                    data[key] = value;
                }
                catch (Exception e)
                {
                    _logger.LogError("Config Server exception, property: {0}={1}", kvp.Key, kvp.Value.GetType(), e);
                }
            }
        }

        protected internal virtual string ConvertKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return key;
            }

            var split = Split(key);
            var sb = new StringBuilder();
            foreach (var part in split)
            {
                var keyPart = ConvertArrayKey(part);
                sb.Append(keyPart);
                sb.Append(ConfigurationPath.KeyDelimiter);
            }

            return sb.ToString(0, sb.Length - 1);
        }

        protected internal virtual string[] Split(string source)
        {
            var result = new List<string>();

            var segmentStart = 0;
            for (var i = 0; i < source.Length; i++)
            {
                var readEscapeChar = false;
                if (source[i] == ESCAPE_CHAR)
                {
                    readEscapeChar = true;
                    i++;
                }

                if (!readEscapeChar && source[i] == DELIMITER_CHAR)
                {
                    result.Add(UnEscapeString(
                      source.Substring(segmentStart, i - segmentStart)));
                    segmentStart = i + 1;
                }

                if (i == source.Length - 1)
                {
                    result.Add(UnEscapeString(source.Substring(segmentStart)));
                }
            }

            return result.ToArray();

            string UnEscapeString(string src)
            {
                return src.Replace(ESCAPE_STRING + DELIMITER_STRING, DELIMITER_STRING)
                  .Replace(ESCAPE_STRING + ESCAPE_STRING, ESCAPE_STRING);
            }
        }

        protected internal virtual string ConvertArrayKey(string key)
        {
            return Regex.Replace(key, ArrayPattern, (match) =>
            {
                var result = match.Value.Replace("[", ":").Replace("]", string.Empty);
                return result;
            });
        }

        protected internal virtual string ConvertValue(object value)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture);
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
            _tokenRenewTimer ??= new Timer(
                RefreshVaultTokenAsync,
                null,
                TimeSpan.FromMilliseconds(_settings.TokenRenewRate),
                TimeSpan.FromMilliseconds(_settings.TokenRenewRate));
        }

        /// <summary>
        /// Conduct the OAuth2 client_credentials grant flow returning a task that can be used to obtain the
        /// results
        /// </summary>
        /// <returns>The task object representing asynchronous operation</returns>
        protected internal string FetchAccessToken()
        {
            if (string.IsNullOrEmpty(_settings.AccessTokenUri))
            {
                return null;
            }

            return HttpClientHelper.GetAccessToken(
                _settings.AccessTokenUri,
                _settings.ClientId,
                _settings.ClientSecret,
                _settings.Timeout,
                _settings.ValidateCertificates,
                _httpClient,
                _logger).GetAwaiter().GetResult();
        }

        // fire and forget
#pragma warning disable S3168 // "async" methods should not return "void"
        protected internal async void RefreshVaultTokenAsync(object state)
#pragma warning restore S3168 // "async" methods should not return "void"
        {
            if (string.IsNullOrEmpty(Settings.Token))
            {
                return;
            }

            var obscuredToken = $"{Settings.Token.Substring(0, 4)}[*]{Settings.Token.Substring(Settings.Token.Length - 4)}";

            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(
                _settings.ValidateCertificates,
                out var prevProtocols,
                out var prevValidator);

            try
            {
                _httpClient ??= GetHttpClient(Settings);

                var uri = GetVaultRenewUri();
                var message = GetVaultRenewMessage(uri);

                _logger.LogInformation("Renewing Vault token {0} for {1} milliseconds at Uri {2}", obscuredToken, Settings.TokenTtl, uri);

                using var response = await _httpClient.SendAsync(message).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning("Renewing Vault token {0} returned status: {1}", obscuredToken, response.StatusCode);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to renew Vault token {0}. Is the token invalid or expired? - {1}", obscuredToken, e);
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_settings.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        protected internal virtual string GetVaultRenewUri()
        {
            var rawUri = Settings.RawUris[0];
            if (!rawUri.EndsWith("/"))
            {
                rawUri += "/";
            }

            return rawUri + VAULT_RENEW_PATH;
        }

        protected internal virtual HttpRequestMessage GetVaultRenewMessage(string requestUri)
        {
            var request = HttpClientHelper.GetRequestMessage(HttpMethod.Post, requestUri, FetchAccessToken);

            if (!string.IsNullOrEmpty(Settings.Token))
            {
                request.Headers.Add(VAULT_TOKEN_HEADER, Settings.Token);
            }

            var renewTtlSeconds = Settings.TokenTtl / 1000;
            var json = $"{{\"increment\":{renewTtlSeconds}}}";

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Content = content;
            return request;
        }

        [Obsolete("Will be removed in next release. See GetVaultRenewMessage(string)")]
        protected internal virtual HttpRequestMessage GetValutRenewMessage(string requestUri)
        {
            return GetVaultRenewMessage(requestUri);
        }

        protected internal bool IsDiscoveryFirstEnabled()
        {
            var clientConfigsection = _configuration.GetSection(PREFIX);
            return clientConfigsection.GetValue("discovery:enabled", _settings.DiscoveryEnabled);
        }

        /// <summary>
        /// Creates an appropriatly configured HttpClient that will be used in communicating with the
        /// Spring Cloud Configuration Server
        /// </summary>
        /// <param name="settings">the settings used in configuring the HttpClient</param>
        /// <returns>The HttpClient used by the provider</returns>
        protected static HttpClient GetHttpClient(ConfigServerClientSettings settings)
        {
            var clientHandler = new HttpClientHandler();
            if (settings.ClientCertificate != null)
            {
                clientHandler.ClientCertificates.Add(settings.ClientCertificate);
            }

            var client = HttpClientHelper.GetHttpClient(settings.ValidateCertificates, clientHandler, settings.Timeout);
            if (settings.Headers != null)
            {
                foreach (var header in settings.Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            return client;
        }

        private IConfiguration WrapWithPlaceholderResolver(IConfiguration configuration)
        {
            var root = configuration as IConfigurationRoot;
            if (root.Providers.LastOrDefault() is PlaceholderResolverProvider)
            {
                return configuration;
            }

            return new ConfigurationRoot(new List<IConfigurationProvider>() { new PlaceholderResolverProvider(new List<IConfigurationProvider>(root.Providers)) });
        }

        private bool IsContinueExceptionType(Exception e)
        {
            if (e is TaskCanceledException)
            {
                return true;
            }

            if (e is HttpRequestException && e.InnerException is SocketException)
            {
                return true;
            }

            return false;
        }
    }
}
