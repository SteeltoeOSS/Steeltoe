// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Http;
using Steeltoe.Common.Logging;
using Steeltoe.Discovery;
using Steeltoe.Extensions.Configuration.Placeholder;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

/// <summary>
/// A Spring Cloud Config Server based <see cref="ConfigurationProvider" />.
/// </summary>
public class ConfigServerConfigurationProvider : ConfigurationProvider
{
    private const string VaultRenewPath = "vault/v1/auth/token/renew-self";
    private const string VaultTokenHeader = "X-Vault-Token";
    private const string DotDelimiterString = ".";
    private const char DotDelimiterChar = '.';
    private const char CommaDelimiter = ',';
    private const char EscapeChar = '\\';
    private const string EscapeString = "\\";

    /// <summary>
    /// The <see cref="IConfigurationSection" /> prefix under which all Spring Cloud Config Server configuration settings (
    /// <see cref="ConfigServerClientSettings" />) are found. (e.g. spring:cloud:config:env, spring:cloud:config:uri, spring:cloud:config:enabled, etc.)
    /// </summary>
    private const string ConfigurationPrefix = "spring:cloud:config";

    internal const string TokenHeader = "X-Config-Token";

    private static readonly Regex ArrayRegex = new(@"(\[[0-9]+\])*$", RegexOptions.Compiled);

    private static readonly string[] EmptyLabels =
    {
        string.Empty
    };

    private ConfigServerDiscoveryService _configServerDiscoveryService;
    private ConfigServerClientSettings _settings;
    private ILoggerFactory _loggerFactory;
    private IConfiguration _configuration;
    private Timer _refreshTimer;
    private bool _hasConfiguration;

    internal JsonSerializerOptions SerializerOptions { get; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    internal IDictionary<string, string> Properties => Data;
    internal ILogger Logger { get; private set; }

    protected HttpClient HttpClient { get; private set; }

    /// <summary>
    /// Gets the configuration settings the provider uses when accessing the server.
    /// </summary>
    public virtual ConfigServerClientSettings Settings => _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class with default configuration settings.
    /// <see cref="ConfigServerClientSettings" />.
    /// </summary>
    public ConfigServerConfigurationProvider()
        : this((ILoggerFactory)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class with default configuration settings.
    /// <see cref="ConfigServerClientSettings" />.
    /// </summary>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationProvider(ILoggerFactory loggerFactory)
        : this(new ConfigServerClientSettings(), loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class.
    /// </summary>
    /// <param name="settings">
    /// The configuration settings the provider uses when accessing the server.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerClientSettings settings)
        : this(settings, (ILoggerFactory)null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class.
    /// </summary>
    /// <param name="settings">
    /// The configuration settings the provider uses when accessing the server.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(settings);

        loggerFactory ??= BootstrapLoggerFactory.Instance;
        Initialize(settings, null, null, loggerFactory);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class.
    /// </summary>
    /// <param name="settings">
    /// The configuration settings the provider uses when accessing the server.
    /// </param>
    /// <param name="httpClient">
    /// A HttpClient the provider uses to make requests of the server.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, HttpClient httpClient)
        : this(settings, httpClient, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class.
    /// </summary>
    /// <param name="settings">
    /// The configuration settings the provider uses when accessing the server.
    /// </param>
    /// <param name="httpClient">
    /// A HttpClient the provider uses to make requests of the server.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(settings);
        ArgumentGuard.NotNull(httpClient);

        loggerFactory ??= BootstrapLoggerFactory.Instance;

        Initialize(settings, null, httpClient, loggerFactory);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class from a <see cref="ConfigServerConfigurationSource" />.
    /// </summary>
    /// <param name="source">
    /// The <see cref="ConfigServerConfigurationSource" /> the provider uses when accessing the server.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerConfigurationSource source)
        : this(source, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class from a <see cref="ConfigServerConfigurationSource" />.
    /// </summary>
    /// <param name="source">
    /// The <see cref="ConfigServerConfigurationSource" /> the provider uses when accessing the server.
    /// </param>
    /// <param name="httpClient">
    /// The httpClient to use.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerConfigurationSource source, HttpClient httpClient)
    {
        ArgumentGuard.NotNull(source);

        ConfigServerClientSettings newSettings = source.DefaultSettings;
        IConfiguration configuration = WrapWithPlaceholderResolver(source.Configuration);
        Initialize(newSettings, configuration, httpClient, null);
    }

    private void Initialize(ConfigServerClientSettings settings, IConfiguration configuration, HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        Logger = _loggerFactory.CreateLogger<ConfigServerConfigurationProvider>();

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
        HttpClient = httpClient ?? GetConfiguredHttpClient(_settings);

        OnSettingsChanged();
    }

    private void OnSettingsChanged()
    {
        TimeSpan existingPollingInterval = _settings.PollingInterval;

        if (_hasConfiguration)
        {
            ConfigurationSettingsHelper.Initialize(ConfigurationPrefix, _settings, _configuration);
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
    /// Loads configuration data from the Spring Cloud Configuration Server as specified by the <see cref="Settings" />.
    /// </summary>
    public override void Load()
    {
        LoadInternal();
    }

    internal ConfigEnvironment LoadInternal(bool updateDictionary = true)
    {
        if (!_settings.Enabled)
        {
            Logger.LogInformation("Config Server client disabled, did not fetch configuration!");
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
            int attempts = 0;
            int backOff = _settings.RetryInitialInterval;

            do
            {
                Logger.LogInformation("Fetching configuration from server at: {uri}", _settings.Uri);

                try
                {
                    return DoLoad(updateDictionary);
                }
                catch (ConfigServerException e)
                {
                    Logger.LogInformation(e, "Failed fetching configuration from server at: {uri}.", _settings.Uri);
                    attempts++;

                    if (attempts < _settings.RetryAttempts)
                    {
                        Thread.CurrentThread.Join(backOff);
                        int nextBackOff = (int)(backOff * _settings.RetryMultiplier);
                        backOff = Math.Min(nextBackOff, _settings.RetryMaxInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            while (true);
        }

        Logger.LogInformation("Fetching configuration from server at: {uri}", _settings.Uri);
        return DoLoad(updateDictionary);
    }

    internal ConfigEnvironment DoLoad(bool updateDictionary = true)
    {
        Exception error = null;

        // Get arrays of Config Server uris to check
        string[] uris = _settings.GetUris();

        try
        {
            foreach (string label in GetLabels())
            {
                if (uris.Length > 1)
                {
                    Logger.LogInformation("Multiple Config Server Uris listed.");
                }

                // Invoke Config Servers
                Task<ConfigEnvironment> task = RemoteLoadAsync(uris, label);

                // Wait for results from server
                ConfigEnvironment env = task.GetAwaiter().GetResult();

                // Update configuration Data dictionary with any results
                if (env != null)
                {
                    Logger.LogInformation("Located environment: {name}, {profiles}, {label}, {version}, {state}", env.Name, env.Profiles, env.Label,
                        env.Version, env.State);

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

                        IList<PropertySource> sources = env.PropertySources;

                        if (sources != null)
                        {
                            int index = sources.Count - 1;

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

                            return dict1.Count == dict2.Count && dict1.Keys.All(key => dict2.ContainsKey(key) && valueComparer.Equals(dict1[key], dict2[key]));
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

        Logger.LogWarning(error, "Could not locate PropertySource");

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
            return EmptyLabels;
        }

        return _settings.Label.Split(CommaDelimiter, StringSplitOptions.RemoveEmptyEntries);
    }

    private void DiscoverServerInstances()
    {
        IServiceInstance[] instances = _configServerDiscoveryService.GetConfigServerInstances().ToArray();

        if (!instances.Any())
        {
            if (_settings.FailFast)
            {
                throw new ConfigServerException("Could not locate Config Server via discovery, are you missing a Discovery service assembly?");
            }

            return;
        }

        UpdateSettingsFromDiscovery(instances, _settings);
    }

    internal void UpdateSettingsFromDiscovery(IEnumerable<IServiceInstance> instances, ConfigServerClientSettings settings)
    {
        var endpoints = new StringBuilder();

        foreach (IServiceInstance instance in instances)
        {
            string uri = instance.Uri.ToString();
            IDictionary<string, string> metaData = instance.Metadata;

            if (metaData != null)
            {
                if (metaData.TryGetValue("password", out string password))
                {
                    metaData.TryGetValue("user", out string username);
                    username ??= "user";
                    settings.Username = username;
                    settings.Password = password;
                }

                if (metaData.TryGetValue("configPath", out string path))
                {
                    if (uri.EndsWith("/", StringComparison.Ordinal) && path.StartsWith("/", StringComparison.Ordinal))
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
            string uris = endpoints.ToString(0, endpoints.Length - 1);
            settings.Uri = uris;
        }
    }

    internal async Task ProvideRuntimeReplacementsAsync(IDiscoveryClient discoveryClientFromDi)
    {
        if (_configServerDiscoveryService is not null)
        {
            await _configServerDiscoveryService.ProvideRuntimeReplacementsAsync(discoveryClientFromDi);
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
    /// Creates the <see cref="HttpRequestMessage" /> that will be used in accessing the Spring Cloud Configuration server.
    /// </summary>
    /// <param name="requestUri">
    /// The Uri used when accessing the server.
    /// </param>
    /// <param name="username">
    /// Username to use if required.
    /// </param>
    /// <param name="password">
    /// Password to use if required.
    /// </param>
    /// <returns>
    /// The HttpRequestMessage built from the path.
    /// </returns>
    protected internal virtual HttpRequestMessage GetRequestMessage(string requestUri, string username, string password)
    {
        HttpRequestMessage request = string.IsNullOrEmpty(_settings.AccessTokenUri)
            ? HttpClientHelper.GetRequestMessage(HttpMethod.Get, requestUri, username, password)
            : HttpClientHelper.GetRequestMessage(HttpMethod.Get, requestUri, () => FetchAccessTokenAsync().GetAwaiter().GetResult());

        if (!string.IsNullOrEmpty(_settings.Token) && !ConfigServerClientSettings.IsMultiServerConfiguration(_settings.Uri))
        {
            if (!_settings.DisableTokenRenewal)
            {
                RenewToken();
            }

            request.Headers.Add(TokenHeader, _settings.Token);
        }

        return request;
    }

    /// <summary>
    /// Adds the client settings for the Configuration Server to the Data dictionary.
    /// </summary>
    protected internal virtual void AddConfigServerClientSettings()
    {
        Dictionary<string, string> data = Data.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        AddConfigServerClientSettings(data);

        Data = data;
    }

    /// <summary>
    /// Adds the client settings for the Configuration Server to the data dictionary.
    /// </summary>
    /// <param name="data">
    /// The client settings to add.
    /// </param>
    protected virtual void AddConfigServerClientSettings(IDictionary<string, string> data)
    {
        ArgumentGuard.NotNull(data);

        CultureInfo culture = CultureInfo.InvariantCulture;
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

    protected internal async Task<ConfigEnvironment> RemoteLoadAsync(IEnumerable<string> requestUris, string label)
    {
        ArgumentGuard.NotNull(requestUris);

        // Get client if not already set
        HttpClient ??= GetConfiguredHttpClient(_settings);

        Exception error = null;

        foreach (string requestUri in requestUris)
        {
            // Get a Config Server uri and username passwords to use
            string trimUri = requestUri.Trim();
            string serverUri = _settings.GetRawUri(trimUri);
            string username = _settings.GetUserName(trimUri);
            string password = _settings.GetPassword(trimUri);

            // Make Config Server URI from settings
            string path = GetConfigServerUri(serverUri, label);

            // Get the request message
            HttpRequestMessage request = GetRequestMessage(path, username, password);

            // Invoke Config Server
            try
            {
                using HttpResponseMessage response = await HttpClient.SendAsync(request);

                // Log status
                string message = $"Config Server returned status: {response.StatusCode} invoking path: {requestUri}";
                Logger.LogInformation(WebUtility.UrlEncode(message));

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

                    return null;
                }

                return await response.Content.ReadFromJsonAsync<ConfigEnvironment>(SerializerOptions);
            }
            catch (Exception exception)
            {
                error = exception;
                Logger.LogError(exception, "Config Server exception, path: {requestUri}", WebUtility.UrlEncode(requestUri));

                if (IsContinueExceptionType(exception))
                {
                    continue;
                }

                throw;
            }
        }

        if (error != null)
        {
            throw error;
        }

        return null;
    }

    /// <summary>
    /// Creates the Uri that will be used in accessing the Configuration Server.
    /// </summary>
    /// <param name="baseRawUri">
    /// Base server uri to use.
    /// </param>
    /// <param name="label">
    /// The label to add.
    /// </param>
    /// <returns>
    /// The request URI for the Configuration Server.
    /// </returns>
    protected internal virtual string GetConfigServerUri(string baseRawUri, string label)
    {
        ArgumentGuard.NotNullOrEmpty(baseRawUri);

        string path = $"{_settings.Name}/{_settings.Environment}";

        if (!string.IsNullOrWhiteSpace(label))
        {
            // If label contains slash, replace it
            if (label.Contains('/'))
            {
                label = label.Replace("/", "(_)");
            }

#pragma warning disable S3900 // Arguments of public methods should be validated against null
            path = $"{path}/{label.Trim()}";
#pragma warning restore S3900 // Arguments of public methods should be validated against null
        }

        if (!baseRawUri.EndsWith("/", StringComparison.Ordinal))
        {
            path = $"/{path}";
        }

        return baseRawUri + path;
    }

    /// <summary>
    /// Adds values from a <see cref="PropertySource" /> to the provided dictionary.
    /// </summary>
    /// <param name="source">
    /// The property source to read from.
    /// </param>
    /// <param name="data">
    /// The dictionary to add the property source to.
    /// </param>
    protected void AddPropertySource(PropertySource source, IDictionary<string, string> data)
    {
        ArgumentGuard.NotNull(data);

        if (source?.Source == null)
        {
            return;
        }

        foreach (KeyValuePair<string, object> pair in source.Source)
        {
            try
            {
                string key = ConvertKey(pair.Key);
                string value = ConvertValue(pair.Value);
                data[key] = value;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Config Server exception, property: {key}={type}", pair.Key, pair.Value.GetType());
            }
        }
    }

    protected internal virtual string ConvertKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }

        IEnumerable<string> split = Split(key);
        var sb = new StringBuilder();

        foreach (string part in split)
        {
            string keyPart = ConvertArrayKey(part);
            sb.Append(keyPart);
            sb.Append(ConfigurationPath.KeyDelimiter);
        }

        return sb.ToString(0, sb.Length - 1);
    }

    protected virtual IEnumerable<string> Split(string source)
    {
        ArgumentGuard.NotNull(source);

        var result = new List<string>();

        int segmentStart = 0;

        for (int i = 0; i < source.Length; i++)
        {
            bool readEscapeChar = false;

            if (source[i] == EscapeChar)
            {
                readEscapeChar = true;
                i++;
            }

            if (!readEscapeChar && source[i] == DotDelimiterChar)
            {
                result.Add(UnEscapeString(source.Substring(segmentStart, i - segmentStart)));
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
            return src.Replace(EscapeString + DotDelimiterString, DotDelimiterString).Replace(EscapeString + EscapeString, EscapeString);
        }
    }

    protected internal virtual string ConvertArrayKey(string key)
    {
        return ArrayRegex.Replace(key, match => match.Value.Replace("[", ":").Replace("]", string.Empty));
    }

    protected virtual string ConvertValue(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Encodes the username and password for a http request.
    /// </summary>
    /// <param name="user">
    /// The username.
    /// </param>
    /// <param name="password">
    /// The password.
    /// </param>
    /// <returns>
    /// Encoded username with password.
    /// </returns>
    protected internal string GetEncoded(string user, string password)
    {
        return HttpClientHelper.GetEncodedUserPassword(user, password);
    }

    protected virtual void RenewToken()
    {
        _ = new Timer(_ => RefreshVaultTokenAsync().GetAwaiter().GetResult(), null, TimeSpan.FromMilliseconds(_settings.TokenRenewRate),
            TimeSpan.FromMilliseconds(_settings.TokenRenewRate));
    }

    /// <summary>
    /// Conduct the OAuth2 client_credentials grant flow returning a task that can be used to obtain the results.
    /// </summary>
    /// <returns>
    /// The task object representing asynchronous operation.
    /// </returns>
    protected async Task<string> FetchAccessTokenAsync()
    {
        if (string.IsNullOrEmpty(_settings.AccessTokenUri))
        {
            return null;
        }

        return await HttpClientHelper.GetAccessTokenAsync(_settings.AccessTokenUri, _settings.ClientId, _settings.ClientSecret, _settings.Timeout,
            _settings.ValidateCertificates, HttpClient, Logger);
    }

    // fire and forget
    protected async Task RefreshVaultTokenAsync()
    {
        if (string.IsNullOrEmpty(Settings.Token))
        {
            return;
        }

        string obscuredToken = $"{Settings.Token[..4]}[*]{Settings.Token[^4..]}";

        try
        {
            HttpClient ??= GetConfiguredHttpClient(Settings);

            string uri = GetVaultRenewUri();
            HttpRequestMessage message = GetVaultRenewMessage(uri);

            Logger.LogInformation("Renewing Vault token {token} for {ttl} milliseconds at Uri {uri}", obscuredToken, Settings.TokenTtl, uri);

            using HttpResponseMessage response = await HttpClient.SendAsync(message);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogWarning("Renewing Vault token {token} returned status: {status}", obscuredToken, response.StatusCode);
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unable to renew Vault token {token}. Is the token invalid or expired?", obscuredToken);
        }
    }

    protected virtual string GetVaultRenewUri()
    {
        string rawUri = Settings.RawUris[0];

        if (!rawUri.EndsWith("/", StringComparison.Ordinal))
        {
            rawUri += "/";
        }

        return rawUri + VaultRenewPath;
    }

    protected virtual HttpRequestMessage GetVaultRenewMessage(string requestUri)
    {
        HttpRequestMessage request = HttpClientHelper.GetRequestMessage(HttpMethod.Post, requestUri, () => FetchAccessTokenAsync().GetAwaiter().GetResult());

        if (!string.IsNullOrEmpty(Settings.Token))
        {
            request.Headers.Add(VaultTokenHeader, Settings.Token);
        }

        int renewTtlInSeconds = Settings.TokenTtl / 1000;
        string json = $"{{\"increment\":{renewTtlInSeconds}}}";

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Content = content;
        return request;
    }

    protected internal bool IsDiscoveryFirstEnabled()
    {
        IConfigurationSection clientConfigSection = _configuration.GetSection(ConfigurationPrefix);
        return clientConfigSection.GetValue("discovery:enabled", _settings.DiscoveryEnabled);
    }

    /// <summary>
    /// Creates an appropriately configured HttpClient that will be used in communicating with the Spring Cloud Configuration Server.
    /// </summary>
    /// <param name="settings">
    /// The settings used in configuring the HttpClient.
    /// </param>
    /// <returns>
    /// The HttpClient used by the provider.
    /// </returns>
    protected virtual HttpClient GetConfiguredHttpClient(ConfigServerClientSettings settings)
    {
        ArgumentGuard.NotNull(settings);

        var clientHandler = new HttpClientHandler();

        if (settings.ClientCertificate != null)
        {
            clientHandler.ClientCertificates.Add(settings.ClientCertificate);
        }

        HttpClient client = HttpClientHelper.GetHttpClient(settings.ValidateCertificates, clientHandler, settings.Timeout);

        if (settings.Headers != null)
        {
            foreach (KeyValuePair<string, string> header in settings.Headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        return client;
    }

    private IConfiguration WrapWithPlaceholderResolver(IConfiguration configuration)
    {
        var root = (IConfigurationRoot)configuration;

        if (root.Providers.LastOrDefault() is PlaceholderResolverProvider)
        {
            return configuration;
        }

        return new ConfigurationRoot(new List<IConfigurationProvider>
        {
            new PlaceholderResolverProvider(root.Providers.ToList())
        });
    }

    private bool IsContinueExceptionType(Exception exception)
    {
        if (exception is TaskCanceledException)
        {
            return true;
        }

        if (exception is HttpRequestException && exception.InnerException is SocketException)
        {
            return true;
        }

        return false;
    }
}
