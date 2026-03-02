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
internal sealed partial class ConfigServerConfigurationProvider : ConfigurationProvider, IDisposable
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
        LogEnteringTimerCycle();
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
                LogExclusiveLockObtained();
                await DoLoadAsync(true, CancellationToken.None);
            }
            else
            {
                LogSkippingCycle();
            }
        }
        catch (Exception exception)
        {
            LogCouldNotReloadDuringPolling(exception);
        }
        finally
        {
            if (lockTaken)
            {
                LogTimerCycleCompleted();

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
            LogConfigServerClientDisabled();
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
                LogFetchingConfiguration();

                try
                {
                    return await DoLoadAsync(updateDictionary, cancellationToken);
                }
                catch (ConfigServerException exception)
                {
                    LogFailedFetchingConfiguration(exception);
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

        LogFetchingConfiguration();
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
                LogProcessingLabel(label);

                if (uris.Count > 1)
                {
                    LogMultipleConfigServerUris();
                }

                // Invoke Config Servers
                ConfigEnvironment? env = await RemoteLoadAsync(uris, label, cancellationToken);

                // Update configuration Data dictionary with any results
                if (env != null)
                {
                    LogEnvironmentLocated(env.Name, string.Join(", ", env.Profiles.Select(p => $"'{p}'")), env.Label, env.Version, env.State);

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
                            LogDataChanged();
                            Data = data;
                            OnReload();
                        }
                        else
                        {
                            LogDataNotChanged();
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

        LogCouldNotLocatePropertySource(error);

        if (ClientOptions.FailFast)
        {
            LogFailFastEnabled(error);
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
            LogAddingCredentials(requestUri.ToMaskedString());

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

                LogAccessTokenFetched(accessTokenUri.ToMaskedString());
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
        LogRemoteLoadEntered(nameof(RemoteLoadAsync));

        // Get client if not already set
        using HttpClient httpClient = CreateHttpClient(ClientOptions);

        Exception? error = null;

        foreach (Uri requestUri in requestUris)
        {
            // Make Config Server URI from settings
            Uri uri = BuildConfigServerUri(requestUri, label);

            LogTryingToConnect(uri.ToMaskedString());

            // Get the request message
            LogBuildingHttpRequest();
            HttpRequestMessage request = await GetRequestMessageAsync(uri, cancellationToken);

            // Invoke Config Server
            try
            {
                LogSendingHttpRequest();
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                LogConfigServerReturnedStatus(response.StatusCode, uri.ToMaskedString());

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

                LogParsingJsonResponse();
                return await response.Content.ReadFromJsonAsync<ConfigEnvironment>(SerializerOptions, cancellationToken);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                error = exception;

                if (IsSocketError(exception))
                {
                    LogSocketError(exception);
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
                LogConfigServerPropertyException(exception, pair.Key, pair.Value.GetType());
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

            LogRenewingVaultToken(obscuredToken, ClientOptions.TokenTtl, uri.ToMaskedString());
            using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                LogVaultTokenRenewalStatus(obscuredToken, response.StatusCode);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            LogUnableToRenewVaultToken(exception, obscuredToken);
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

            LogAccessTokenFetched(accessTokenUri.ToMaskedString());
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

    [LoggerMessage(Level = LogLevel.Trace, Message = "Entering timer cycle.")]
    private partial void LogEnteringTimerCycle();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Exclusive lock obtained.")]
    private partial void LogExclusiveLockObtained();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Previous cycle is still running, or already disposed; skipping this cycle.")]
    private partial void LogSkippingCycle();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not reload configuration during polling.")]
    private partial void LogCouldNotReloadDuringPolling(Exception exception);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Timer cycle completed, releasing exclusive lock.")]
    private partial void LogTimerCycleCompleted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Config Server client disabled, not fetching configuration.")]
    private partial void LogConfigServerClientDisabled();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching configuration from server(s).")]
    private partial void LogFetchingConfiguration();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed fetching configuration from server(s).")]
    private partial void LogFailedFetchingConfiguration(Exception exception);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Processing label '{Label}'.")]
    private partial void LogProcessingLabel(string? label);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Multiple Config Server Uris listed.")]
    private partial void LogMultipleConfigServerUris();

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Located environment with name {Name}, profiles {Profiles}, label {Label}, version {Version} and state {State}.")]
    private partial void LogEnvironmentLocated(string? name, string profiles, string? label, string? version, string? state);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Data has changed, raising configuration reload.")]
    private partial void LogDataChanged();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Data has not changed.")]
    private partial void LogDataNotChanged();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not locate property source.")]
    private partial void LogCouldNotLocatePropertySource(Exception? error);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Failure with FailFast enabled, throwing ConfigServerException.")]
    private partial void LogFailFastEnabled(Exception? error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Adding credentials from '{RequestUri}' to Authorization header.")]
    private partial void LogAddingCredentials(string requestUri);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched access token from {AccessTokenUri}.")]
    private partial void LogAccessTokenFetched(string accessTokenUri);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Entered {Method}.")]
    private partial void LogRemoteLoadEntered(string method);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Trying to connect to Config Server at {RequestUri}.")]
    private partial void LogTryingToConnect(string requestUri);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Building HTTP request message.")]
    private partial void LogBuildingHttpRequest();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Sending HTTP request.")]
    private partial void LogSendingHttpRequest();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Config Server returned status {StatusCode} for path {RequestUri}.")]
    private partial void LogConfigServerReturnedStatus(HttpStatusCode statusCode, string requestUri);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Parsing JSON response.")]
    private partial void LogParsingJsonResponse();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Socket error detected.")]
    private partial void LogSocketError(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Config Server exception for property {Key} of type {Type}.")]
    private partial void LogConfigServerPropertyException(Exception exception, string key, Type type);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Renewing Vault token {Token} for {Ttl} milliseconds at Uri {Uri}.")]
    private partial void LogRenewingVaultToken(string token, int ttl, string uri);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Renewing Vault token {Token} returned status {Status}.")]
    private partial void LogVaultTokenRenewalStatus(string token, HttpStatusCode status);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unable to renew Vault token {Token}. The token is likely invalid or has expired.")]
    private partial void LogUnableToRenewVaultToken(Exception exception, string token);
}
