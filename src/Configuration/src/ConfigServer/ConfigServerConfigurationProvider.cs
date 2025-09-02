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
using Microsoft.Extensions.Options;
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

    internal const string TokenHeader = "X-Config-Token";

    private static readonly string[] EmptyLabels = [string.Empty];

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly bool _hasConfiguration;
    private readonly bool _ownsHttpClientHandler;
    private readonly ConfigureConfigServerClientOptions _configurer;
    private HttpClientHandler? _httpClientHandler;

    private ConfigServerDiscoveryService? _configServerDiscoveryService;
    private Timer? _refreshTimer;
    private SemaphoreSlim? _timerTickLock = new(1, 1);

    internal static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
    };

    internal IDictionary<string, string?> Properties => Data;

    /// <summary>
    /// Gets the configuration settings the provider uses when accessing the server.
    /// </summary>
    public ConfigServerClientOptions ClientOptions { get; }

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

    internal ConfigServerConfigurationProvider(ConfigServerClientOptions clientOptions, IConfiguration? configuration, HttpClientHandler? httpClientHandler,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(clientOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _loggerFactory = loggerFactory;
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

        _configurer = new ConfigureConfigServerClientOptions(_configuration);

        ClientOptions = clientOptions;

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
        TimeSpan existingPollingInterval = ClientOptions.PollingInterval;

        _configurer.Configure(ClientOptions);

        if (_hasConfiguration)
        {
            _configuration.GetReloadToken().RegisterChangeCallback(_ => OnSettingsChanged(), null);
        }

        if (ClientOptions.PollingInterval == TimeSpan.Zero || !ClientOptions.Enabled)
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;
        }
        else if (ClientOptions.Enabled)
        {
            if (_refreshTimer == null)
            {
#pragma warning disable S4462 // Calls to "async" methods should not be blocking
                // Justification: Configuration sources and providers don't support async.
                _refreshTimer = new Timer(_ => DoPolledLoadAsync().GetAwaiter().GetResult(), null, TimeSpan.Zero, ClientOptions.PollingInterval);
#pragma warning restore S4462 // Calls to "async" methods should not be blocking
            }
            else if (existingPollingInterval != ClientOptions.PollingInterval)
            {
                _refreshTimer.Change(TimeSpan.Zero, ClientOptions.PollingInterval);
            }
        }
    }

    /// <remarks>
    /// DoPolledLoad is called by a Timer callback, so must catch all exceptions.
    /// </remarks>
    private async Task DoPolledLoadAsync()
    {
        _logger.LogTrace("Entering timer cycle");
        bool lockTaken = false;

        try
        {
            lockTaken = _timerTickLock != null && await _timerTickLock.WaitAsync(0);
        }
        catch (ObjectDisposedException)
        {
            // Ignore exception originating from potential race condition.
        }

        try
        {
            if (lockTaken)
            {
                _logger.LogTrace("Exclusive lock obtained");
                await DoLoadAsync(true, CancellationToken.None);
            }
            else
            {
                _logger.LogTrace("Previous cycle is still running, or already disposed; skipping this cycle");
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not reload configuration during polling");
        }
        finally
        {
            if (lockTaken)
            {
                _logger.LogTrace("Timer cycle completed, releasing exclusive lock");

                try
                {
                    _timerTickLock?.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Ignore exception originating from potential race condition.
                }
            }
        }
    }

    /// <summary>
    /// Loads configuration data from the Spring Cloud Configuration Server as specified by the <see cref="ClientOptions" />.
    /// </summary>
    public override void Load()
    {
#pragma warning disable S4462 // Calls to "async" methods should not be blocking
        // Justification: Configuration sources and providers don't support async.
        LoadInternalAsync(true, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore S4462 // Calls to "async" methods should not be blocking
    }

    internal async Task<ConfigEnvironment?> LoadInternalAsync(bool updateDictionary, CancellationToken cancellationToken)
    {
        if (!ClientOptions.Enabled)
        {
            _logger.LogInformation("Config Server client disabled, did not fetch configuration!");
            return null;
        }

        if (IsDiscoveryFirstEnabled())
        {
            _configServerDiscoveryService ??= new ConfigServerDiscoveryService(_configuration, ClientOptions, _loggerFactory);
            await DiscoverServerInstancesAsync(_configServerDiscoveryService, cancellationToken);
        }

        // Adds client settings (e.g. spring:cloud:config:uri, etc.) to the Data dictionary
        AddConfigServerClientOptions();

        if (ClientOptions is { Retry.Enabled: true, FailFast: true })
        {
            int attempts = 0;
            int backOff = ClientOptions.Retry.InitialInterval;

            do
            {
                _logger.LogDebug("Fetching configuration from server(s).");

                try
                {
                    return await DoLoadAsync(updateDictionary, cancellationToken);
                }
                catch (ConfigServerException exception)
                {
                    _logger.LogWarning(exception, "Failed fetching configuration from server(s).");
                    attempts++;

                    if (attempts < ClientOptions.Retry.MaxAttempts)
                    {
                        Thread.CurrentThread.Join(backOff);
                        int nextBackOff = (int)(backOff * ClientOptions.Retry.Multiplier);
                        backOff = Math.Min(nextBackOff, ClientOptions.Retry.MaxInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            while (true);
        }

        _logger.LogDebug("Fetching configuration from server(s).");
        return await DoLoadAsync(updateDictionary, cancellationToken);
    }

    internal async Task<ConfigEnvironment?> DoLoadAsync(bool updateDictionary, CancellationToken cancellationToken)
    {
        Exception? error = null;

        // Get list of Config Server uris to check
        List<Uri> uris = ClientOptions.GetUris();

        try
        {
            foreach (string label in GetLabels())
            {
                _logger.LogTrace("Processing label '{Label}'", label);

                if (uris.Count > 1)
                {
                    _logger.LogDebug("Multiple Config Server Uris listed.");
                }

                // Invoke Config Servers
                ConfigEnvironment? env = await RemoteLoadAsync(uris, label, cancellationToken);

                // Update configuration Data dictionary with any results
                if (env != null)
                {
                    _logger.LogDebug("Located environment name: {Name}, profiles: {Profiles}, labels: {Label}, version: {Version}, state: {State}", env.Name,
                        env.Profiles, env.Label, env.Version, env.State);

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
                            _logger.LogTrace("Data has changed, raising configuration reload");
                            Data = data;
                            OnReload();
                        }
                        else
                        {
                            _logger.LogTrace("Data has not changed");
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

        _logger.LogWarning(error, "Could not locate PropertySource");

        if (ClientOptions.FailFast)
        {
            _logger.LogTrace(error, "Failure with FailFast enabled, throwing ConfigServerException");
            throw new ConfigServerException("Could not locate PropertySource, fail fast property is set, failing", error);
        }

        return null;
    }

    private static bool AreDictionariesEqual<TKey, TValue>(IDictionary<TKey, TValue> first, Dictionary<TKey, TValue> second)
        where TKey : notnull
    {
        return first.Count == second.Count && first.Keys.All(firstKey =>
            second.ContainsKey(firstKey) && EqualityComparer<TValue>.Default.Equals(first[firstKey], second[firstKey]));
    }

    internal string[] GetLabels()
    {
        if (string.IsNullOrWhiteSpace(ClientOptions.Label))
        {
            return EmptyLabels;
        }

        return ClientOptions.Label.Split(CommaDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private async Task DiscoverServerInstancesAsync(ConfigServerDiscoveryService configServerDiscoveryService, CancellationToken cancellationToken)
    {
        IServiceInstance[] instances = (await configServerDiscoveryService.GetConfigServerInstancesAsync(cancellationToken)).ToArray();

        if (instances.Length == 0)
        {
            if (ClientOptions.FailFast)
            {
                throw new ConfigServerException("Could not locate Config Server via discovery, are you missing a Discovery service assembly?");
            }

            return;
        }

        UpdateSettingsFromDiscovery(instances, ClientOptions);
    }

    internal void UpdateSettingsFromDiscovery(IEnumerable<IServiceInstance> instances, ConfigServerClientOptions clientOptions)
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
                    clientOptions.Username = username;
                    clientOptions.Password = password;
                }

                if (metaData.TryGetValue("configPath", out string? path) && path != null)
                {
                    if (uri.EndsWith('/') && path.StartsWith('/'))
                    {
                        uri = uri[..^1];
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
            clientOptions.Uri = uris;
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
            _logger.LogDebug("Adding credentials from '{RequestUri}' to Authorization header.", requestUri.ToMaskedString());

            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));
        }
        else
        {
            if (!string.IsNullOrEmpty(ClientOptions.AccessTokenUri))
            {
                using HttpClient httpClient = CreateHttpClient(ClientOptions);
                var accessTokenUri = new Uri(ClientOptions.AccessTokenUri);

                string accessToken =
                    await httpClient.GetAccessTokenAsync(accessTokenUri, ClientOptions.ClientId, ClientOptions.ClientSecret, cancellationToken);

                _logger.LogDebug("Fetched access token from '{AccessTokenUri}'.", accessTokenUri.ToMaskedString());
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        if (!string.IsNullOrEmpty(ClientOptions.Token) && ClientOptions is { Uri: not null, IsMultiServerConfiguration: false })
        {
            if (!ClientOptions.DisableTokenRenewal)
            {
                RenewToken();
            }

            requestMessage.Headers.Add(TokenHeader, ClientOptions.Token);
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
    private void AddConfigServerClientOptions(Dictionary<string, string?> data)
    {
        data["spring:cloud:config:enabled"] = ClientOptions.Enabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:failFast"] = ClientOptions.FailFast.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:env"] = ClientOptions.Environment;
        data["spring:cloud:config:label"] = ClientOptions.Label;
        data["spring:cloud:config:name"] = ClientOptions.Name;
        data["spring:cloud:config:uri"] = ClientOptions.Uri;
        data["spring:cloud:config:username"] = ClientOptions.Username;
        data["spring:cloud:config:password"] = ClientOptions.Password;
        data["spring:cloud:config:token"] = ClientOptions.Token;
        data["spring:cloud:config:timeout"] = ClientOptions.Timeout.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:pollingInterval"] = ClientOptions.PollingInterval.ToString(null, CultureInfo.InvariantCulture);
        data["spring:cloud:config:validateCertificates"] = ClientOptions.ValidateCertificates.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:accessTokenUri"] = ClientOptions.AccessTokenUri;
        data["spring:cloud:config:clientSecret"] = ClientOptions.ClientSecret;
        data["spring:cloud:config:clientId"] = ClientOptions.ClientId;
        data["spring:cloud:config:tokenTtl"] = ClientOptions.TokenTtl.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:tokenRenewRate"] = ClientOptions.TokenRenewRate.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:disableTokenRenewal"] = ClientOptions.DisableTokenRenewal.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:enabled"] = ClientOptions.Retry.Enabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:initialInterval"] = ClientOptions.Retry.InitialInterval.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:maxInterval"] = ClientOptions.Retry.MaxInterval.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:multiplier"] = ClientOptions.Retry.Multiplier.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:retry:maxAttempts"] = ClientOptions.Retry.MaxAttempts.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:discovery:enabled"] = ClientOptions.Discovery.Enabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:discovery:serviceId"] = ClientOptions.Discovery.ServiceId;
        data["spring:cloud:config:health:enabled"] = ClientOptions.Health.Enabled.ToString(CultureInfo.InvariantCulture);
        data["spring:cloud:config:health:timeToLive"] = ClientOptions.Health.TimeToLive.ToString(CultureInfo.InvariantCulture);

        foreach ((string headerName, string headerValue) in ClientOptions.Headers)
        {
            data[$"spring:cloud:config:headers:{headerName}"] = headerValue;
        }
    }

    internal async Task<ConfigEnvironment?> RemoteLoadAsync(List<Uri> requestUris, string? label, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Entered {Method}", nameof(RemoteLoadAsync));

        // Get client if not already set
        using HttpClient httpClient = CreateHttpClient(ClientOptions);

        Exception? error = null;

        foreach (Uri requestUri in requestUris)
        {
            // Make Config Server URI from settings
            Uri uri = BuildConfigServerUri(requestUri, label);

            _logger.LogDebug("Trying to connect to Config Server at {RequestUri}", uri.ToMaskedString());

            // Get the request message
            _logger.LogTrace("Building HTTP request message");
            HttpRequestMessage request = await GetRequestMessageAsync(uri, cancellationToken);

            // Invoke Config Server
            try
            {
                _logger.LogTrace("Sending HTTP request");
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                _logger.LogDebug("Config Server returned status: {StatusCode} invoking path: {RequestUri}", response.StatusCode, uri.ToMaskedString());

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

                _logger.LogTrace("Parsing JSON response");
                return await response.Content.ReadFromJsonAsync<ConfigEnvironment>(SerializerOptions, cancellationToken);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                error = exception;

                if (IsSocketError(exception))
                {
                    _logger.LogTrace(exception, "Socket error detected");
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
    internal Uri BuildConfigServerUri(Uri serverUri, string? label)
    {
        ArgumentNullException.ThrowIfNull(serverUri);

        var uriBuilder = new UriBuilder(serverUri);

        if (!string.IsNullOrEmpty(ClientOptions.Username))
        {
            uriBuilder.UserName = WebUtility.UrlEncode(ClientOptions.Username);
        }

        if (!string.IsNullOrEmpty(ClientOptions.Password))
        {
            uriBuilder.Password = WebUtility.UrlEncode(ClientOptions.Password);
        }

        string pathSuffix = $"{WebUtility.UrlEncode(ClientOptions.Name)}/{WebUtility.UrlEncode(ClientOptions.Environment)}";

        if (!string.IsNullOrWhiteSpace(label))
        {
            // If label contains slash, replace it
            if (label.Contains('/'))
            {
                label = label.Replace("/", "(_)", StringComparison.Ordinal);
            }

            pathSuffix = $"{pathSuffix}/{WebUtility.UrlEncode(label)}";
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
    private void AddPropertySource(PropertySource? source, Dictionary<string, string?> data)
    {
        ArgumentNullException.ThrowIfNull(data);

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
                _logger.LogError(exception, "Config Server exception, property: {Key}={Type}", pair.Key, pair.Value.GetType());
            }
        }
    }

    private string? ConvertValue(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private void RenewToken()
    {
#pragma warning disable S4462 // Calls to "async" methods should not be blocking
        // Justification: Configuration sources and providers don't support async.
        _ = new Timer(_ => RefreshVaultTokenAsync(CancellationToken.None).GetAwaiter().GetResult(), null,
            TimeSpan.FromMilliseconds(ClientOptions.TokenRenewRate), TimeSpan.FromMilliseconds(ClientOptions.TokenRenewRate));
#pragma warning restore S4462 // Calls to "async" methods should not be blocking
    }

    // fire and forget
    internal async Task RefreshVaultTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ClientOptions.Token))
        {
            return;
        }

        string obscuredToken = $"{ClientOptions.Token[..4]}[*]{ClientOptions.Token[^4..]}";

        try
        {
            using HttpClient httpClient = CreateHttpClient(ClientOptions);

            Uri uri = GetVaultRenewUri();
            HttpRequestMessage message = await GetVaultRenewRequestMessageAsync(uri, cancellationToken);

            _logger.LogInformation("Renewing Vault token {Token} for {Ttl} milliseconds at Uri {Uri}", obscuredToken, ClientOptions.TokenTtl,
                uri.ToMaskedString());

            using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning("Renewing Vault token {Token} returned status: {Status}", obscuredToken, response.StatusCode);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Unable to renew Vault token {Token}. Is the token invalid or expired?", obscuredToken);
        }
    }

    private Uri GetVaultRenewUri()
    {
        string baseUri = ClientOptions.Uri!.Split(',')[0].Trim();

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

        if (!string.IsNullOrEmpty(ClientOptions.AccessTokenUri))
        {
            using HttpClient httpClient = CreateHttpClient(ClientOptions);
            var accessTokenUri = new Uri(ClientOptions.AccessTokenUri);

            string accessToken = await httpClient.GetAccessTokenAsync(accessTokenUri, ClientOptions.ClientId, ClientOptions.ClientSecret, cancellationToken);

            _logger.LogDebug("Fetched access token from '{AccessTokenUri}'.", accessTokenUri.ToMaskedString());
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (!string.IsNullOrEmpty(ClientOptions.Token))
        {
            requestMessage.Headers.Add(VaultTokenHeader, ClientOptions.Token);
        }

        int renewTtlInSeconds = ClientOptions.TokenTtl / 1000;
        string json = $"{{\"increment\":{renewTtlInSeconds}}}";
        requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return requestMessage;
    }

    internal bool IsDiscoveryFirstEnabled()
    {
        IConfigurationSection clientConfigSection = _configuration.GetSection(ConfigServerClientOptions.ConfigurationPrefix);
        return clientConfigSection.GetValue("discovery:enabled", ClientOptions.Discovery.Enabled);
    }

    /// <summary>
    /// Creates an appropriately configured HttpClient that can be used in communicating with the Spring Cloud Configuration Server.
    /// </summary>
    /// <param name="clientOptions">
    /// The settings used to configure the HttpClient.
    /// </param>
    /// <returns>
    /// The HttpClient used by the provider.
    /// </returns>
    internal HttpClient CreateHttpClient(ConfigServerClientOptions clientOptions)
    {
        ArgumentNullException.ThrowIfNull(clientOptions);
        ObjectDisposedException.ThrowIf(_httpClientHandler == null, this);

        var clientCertificateConfigurer = new ClientCertificateHttpClientHandlerConfigurer(OptionsMonitorWrapper.Create(clientOptions.ClientCertificate));
        clientCertificateConfigurer.Configure("ConfigServer", _httpClientHandler);

        var validateCertificatesHandler =
            new ValidateCertificatesHttpClientHandlerConfigurer<ConfigServerClientOptions>(OptionsMonitorWrapper.Create(clientOptions));

        validateCertificatesHandler.Configure(Options.DefaultName, _httpClientHandler);

        var httpClient = new HttpClient(_httpClientHandler, false);
        httpClient.ConfigureForSteeltoe(clientOptions.HttpTimeout);

        foreach ((string headerName, string headerValue) in clientOptions.Headers)
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

        _timerTickLock?.Dispose();
        _timerTickLock = null;

        if (_ownsHttpClientHandler)
        {
            _httpClientHandler?.Dispose();
        }

        _httpClientHandler = null;
    }
}
