// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
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
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Http;
using Steeltoe.Common.Http.HttpClientPooling;

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
    /// <see cref="ConfigServerClientOptions" />) are found. (e.g. spring:cloud:config:env, spring:cloud:config:uri, spring:cloud:config:enabled, etc.)
    /// </summary>
    private const string ConfigurationPrefix = "spring:cloud:config";

    internal const string TokenHeader = "X-Config-Token";

    private static readonly string[] EmptyLabels = [string.Empty];

    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly bool _hasConfiguration;
    private readonly bool _ownsHttpClientHandler;
    private HttpClientHandler? _httpClientHandler;

    private ConfigServerDiscoveryService? _configServerDiscoveryService;
    private Timer? _refreshTimer;

    internal static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    internal IDictionary<string, string?> Properties => Data;
    internal ILogger Logger { get; }

    /// <summary>
    /// Gets the configuration settings the provider uses when accessing the server.
    /// </summary>
    public ConfigServerClientOptions Options { get; }

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
        : this(source.DefaultOptions, source.Configuration, null, loggerFactory)
    {
    }

    internal ConfigServerConfigurationProvider(ConfigServerClientOptions options, IConfiguration? configuration, HttpClientHandler? httpClientHandler,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
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

        Options = options;

        if (httpClientHandler == null)
        {
            _httpClientHandler = new HttpClientHandler();
            _ownsHttpClientHandler = true;
        }
        else
        {
            _httpClientHandler = httpClientHandler;
        }

        OnSettingsChanged();
    }

    private void OnSettingsChanged()
    {
        TimeSpan existingPollingInterval = Options.PollingInterval;

        if (_hasConfiguration)
        {
            ConfigurationSettingsHelper.Initialize(ConfigurationPrefix, Options, _configuration);
            _configuration.GetReloadToken().RegisterChangeCallback(_ => OnSettingsChanged(), null);
        }

        if (Options.PollingInterval == TimeSpan.Zero || !Options.Enabled)
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }
        else if (Options.Enabled)
        {
            if (_refreshTimer == null)
            {
                _refreshTimer = new Timer(_ => DoPolledLoadAsync().GetAwaiter().GetResult(), null, TimeSpan.Zero, Options.PollingInterval);
            }
            else if (existingPollingInterval != Options.PollingInterval)
            {
                _refreshTimer.Change(TimeSpan.Zero, Options.PollingInterval);
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
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Could not reload configuration during polling");
        }
    }

    /// <summary>
    /// Loads configuration data from the Spring Cloud Configuration Server as specified by the <see cref="Options" />.
    /// </summary>
    public override void Load()
    {
        LoadInternalAsync(true, CancellationToken.None).GetAwaiter().GetResult();
    }

    internal async Task<ConfigEnvironment?> LoadInternalAsync(bool updateDictionary, CancellationToken cancellationToken)
    {
        if (!Options.Enabled)
        {
            Logger.LogInformation("Config Server client disabled, did not fetch configuration!");
            return null;
        }

        if (IsDiscoveryFirstEnabled())
        {
            _configServerDiscoveryService ??= new ConfigServerDiscoveryService(_configuration, Options, _loggerFactory);
            await DiscoverServerInstancesAsync(_configServerDiscoveryService, cancellationToken);
        }

        // Adds client settings (e.g. spring:cloud:config:uri, etc.) to the Data dictionary
        AddConfigServerClientOptions();

        string logUri = string.Join(',', Options.GetUris().Select(uri => new Uri(uri).ToMaskedString()));

        if (Options is { Retry.Enabled: true, FailFast: true })
        {
            int attempts = 0;
            int backOff = Options.Retry.InitialInterval;

            do
            {
                Logger.LogInformation("Fetching configuration from server at: {Uri}", logUri);

                try
                {
                    return await DoLoadAsync(updateDictionary, cancellationToken);
                }
                catch (ConfigServerException exception)
                {
                    Logger.LogInformation(exception, "Failed fetching configuration from server at: {Uri}.", logUri);
                    attempts++;

                    if (attempts < Options.Retry.MaxAttempts)
                    {
                        Thread.CurrentThread.Join(backOff);
                        int nextBackOff = (int)(backOff * Options.Retry.Multiplier);
                        backOff = Math.Min(nextBackOff, Options.Retry.MaxInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            while (true);
        }

        Logger.LogInformation("Fetching configuration from server at: {Uri}", logUri);
        return await DoLoadAsync(updateDictionary, cancellationToken);
    }

    internal async Task<ConfigEnvironment?> DoLoadAsync(bool updateDictionary, CancellationToken cancellationToken)
    {
        Exception? error = null;

        // Get arrays of Config Server uris to check
        IList<string> uris = Options.GetUris();

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
                    Logger.LogInformation("Located environment name: {Name}, profiles: {Profiles}, labels: {Label}, version: {Version}, state: {State}",
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
                        AddConfigServerClientOptions(data);

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

        if (Options.FailFast)
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
        if (string.IsNullOrWhiteSpace(Options.Label))
        {
            return EmptyLabels;
        }

        return Options.Label.Split(CommaDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private async Task DiscoverServerInstancesAsync(ConfigServerDiscoveryService configServerDiscoveryService, CancellationToken cancellationToken)
    {
        IServiceInstance[] instances = (await configServerDiscoveryService.GetConfigServerInstancesAsync(cancellationToken)).ToArray();

        if (!instances.Any())
        {
            if (Options.FailFast)
            {
                throw new ConfigServerException("Could not locate Config Server via discovery, are you missing a Discovery service assembly?");
            }

            return;
        }

        UpdateSettingsFromDiscovery(instances, Options);
    }

    internal void UpdateSettingsFromDiscovery(IEnumerable<IServiceInstance> instances, ConfigServerClientOptions options)
    {
        var endpoints = new StringBuilder();

        foreach (IServiceInstance instance in instances)
        {
            string uri = instance.Uri.ToString();
            IReadOnlyDictionary<string, string?> metaData = instance.Metadata;

            if (metaData.Count > 0)
            {
                if (metaData.TryGetValue("password", out string? password))
                {
                    metaData.TryGetValue("user", out string? username);
                    username ??= "user";
                    options.Username = username;
                    options.Password = password;
                }

                if (metaData.TryGetValue("configPath", out string? path) && path != null)
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
            options.Uri = uris;
        }
    }

    internal async Task ProvideRuntimeReplacementsAsync(ICollection<IDiscoveryClient> discoveryClientsFromServiceProvider, CancellationToken cancellationToken)
    {
        if (_configServerDiscoveryService is not null)
        {
            await _configServerDiscoveryService.ProvideRuntimeReplacementsAsync(discoveryClientsFromServiceProvider, cancellationToken);
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
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The HttpRequestMessage built from the path.
    /// </returns>
    internal async Task<HttpRequestMessage> GetRequestMessageAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        var uriWithoutUserInfo = new Uri(requestUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped));
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uriWithoutUserInfo);

        if (requestUri.TryGetUsernamePassword(out string? username, out string? password) && password.Length > 0)
        {
            Logger.LogDebug("Adding credentials from '{RequestUri}' to Authorization header.", requestUri.ToMaskedString());

            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));
        }
        else
        {
            if (!string.IsNullOrEmpty(Options.AccessTokenUri))
            {
                using HttpClient httpClient = CreateHttpClient(Options);
                var accessTokenUri = new Uri(Options.AccessTokenUri);

                string accessToken = await httpClient.GetAccessTokenAsync(accessTokenUri, Options.ClientId, Options.ClientSecret, cancellationToken);

                Logger.LogDebug("Fetched access token from '{AccessTokenUri}'.", accessTokenUri.ToMaskedString());
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        if (!string.IsNullOrEmpty(Options.Token) && Options.Uri != null && !Options.IsMultiServerConfiguration)
        {
            if (!Options.DisableTokenRenewal)
            {
                RenewToken();
            }

            requestMessage.Headers.Add(TokenHeader, Options.Token);
        }

        return requestMessage;
    }

    /// <summary>
    /// Adds the client settings for the Configuration Server to the data dictionary.
    /// </summary>
    internal void AddConfigServerClientOptions()
    {
        Dictionary<string, string?> data = Data.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        AddConfigServerClientOptions(data);

        Data = data;
    }

    /// <summary>
    /// Adds the client settings for the Configuration Server to the data dictionary.
    /// </summary>
    /// <param name="data">
    /// The client settings to add.
    /// </param>
    private void AddConfigServerClientOptions(IDictionary<string, string?> data)
    {
        ArgumentGuard.NotNull(data);

        data["spring:cloud:config:enabled"] = Options.Enabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:failFast"] = Options.FailFast.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:env"] = Options.Environment;
        data["spring:cloud:config:label"] = Options.Label;
        data["spring:cloud:config:name"] = Options.Name;
        data["spring:cloud:config:password"] = Options.Password;
        data["spring:cloud:config:uri"] = Options.Uri;
        data["spring:cloud:config:username"] = Options.Username;
        data["spring:cloud:config:token"] = Options.Token;
        data["spring:cloud:config:timeout"] = Options.Timeout.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:validate_certificates"] = Options.ValidateCertificates.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:enabled"] = Options.Retry.Enabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:maxAttempts"] = Options.Retry.MaxAttempts.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:initialInterval"] = Options.Retry.InitialInterval.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:maxInterval"] = Options.Retry.MaxInterval.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:multiplier"] = Options.Retry.Multiplier.ToString(CultureInfo.InvariantCulture);

        data["spring:cloud:config:access_token_uri"] = Options.AccessTokenUri;
        data["spring:cloud:config:client_secret"] = Options.ClientSecret;
        data["spring:cloud:config:client_id"] = Options.ClientId;
        data["spring:cloud:config:tokenTtl"] = Options.TokenTtl.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:tokenRenewRate"] = Options.TokenRenewRate.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:disableTokenRenewal"] = Options.DisableTokenRenewal.ToString(CultureInfo.InvariantCulture);

        data["spring:cloud:config:discovery:enabled"] = Options.Discovery.Enabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:discovery:serviceId"] = Options.Discovery.ServiceId?.ToString(CultureInfo.InvariantCulture);

        data["spring:cloud:config:health:enabled"] = Options.Health.Enabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:health:timeToLive"] = Options.Health.TimeToLive.ToString(CultureInfo.InvariantCulture);
    }

    internal async Task<ConfigEnvironment?> RemoteLoadAsync(IEnumerable<string> requestUris, string? label, CancellationToken cancellationToken)
    {
        // Get client if not already set
        using HttpClient httpClient = CreateHttpClient(Options);

        Exception? error = null;

        foreach (string requestUri in requestUris)
        {
            // Make Config Server URI from settings
            Uri uri = BuildConfigServerUri(requestUri, label);

            // Get the request message
            HttpRequestMessage request = await GetRequestMessageAsync(uri, cancellationToken);

            // Invoke Config Server
            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                Logger.LogInformation("Config Server returned status: {StatusCode} invoking path: {RequestUri}", response.StatusCode, uri.ToMaskedString());

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }

                    // Throw if status >= 400
                    if (response.StatusCode >= HttpStatusCode.BadRequest)
                    {
                        throw new HttpRequestException($"Config Server returned status: {response.StatusCode} invoking path: {uri.ToMaskedString()}");
                    }

                    return null;
                }

                return await response.Content.ReadFromJsonAsync<ConfigEnvironment>(SerializerOptions, cancellationToken);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                error = exception;

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
    /// <param name="serverUri">
    /// Base server uri to use.
    /// </param>
    /// <param name="label">
    /// The label to add.
    /// </param>
    /// <returns>
    /// The request URI for the Configuration Server.
    /// </returns>
    internal Uri BuildConfigServerUri(string serverUri, string? label)
    {
        ArgumentGuard.NotNullOrEmpty(serverUri);

        var uriBuilder = new UriBuilder(new Uri(serverUri));

        if (!string.IsNullOrEmpty(Options.Username))
        {
            uriBuilder.UserName = Options.Username;
        }

        if (!string.IsNullOrEmpty(Options.Password))
        {
            uriBuilder.Password = Options.Password;
        }

        string pathSuffix = $"{Options.Name}/{Options.Environment}";

        if (!string.IsNullOrWhiteSpace(label))
        {
            // If label contains slash, replace it
            if (label.Contains('/'))
            {
                label = label.Replace("/", "(_)", StringComparison.Ordinal);
            }

            pathSuffix = $"{pathSuffix}/{label.Trim()}";
        }

        if (!uriBuilder.Path.EndsWith('/'))
        {
            pathSuffix = $"/{pathSuffix}";
        }

        uriBuilder.Path += pathSuffix;

        return uriBuilder.Uri;
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
                Logger.LogError(exception, "Config Server exception, property: {Key}={Type}", pair.Key, pair.Value.GetType());
            }
        }
    }

    private string? ConvertValue(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private void RenewToken()
    {
        _ = new Timer(_ => RefreshVaultTokenAsync(CancellationToken.None).GetAwaiter().GetResult(), null, TimeSpan.FromMilliseconds(Options.TokenRenewRate),
            TimeSpan.FromMilliseconds(Options.TokenRenewRate));
    }

    // fire and forget
    internal async Task RefreshVaultTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Options.Token))
        {
            return;
        }

        string obscuredToken = $"{Options.Token[..4]}[*]{Options.Token[^4..]}";

        try
        {
            using HttpClient httpClient = CreateHttpClient(Options);

            Uri uri = GetVaultRenewUri();
            HttpRequestMessage message = await GetVaultRenewRequestMessageAsync(uri, cancellationToken);

            Logger.LogInformation("Renewing Vault token {Token} for {Ttl} milliseconds at Uri {Uri}", obscuredToken, Options.TokenTtl, uri.ToMaskedString());

            using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogWarning("Renewing Vault token {Token} returned status: {Status}", obscuredToken, response.StatusCode);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            Logger.LogError(exception, "Unable to renew Vault token {Token}. Is the token invalid or expired?", obscuredToken);
        }
    }

    private Uri GetVaultRenewUri()
    {
        string baseUri = Options.Uri!.Split(',')[0].Trim();

        if (!baseUri.EndsWith('/'))
        {
            baseUri += '/';
        }

        return new Uri(baseUri + VaultRenewPath, UriKind.RelativeOrAbsolute);
    }

    private async Task<HttpRequestMessage> GetVaultRenewRequestMessageAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        var uriWithoutUserInfo = new Uri(requestUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped));
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, uriWithoutUserInfo);

        if (!string.IsNullOrEmpty(Options.AccessTokenUri))
        {
            using HttpClient httpClient = CreateHttpClient(Options);
            var accessTokenUri = new Uri(Options.AccessTokenUri);

            string accessToken = await httpClient.GetAccessTokenAsync(accessTokenUri, Options.ClientId, Options.ClientSecret, cancellationToken);

            Logger.LogDebug("Fetched access token from '{AccessTokenUri}'.", accessTokenUri.ToMaskedString());
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (!string.IsNullOrEmpty(Options.Token))
        {
            requestMessage.Headers.Add(VaultTokenHeader, Options.Token);
        }

        int renewTtlInSeconds = Options.TokenTtl / 1000;
        string json = $"{{\"increment\":{renewTtlInSeconds}}}";
        requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return requestMessage;
    }

    internal bool IsDiscoveryFirstEnabled()
    {
        IConfigurationSection clientConfigSection = _configuration.GetSection(ConfigurationPrefix);
        return clientConfigSection.GetValue("discovery:enabled", Options.Discovery.Enabled);
    }

    /// <summary>
    /// Creates an appropriately configured HttpClient that can be used in communicating with the Spring Cloud Configuration Server.
    /// </summary>
    /// <param name="options">
    /// The settings used to configure the HttpClient.
    /// </param>
    /// <returns>
    /// The HttpClient used by the provider.
    /// </returns>
    internal HttpClient CreateHttpClient(ConfigServerClientOptions options)
    {
        ArgumentGuard.NotNull(options);
        ObjectDisposedException.ThrowIf(_httpClientHandler == null, this);

        var clientCertificateConfigurer = new ClientCertificateHttpClientHandlerConfigurer(OptionsMonitorWrapper.Create(options.ClientCertificate));
        clientCertificateConfigurer.Configure(_httpClientHandler);

        var validateCertificatesHandler = new ValidateCertificatesHttpClientHandlerConfigurer<ConfigServerClientOptions>(OptionsMonitorWrapper.Create(options));
        validateCertificatesHandler.Configure(_httpClientHandler);

        var httpClient = new HttpClient(_httpClientHandler, false);
        httpClient.ConfigureForSteeltoe(options.HttpTimeout);

        foreach ((string headerName, string headerValue) in options.Headers)
        {
            httpClient.DefaultRequestHeaders.Add(headerName, headerValue);
        }

        return httpClient;
    }

    private static bool IsSocketError(Exception exception)
    {
        return exception is HttpRequestException && exception.InnerException is SocketException;
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;

        if (_ownsHttpClientHandler)
        {
            _httpClientHandler?.Dispose();
        }

        _httpClientHandler = null;
    }
}
