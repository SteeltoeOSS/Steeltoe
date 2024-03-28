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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Http;
using Steeltoe.Discovery;

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// A Spring Cloud Config Server based <see cref="ConfigurationProvider" />.
/// </summary>
internal sealed class ConfigServerConfigurationProvider : ConfigurationProvider, IDisposable
{
    private const string VaultRenewPath = "vault/v1/auth/token/renew-self";
    private const string VaultTokenHeader = "X-Vault-Token";
    private const char CommaDelimiter = ',';

    /// <summary>
    /// The <see cref="IConfigurationSection" /> prefix under which all Spring Cloud Config Server configuration settings (
    /// <see cref="ConfigServerClientSettings" />) are found. (e.g. spring:cloud:config:env, spring:cloud:config:uri, spring:cloud:config:enabled, etc.)
    /// </summary>
    private const string ConfigurationPrefix = "spring:cloud:config";

    internal const string TokenHeader = "X-Config-Token";

    private static readonly string[] EmptyLabels = [string.Empty];

    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly bool _hasConfiguration;

    private ConfigServerDiscoveryService? _configServerDiscoveryService;
    private Timer? _refreshTimer;

    internal static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    internal IDictionary<string, string?> Properties => Data;
    internal ILogger Logger { get; }

    internal HttpClient? HttpClient { get; private set; }

    /// <summary>
    /// Gets the configuration settings the provider uses when accessing the server.
    /// </summary>
    public ConfigServerClientSettings Settings { get; }

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
        : this(settings, null, null, loggerFactory)
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
        : this(settings, null, httpClient, loggerFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class from a <see cref="ConfigServerConfigurationSource" />.
    /// </summary>
    /// <param name="source">
    /// The <see cref="ConfigServerConfigurationSource" /> the provider uses when accessing the server.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerConfigurationSource source, ILoggerFactory loggerFactory)
        : this(source.DefaultSettings, source.Configuration, null, loggerFactory)
    {
    }

    private ConfigServerConfigurationProvider(ConfigServerClientSettings settings, IConfiguration? configuration, HttpClient? httpClient,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(settings);
        ArgumentGuard.NotNull(loggerFactory);

        _loggerFactory = loggerFactory;
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

        Settings = settings;

        if (httpClient != null)
        {
            HttpClient = httpClient;
        }

        OnSettingsChanged();

        HttpClient ??= GetConfiguredHttpClient(Settings);
    }

    private void OnSettingsChanged()
    {
        TimeSpan existingPollingInterval = Settings.PollingInterval;

        if (_hasConfiguration)
        {
            ConfigurationSettingsHelper.Initialize(ConfigurationPrefix, Settings, _configuration);
            _configuration.GetReloadToken().RegisterChangeCallback(_ => OnSettingsChanged(), null);
        }

        if (Settings.PollingInterval == TimeSpan.Zero || !Settings.Enabled)
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }
        else if (Settings.Enabled)
        {
            if (_refreshTimer == null)
            {
                _refreshTimer = new Timer(_ => DoPolledLoadAsync().GetAwaiter().GetResult(), null, TimeSpan.Zero, Settings.PollingInterval);
            }
            else if (existingPollingInterval != Settings.PollingInterval)
            {
                _refreshTimer.Change(TimeSpan.Zero, Settings.PollingInterval);
            }
        }
    }

    /// <remarks>
    /// DoPolledLoad is called by a Timer callback, so must catch all exceptions.
    /// </remarks>
    private async Task DoPolledLoadAsync()
    {
        try
        {
            await DoLoadAsync(true, CancellationToken.None);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Could not reload configuration during polling");
        }
    }

    /// <summary>
    /// Loads configuration data from the Spring Cloud Configuration Server as specified by the <see cref="Settings" />.
    /// </summary>
    public override void Load()
    {
        LoadInternalAsync(true, CancellationToken.None).GetAwaiter().GetResult();
    }

    internal async Task<ConfigEnvironment?> LoadInternalAsync(bool updateDictionary, CancellationToken cancellationToken)
    {
        if (!Settings.Enabled)
        {
            Logger.LogInformation("Config Server client disabled, did not fetch configuration!");
            return null;
        }

        if (IsDiscoveryFirstEnabled())
        {
            _configServerDiscoveryService ??= new ConfigServerDiscoveryService(_configuration, Settings, _loggerFactory);
            await DiscoverServerInstancesAsync(_configServerDiscoveryService, cancellationToken);
        }

        // Adds client settings (e.g spring:cloud:config:uri, etc) to the Data dictionary
        AddConfigServerClientSettings();

        if (Settings is { RetryEnabled: true, FailFast: true })
        {
            int attempts = 0;
            int backOff = Settings.RetryInitialInterval;

            do
            {
                Logger.LogInformation("Fetching configuration from server at: {uri}", Settings.Uri);

                try
                {
                    return await DoLoadAsync(updateDictionary, cancellationToken);
                }
                catch (ConfigServerException e)
                {
                    Logger.LogInformation(e, "Failed fetching configuration from server at: {uri}.", Settings.Uri);
                    attempts++;

                    if (attempts < Settings.RetryAttempts)
                    {
                        Thread.CurrentThread.Join(backOff);
                        int nextBackOff = (int)(backOff * Settings.RetryMultiplier);
                        backOff = Math.Min(nextBackOff, Settings.RetryMaxInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            while (true);
        }

        Logger.LogInformation("Fetching configuration from server at: {uri}", Settings.Uri);
        return await DoLoadAsync(updateDictionary, cancellationToken);
    }

    internal async Task<ConfigEnvironment?> DoLoadAsync(bool updateDictionary, CancellationToken cancellationToken)
    {
        Exception? error = null;

        // Get arrays of Config Server uris to check
        IList<string> uris = Settings.GetUris();

        try
        {
            foreach (string label in GetLabels())
            {
                if (uris.Count > 1)
                {
                    Logger.LogInformation("Multiple Config Server Uris listed.");
                }

                // Invoke Config Servers
                ConfigEnvironment? env = await RemoteLoadAsync(uris, label, cancellationToken);

                // Update configuration Data dictionary with any results
                if (env != null)
                {
                    Logger.LogInformation("Located environment name: {name}, profiles: {profiles}, labels: {label}, version: {version}, state: {state}",
                        env.Name, env.Profiles, env.Label, env.Version, env.State);

                    if (updateDictionary)
                    {
                        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

                        if (!string.IsNullOrEmpty(env.State))
                        {
                            data["spring:cloud:config:client:state"] = env.State;
                        }

                        if (!string.IsNullOrEmpty(env.Version))
                        {
                            data["spring:cloud:config:client:version"] = env.Version;
                        }

                        IList<PropertySource> sources = env.PropertySources;
                        int index = sources.Count - 1;

                        for (; index >= 0; index--)
                        {
                            AddPropertySource(sources[index], data);
                        }

                        // Adds client settings (e.g. spring:cloud:config:uri, etc.) back to the (new) Data dictionary
                        AddConfigServerClientSettings(data);

                        if (!AreDictionariesEqual(Data, data))
                        {
                            Data = data;
                            OnReload();
                        }
                    }

                    return env;
                }
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            error = exception;
        }

        Logger.LogWarning(error, "Could not locate PropertySource");

        if (Settings.FailFast)
        {
            throw new ConfigServerException("Could not locate PropertySource, fail fast property is set, failing", error);
        }

        return null;
    }

    private static bool AreDictionariesEqual<TKey, TValue>(IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
    {
        return first.Count == second.Count && first.Keys.All(firstKey =>
            second.ContainsKey(firstKey) && EqualityComparer<TValue>.Default.Equals(first[firstKey], second[firstKey]));
    }

    internal string[] GetLabels()
    {
        if (string.IsNullOrWhiteSpace(Settings.Label))
        {
            return EmptyLabels;
        }

        return Settings.Label.Split(CommaDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private async Task DiscoverServerInstancesAsync(ConfigServerDiscoveryService configServerDiscoveryService, CancellationToken cancellationToken)
    {
        IServiceInstance[] instances = (await configServerDiscoveryService.GetConfigServerInstancesAsync(cancellationToken)).ToArray();

        if (!instances.Any())
        {
            if (Settings.FailFast)
            {
                throw new ConfigServerException("Could not locate Config Server via discovery, are you missing a Discovery service assembly?");
            }

            return;
        }

        UpdateSettingsFromDiscovery(instances, Settings);
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
                if (metaData.TryGetValue("password", out string? password))
                {
                    metaData.TryGetValue("user", out string? username);
                    username ??= "user";
                    settings.Username = username;
                    settings.Password = password;
                }

                if (metaData.TryGetValue("configPath", out string? path))
                {
                    if (uri.EndsWith('/') && path.StartsWith('/'))
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

    internal async Task ProvideRuntimeReplacementsAsync(IDiscoveryClient? discoveryClientFromDi, CancellationToken cancellationToken)
    {
        if (_configServerDiscoveryService is not null)
        {
            await _configServerDiscoveryService.ProvideRuntimeReplacementsAsync(discoveryClientFromDi, cancellationToken);
        }
    }

    internal async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        if (_configServerDiscoveryService is not null)
        {
            await _configServerDiscoveryService.ShutdownAsync(cancellationToken);
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
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The HttpRequestMessage built from the path.
    /// </returns>
    internal async Task<HttpRequestMessage> GetRequestMessageAsync(Uri requestUri, string? username, string? password, CancellationToken cancellationToken)
    {
        HttpRequestMessage request = string.IsNullOrEmpty(Settings.AccessTokenUri)
            ? HttpClientHelper.GetRequestMessage(HttpMethod.Get, requestUri, username, password)
            : await HttpClientHelper.GetRequestMessageAsync(HttpMethod.Get, requestUri, FetchAccessTokenAsync, cancellationToken);

        if (!string.IsNullOrEmpty(Settings.Token) && Settings.Uri != null && !ConfigServerClientSettings.IsMultiServerConfiguration(Settings.Uri))
        {
            if (!Settings.DisableTokenRenewal)
            {
                RenewToken();
            }

            request.Headers.Add(TokenHeader, Settings.Token);
        }

        return request;
    }

    /// <summary>
    /// Adds the client settings for the Configuration Server to the Data dictionary.
    /// </summary>
    internal void AddConfigServerClientSettings()
    {
        Dictionary<string, string?> data = Data.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        AddConfigServerClientSettings(data);

        Data = data;
    }

    /// <summary>
    /// Adds the client settings for the Configuration Server to the data dictionary.
    /// </summary>
    /// <param name="data">
    /// The client settings to add.
    /// </param>
    private void AddConfigServerClientSettings(IDictionary<string, string?> data)
    {
        ArgumentGuard.NotNull(data);

        data["spring:cloud:config:enabled"] = Settings.Enabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:failFast"] = Settings.FailFast.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:env"] = Settings.Environment;
        data["spring:cloud:config:label"] = Settings.Label;
        data["spring:cloud:config:name"] = Settings.Name;
        data["spring:cloud:config:password"] = Settings.Password;
        data["spring:cloud:config:uri"] = Settings.Uri;
        data["spring:cloud:config:username"] = Settings.Username;
        data["spring:cloud:config:token"] = Settings.Token;
        data["spring:cloud:config:timeout"] = Settings.Timeout.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:validate_certificates"] = Settings.ValidateCertificates.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:enabled"] = Settings.RetryEnabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:maxAttempts"] = Settings.RetryAttempts.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:initialInterval"] = Settings.RetryInitialInterval.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:maxInterval"] = Settings.RetryMaxInterval.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:multiplier"] = Settings.RetryMultiplier.ToString(CultureInfo.InvariantCulture);

        data["spring:cloud:config:access_token_uri"] = Settings.AccessTokenUri;
        data["spring:cloud:config:client_secret"] = Settings.ClientSecret;
        data["spring:cloud:config:client_id"] = Settings.ClientId;
        data["spring:cloud:config:tokenTtl"] = Settings.TokenTtl.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:tokenRenewRate"] = Settings.TokenRenewRate.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:disableTokenRenewal"] = Settings.DisableTokenRenewal.ToString(CultureInfo.InvariantCulture);

        data["spring:cloud:config:discovery:enabled"] = Settings.DiscoveryEnabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:discovery:serviceId"] = Settings.DiscoveryServiceId?.ToString(CultureInfo.InvariantCulture);

        data["spring:cloud:config:health:enabled"] = Settings.HealthEnabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:health:timeToLive"] = Settings.HealthTimeToLive.ToString(CultureInfo.InvariantCulture);
    }

    internal async Task<ConfigEnvironment?> RemoteLoadAsync(IEnumerable<string> requestUris, string? label, CancellationToken cancellationToken)
    {
        // Get client if not already set
        HttpClient ??= GetConfiguredHttpClient(Settings);

        Exception? error = null;

        foreach (string requestUri in requestUris)
        {
            // Get a Config Server uri and username/password to use
            string trimUri = requestUri.Trim();
            string? serverUri = ConfigServerClientSettings.GetRawUri(trimUri);
            string? username = Settings.GetUserName(trimUri);
            string? password = Settings.GetPassword(trimUri);

            // Make Config Server URI from settings
            var uri = new Uri(GetConfigServerUri(serverUri!, label), UriKind.RelativeOrAbsolute);

            // Get the request message
            HttpRequestMessage request = await GetRequestMessageAsync(uri, username, password, cancellationToken);

            // Invoke Config Server
            try
            {
                using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);

                Logger.LogInformation("Config Server returned status: {statusCode} invoking path: {requestUri}", response.StatusCode,
                    WebUtility.UrlEncode(requestUri));

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
                        throw new HttpRequestException(
                            $"Config Server returned status: {response.StatusCode} invoking path: {WebUtility.UrlEncode(requestUri)}");
                    }

                    return null;
                }

                return await response.Content.ReadFromJsonAsync<ConfigEnvironment>(SerializerOptions, cancellationToken);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                error = exception;

                Logger.LogError(exception, "Config Server exception, path: {requestUri}", WebUtility.UrlEncode(requestUri));

                if (IsSocketError(exception))
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
    internal string GetConfigServerUri(string baseRawUri, string? label)
    {
        ArgumentGuard.NotNullOrEmpty(baseRawUri);

        string path = $"{Settings.Name}/{Settings.Environment}";

        if (!string.IsNullOrWhiteSpace(label))
        {
            // If label contains slash, replace it
            if (label.Contains('/'))
            {
                label = label.Replace("/", "(_)", StringComparison.Ordinal);
            }

            path = $"{path}/{label.Trim()}";
        }

        if (!baseRawUri.EndsWith('/'))
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
    private void AddPropertySource(PropertySource? source, IDictionary<string, string?> data)
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
                string key = ConfigurationKeyConverter.AsDotNetConfigurationKey(pair.Key);
                string? value = ConvertValue(pair.Value);
                data[key] = value;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Config Server exception, property: {key}={type}", pair.Key, pair.Value.GetType());
            }
        }
    }

    private string? ConvertValue(object value)
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
    internal string GetEncoded(string user, string password)
    {
        return HttpClientHelper.GetEncodedUserPassword(user, password);
    }

    private void RenewToken()
    {
        _ = new Timer(_ => RefreshVaultTokenAsync(CancellationToken.None).GetAwaiter().GetResult(), null, TimeSpan.FromMilliseconds(Settings.TokenRenewRate),
            TimeSpan.FromMilliseconds(Settings.TokenRenewRate));
    }

    /// <summary>
    /// Conduct the OAuth2 client_credentials grant flow returning a task that can be used to obtain the results.
    /// </summary>
    /// <returns>
    /// The task object representing asynchronous operation.
    /// </returns>
    private async Task<string?> FetchAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Settings.AccessTokenUri))
        {
            return null;
        }

        return await HttpClientHelper.GetAccessTokenAsync(Settings.AccessTokenUri, Settings.ClientId, Settings.ClientSecret, Settings.Timeout,
            Settings.ValidateCertificates, HttpClient, Logger, cancellationToken);
    }

    // fire and forget
    private async Task RefreshVaultTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Settings.Token))
        {
            return;
        }

        string obscuredToken = $"{Settings.Token[..4]}[*]{Settings.Token[^4..]}";

        try
        {
            HttpClient ??= GetConfiguredHttpClient(Settings);

            Uri uri = GetVaultRenewUri();
            HttpRequestMessage message = await GetVaultRenewMessageAsync(uri, cancellationToken);

            Logger.LogInformation("Renewing Vault token {token} for {ttl} milliseconds at Uri {uri}", obscuredToken, Settings.TokenTtl, uri);

            using HttpResponseMessage response = await HttpClient.SendAsync(message, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogWarning("Renewing Vault token {token} returned status: {status}", obscuredToken, response.StatusCode);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            Logger.LogError(exception, "Unable to renew Vault token {token}. Is the token invalid or expired?", obscuredToken);
        }
    }

    private Uri GetVaultRenewUri()
    {
        string rawUri = Settings.RawUris[0];

        if (!rawUri.EndsWith('/'))
        {
            rawUri += '/';
        }

        return new Uri(rawUri + VaultRenewPath, UriKind.RelativeOrAbsolute);
    }

    private async Task<HttpRequestMessage> GetVaultRenewMessageAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        HttpRequestMessage request = await HttpClientHelper.GetRequestMessageAsync(HttpMethod.Post, requestUri, FetchAccessTokenAsync, cancellationToken);

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

    internal bool IsDiscoveryFirstEnabled()
    {
        IConfigurationSection clientConfigSection = _configuration.GetSection(ConfigurationPrefix);
        return clientConfigSection.GetValue("discovery:enabled", Settings.DiscoveryEnabled);
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
    private HttpClient GetConfiguredHttpClient(ConfigServerClientSettings settings)
    {
        ArgumentGuard.NotNull(settings);

        var clientHandler = new HttpClientHandler();

        if (settings.ClientCertificate != null)
        {
            clientHandler.ClientCertificates.Add(settings.ClientCertificate);
        }

        HttpClient client = HttpClientHelper.GetHttpClient(settings.ValidateCertificates, clientHandler, settings.Timeout);

        foreach (KeyValuePair<string, string> header in settings.Headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        return client;
    }

    private static bool IsSocketError(Exception exception)
    {
        return exception is HttpRequestException && exception.InnerException is SocketException;
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }
}
