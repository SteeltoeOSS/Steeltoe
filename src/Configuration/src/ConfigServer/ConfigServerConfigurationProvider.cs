// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Http;
using Steeltoe.Common.Http.HttpClientPooling;
using LockPrimitive =
#if NET10_0_OR_GREATER
    System.Threading.Lock
#else
    object
#endif
    ;

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

    private readonly ILogger<ConfigServerConfigurationProvider> _logger;
    private readonly bool _ownsHttpClientHandler;
    private readonly ConfigureConfigServerClientOptions _configurer;
    private readonly ConfigServerClientOptions _initialOptions;
    private readonly LockPrimitive _lifecycleLock = new();
    private readonly LockPrimitive _configurationReloadTickLock = new();
    private readonly LockPrimitive _vaultRenewTickLock = new();
    private readonly ConfigServerDiscoveryService _configServerDiscoveryService;
    private readonly IDisposable _changeTokenRegistration;

    private volatile HttpClientHandler? _httpClientHandler;
    private Timer? _configurationReloadTimer;
    private Timer? _vaultRenewTimer;
    private volatile DiscoveryLookupResult? _lastDiscoveryLookupResult;
    private volatile ConfigServerClientOptions _clientOptions;
    private volatile bool _isDisposed;

    internal static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
    };

    internal IDictionary<string, string?> InnerData => Data;

    /// <summary>
    /// Gets the settings used to access Config Server, excluding information found during service discovery (so that a provider (re)load properly observes
    /// changes and triggers its change token). Returns a cloned snapshot to prevent tearing during reads/writes.
    /// </summary>
    internal ConfigServerClientOptions ClientOptions => _clientOptions.Clone();

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
        : this(source.DefaultOptions, source.Configuration, source.HttpClientHandler, loggerFactory)
    {
    }

    internal ConfigServerConfigurationProvider(ConfigServerClientOptions clientOptions, IConfiguration? configuration, HttpClientHandler? httpClientHandler,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(clientOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<ConfigServerConfigurationProvider>();
        IConfiguration effectiveConfiguration = configuration ?? new ConfigurationBuilder().Build();
        _configurer = new ConfigureConfigServerClientOptions(effectiveConfiguration);
        _configServerDiscoveryService = new ConfigServerDiscoveryService(effectiveConfiguration, loggerFactory);

        _initialOptions = clientOptions.Clone();
        _clientOptions = _initialOptions;

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
        _changeTokenRegistration = ChangeToken.OnChange(effectiveConfiguration.GetReloadToken, OnSettingsChanged);
    }

    private void OnSettingsChanged()
    {
        ConfigServerClientOptions newOptions = _initialOptions.Clone();
        _configurer.Configure(newOptions);

        lock (_lifecycleLock)
        {
            if (_isDisposed)
            {
                // Prevent creating new timers after Dispose has already torn them down.
                return;
            }

            TimeSpan previousPollingInterval = _clientOptions.PollingInterval;
            int previousTokenRenewRate = _clientOptions.TokenRenewRate;

            ConfigureHttpClientHandler(newOptions);
            UpdateConfigurationReloadTimer(newOptions, previousPollingInterval);
            UpdateVaultRenewTimer(newOptions, previousTokenRenewRate);

            _clientOptions = newOptions;
        }
    }

    private void UpdateConfigurationReloadTimer(ConfigServerClientOptions optionsSnapshot, TimeSpan previousPollingInterval)
    {
        if (optionsSnapshot.PollingInterval == TimeSpan.Zero || !optionsSnapshot.Enabled)
        {
            _configurationReloadTimer?.Dispose();
            _configurationReloadTimer = null;
        }
        else if (_configurationReloadTimer == null)
        {
            _configurationReloadTimer = new Timer(_ => ConfigurationReloadTimerTick(), null, TimeSpan.Zero, optionsSnapshot.PollingInterval);
        }
        else if (previousPollingInterval != optionsSnapshot.PollingInterval)
        {
            _configurationReloadTimer.Change(TimeSpan.Zero, optionsSnapshot.PollingInterval);
        }
    }

    private void UpdateVaultRenewTimer(ConfigServerClientOptions optionsSnapshot, int previousTokenRenewRate)
    {
        if (string.IsNullOrEmpty(optionsSnapshot.Token) || optionsSnapshot.DisableTokenRenewal ||
            optionsSnapshot is not { Uri: not null, IsMultiServerConfiguration: false })
        {
            _vaultRenewTimer?.Dispose();
            _vaultRenewTimer = null;
        }
        else if (_vaultRenewTimer == null)
        {
            _vaultRenewTimer = new Timer(_ => VaultRenewTimerTick(), null, TimeSpan.FromMilliseconds(optionsSnapshot.TokenRenewRate),
                TimeSpan.FromMilliseconds(optionsSnapshot.TokenRenewRate));
        }
        else if (previousTokenRenewRate != optionsSnapshot.TokenRenewRate)
        {
            _vaultRenewTimer.Change(TimeSpan.FromMilliseconds(optionsSnapshot.TokenRenewRate), TimeSpan.FromMilliseconds(optionsSnapshot.TokenRenewRate));
        }
    }

    /// <remarks>
    /// ConfigurationReloadTimerTick is called by a Timer callback, so must catch all exceptions.
    /// </remarks>
    private void ConfigurationReloadTimerTick()
    {
        LogEnteringConfigurationReloadCycle();

#if NET10_0_OR_GREATER
        bool lockTaken = _configurationReloadTickLock.TryEnter();
#else
        bool lockTaken = Monitor.TryEnter(_configurationReloadTickLock);
#endif

        try
        {
            if (!lockTaken || _isDisposed)
            {
                LogSkippingConfigurationReloadCycle();
                return;
            }

            LogConfigurationReloadLockObtained();
            ConfigServerClientOptions optionsSnapshot = ClientOptions;

#pragma warning disable S4462 // Calls to "async" methods should not be blocking
            // Justification: Configuration sources and providers don't support async.
            UpdateDiscoveryAsync(optionsSnapshot, false, CancellationToken.None).GetAwaiter().GetResult();
            DoLoadAsync(optionsSnapshot, true, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore S4462 // Calls to "async" methods should not be blocking

            LogConfigurationReloadCycleCompleted();
        }
        catch (Exception exception)
        {
            if (exception is ObjectDisposedException && _isDisposed)
            {
                // Disposal can race with the timer callback because _isDisposed is read without locking.
            }
            else
            {
                LogFailedToReloadConfiguration(exception);
            }
        }
        finally
        {
            if (lockTaken)
            {
#if NET10_0_OR_GREATER
                _configurationReloadTickLock.Exit();
#else
                Monitor.Exit(_configurationReloadTickLock);
#endif
            }
        }
    }

    /// <remarks>
    /// VaultRenewTimerTick is called by a Timer callback, so must catch all exceptions.
    /// </remarks>
    private void VaultRenewTimerTick()
    {
        LogEnteringVaultRenewCycle();

#if NET10_0_OR_GREATER
        bool lockTaken = _vaultRenewTickLock.TryEnter();
#else
        bool lockTaken = Monitor.TryEnter(_vaultRenewTickLock);
#endif

        try
        {
            if (!lockTaken || _isDisposed)
            {
                LogSkippingVaultRenewCycle();
                return;
            }

            LogVaultRenewLockObtained();

#pragma warning disable S4462 // Calls to "async" methods should not be blocking
            // Justification: Configuration sources and providers don't support async.
            RefreshVaultTokenAsync(ClientOptions, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore S4462 // Calls to "async" methods should not be blocking

            LogVaultRenewCycleCompleted();
        }
        catch (Exception exception)
        {
            if (exception is ObjectDisposedException && _isDisposed)
            {
                // Disposal can race with the timer callback because _isDisposed is read without locking.
            }
            else
            {
                LogFailedToRenewVaultToken(exception);
            }
        }
        finally
        {
            if (lockTaken)
            {
#if NET10_0_OR_GREATER
                _vaultRenewTickLock.Exit();
#else
                Monitor.Exit(_vaultRenewTickLock);
#endif
            }
        }
    }

    /// <summary>
    /// Loads configuration data from the Spring Cloud Config Server as specified by the <see cref="ConfigServerClientOptions" />.
    /// </summary>
    public override void Load()
    {
        try
        {
#pragma warning disable S4462 // Calls to "async" methods should not be blocking
            // Justification: Configuration sources and providers don't support async.
            LoadInternalAsync(ClientOptions, true, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore S4462 // Calls to "async" methods should not be blocking
        }
        catch (ObjectDisposedException) when (_isDisposed)
        {
            // Expected during disposal; silently ignore.
        }
    }

    internal async Task<ConfigEnvironment?> LoadInternalAsync(ConfigServerClientOptions optionsSnapshot, bool updateDictionary,
        CancellationToken cancellationToken)
    {
        if (!optionsSnapshot.Enabled)
        {
            LogConfigServerClientDisabled();
            return null;
        }

        await UpdateDiscoveryAsync(optionsSnapshot, optionsSnapshot.FailFast, cancellationToken);

        if (optionsSnapshot is { Retry.Enabled: true, FailFast: true })
        {
            int attempts = 0;
            int backOff = optionsSnapshot.Retry.InitialInterval;

            do
            {
                LogFetchingConfiguration();

                try
                {
                    return await DoLoadAsync(optionsSnapshot, updateDictionary, cancellationToken);
                }
                catch (ConfigServerException exception)
                {
                    LogFailedFetchingConfiguration(exception);
                    attempts++;

                    if (attempts < optionsSnapshot.Retry.MaxAttempts)
                    {
                        Thread.CurrentThread.Join(backOff);
                        int nextBackOff = (int)(backOff * optionsSnapshot.Retry.Multiplier);
                        backOff = Math.Min(nextBackOff, optionsSnapshot.Retry.MaxInterval);
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
        return await DoLoadAsync(optionsSnapshot, updateDictionary, cancellationToken);
    }

    internal async Task<ConfigEnvironment?> DoLoadAsync(ConfigServerClientOptions optionsSnapshot, bool updateDictionary, CancellationToken cancellationToken)
    {
        ApplyLastDiscoveryLookupResultToClientOptions(optionsSnapshot);

        Exception? error = null;

        // Get list of Config Server uris to check
        List<Uri> uris = optionsSnapshot.GetUris();

        try
        {
            foreach (string label in GetLabels(optionsSnapshot))
            {
                LogProcessingLabel(label);

                if (uris.Count > 1)
                {
                    LogMultipleConfigServerUris();
                }

                // Invoke Config Servers
                ConfigEnvironment? env = await RemoteLoadAsync(optionsSnapshot, uris, label, cancellationToken);

                // Update configuration Data dictionary with any results
                if (env != null)
                {
                    LogEnvironmentLocated(env.Name, string.Join(", ", env.Profiles.Select(p => $"'{p}'")), env.Label, env.Version, env.State);

                    if (updateDictionary)
                    {
                        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                        CopyLastDiscoveryLookupResultToData(data, optionsSnapshot.Discovery.Enabled);

                        if (!string.IsNullOrEmpty(env.State))
                        {
                            data["spring:cloud:config:client:state"] = env.State;
                        }

                        if (!string.IsNullOrEmpty(env.Version))
                        {
                            data["spring:cloud:config:client:version"] = env.Version;
                        }

                        IList<PropertySource> sources = env.PropertySources;

                        for (int index = sources.Count - 1; index >= 0; index--)
                        {
                            AddPropertySource(sources[index], data);
                        }

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

        if (optionsSnapshot.FailFast)
        {
            LogFailFastEnabled(error);
            throw new ConfigServerException("Could not locate PropertySource, fail fast property is set, failing", error);
        }

        return null;
    }

    internal void ApplyLastDiscoveryLookupResultToClientOptions(ConfigServerClientOptions optionsSnapshot)
    {
        DiscoveryLookupResult? lastResult = _lastDiscoveryLookupResult;

        if (lastResult != null && optionsSnapshot.Discovery.Enabled)
        {
            optionsSnapshot.Uri = lastResult.ConfigServerUri;

            if (lastResult.Username != null)
            {
                optionsSnapshot.Username = lastResult.Username;
            }

            if (lastResult.Password != null)
            {
                optionsSnapshot.Password = lastResult.Password;
            }
        }
    }

    private void CopyLastDiscoveryLookupResultToData(Dictionary<string, string?> data, bool isDiscoveryEnabled)
    {
        DiscoveryLookupResult? lastResult = _lastDiscoveryLookupResult;

        if (lastResult != null && isDiscoveryEnabled)
        {
            data["spring:cloud:config:uri"] = lastResult.ConfigServerUri;

            if (lastResult.Username != null)
            {
                data["spring:cloud:config:username"] = lastResult.Username;
            }

            if (lastResult.Password != null)
            {
                data["spring:cloud:config:password"] = lastResult.Password;
            }
        }
    }

    private static bool AreDictionariesEqual<TKey, TValue>(IDictionary<TKey, TValue> first, Dictionary<TKey, TValue> second)
        where TKey : notnull
    {
        return first.Count == second.Count && first.Keys.All(firstKey =>
            second.ContainsKey(firstKey) && EqualityComparer<TValue>.Default.Equals(first[firstKey], second[firstKey]));
    }

    internal string[] GetLabels(ConfigServerClientOptions optionsSnapshot)
    {
        if (string.IsNullOrWhiteSpace(optionsSnapshot.Label))
        {
            return EmptyLabels;
        }

        return optionsSnapshot.Label.Split(CommaDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private async Task UpdateDiscoveryAsync(ConfigServerClientOptions optionsSnapshot, bool failFast, CancellationToken cancellationToken)
    {
        if (optionsSnapshot.Discovery.Enabled)
        {
            List<IServiceInstance> instances = await _configServerDiscoveryService.GetConfigServerInstancesAsync(optionsSnapshot, cancellationToken);
            SetLastDiscoveryLookupResult(instances);

            if (instances.Count == 0 && failFast)
            {
                throw new ConfigServerException("Could not locate Config Server via discovery, are you missing a Discovery service assembly?");
            }
        }
        else
        {
            SetLastDiscoveryLookupResult([]);
        }
    }

    internal void SetLastDiscoveryLookupResult(IEnumerable<IServiceInstance> instances)
    {
        var endpointBuilder = new StringBuilder();
        string? username = null;
        string? password = null;

        foreach (IServiceInstance instance in instances)
        {
            if (instance.Metadata.TryGetValue("password", out string? instancePassword))
            {
                instance.Metadata.TryGetValue("user", out string? instanceUsername);
                username = instanceUsername ?? "user";
                password = instancePassword;
            }

            string uri = instance.Uri.ToString();

            if (instance.Metadata.TryGetValue("configPath", out string? path) && path != null)
            {
                if (uri.EndsWith('/') && path.StartsWith('/'))
                {
                    uri = uri[..^1];
                }

                uri += path;
            }

            endpointBuilder.Append(uri);
            endpointBuilder.Append(',');
        }

        if (endpointBuilder.Length > 0)
        {
            string uris = endpointBuilder.ToString(0, endpointBuilder.Length - 1);
            _lastDiscoveryLookupResult = new DiscoveryLookupResult(uris, username, password);
        }
        else
        {
            _lastDiscoveryLookupResult = null;
        }
    }

    internal async Task ProvideRuntimeReplacementsAsync(ICollection<IDiscoveryClient> discoveryClientsFromServiceProvider, CancellationToken cancellationToken)
    {
        await _configServerDiscoveryService.ProvideRuntimeReplacementsAsync(discoveryClientsFromServiceProvider, cancellationToken);
    }

    internal async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        await _configServerDiscoveryService.ShutdownAsync(cancellationToken);
    }

    /// <summary>
    /// Creates the <see cref="HttpRequestMessage" /> that will be used in accessing the Spring Cloud Config server.
    /// </summary>
    /// <param name="optionsSnapshot">
    /// A snapshot of the client options to use for this request.
    /// </param>
    /// <param name="requestUri">
    /// The Uri used when accessing the server.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The HttpRequestMessage built from the path.
    /// </returns>
    internal async Task<HttpRequestMessage> GetRequestMessageAsync(ConfigServerClientOptions optionsSnapshot, Uri requestUri,
        CancellationToken cancellationToken)
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
            if (!string.IsNullOrEmpty(optionsSnapshot.AccessTokenUri))
            {
                using HttpClient httpClient = CreateHttpClient(optionsSnapshot);
                var accessTokenUri = new Uri(optionsSnapshot.AccessTokenUri);

                string accessToken =
                    await httpClient.GetAccessTokenAsync(accessTokenUri, optionsSnapshot.ClientId, optionsSnapshot.ClientSecret, cancellationToken);

                LogAccessTokenFetched(accessTokenUri.ToMaskedString());
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        if (!string.IsNullOrEmpty(optionsSnapshot.Token) && optionsSnapshot is { Uri: not null, IsMultiServerConfiguration: false })
        {
            requestMessage.Headers.Add(TokenHeader, optionsSnapshot.Token);
        }

        return requestMessage;
    }

    internal async Task<ConfigEnvironment?> RemoteLoadAsync(ConfigServerClientOptions optionsSnapshot, List<Uri> requestUris, string? label,
        CancellationToken cancellationToken)
    {
        LogRemoteLoadEntered(nameof(RemoteLoadAsync));

        // Get client if not already set
        using HttpClient httpClient = CreateHttpClient(optionsSnapshot);

        Exception? error = null;

        foreach (Uri requestUri in requestUris)
        {
            // Make Config Server URI from settings
            Uri uri = BuildConfigServerUri(optionsSnapshot, requestUri, label);

            LogTryingToConnect(uri.ToMaskedString());

            // Get the request message
            LogBuildingHttpRequest();
            HttpRequestMessage request = await GetRequestMessageAsync(optionsSnapshot, uri, cancellationToken);

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
            ExceptionDispatchInfo.Capture(error).Throw();
        }

        return null;
    }

    /// <summary>
    /// Creates the Uri that will be used in accessing the Configuration Server.
    /// </summary>
    /// <param name="optionsSnapshot">
    /// A snapshot of the client options to use for URI construction.
    /// </param>
    /// <param name="serverUri">
    /// Base server uri to use.
    /// </param>
    /// <param name="label">
    /// The label to add.
    /// </param>
    /// <returns>
    /// The request URI for the Configuration Server.
    /// </returns>
    internal Uri BuildConfigServerUri(ConfigServerClientOptions optionsSnapshot, Uri serverUri, string? label)
    {
        ArgumentNullException.ThrowIfNull(serverUri);

        var uriBuilder = new UriBuilder(serverUri);

        if (!string.IsNullOrEmpty(optionsSnapshot.Username))
        {
            uriBuilder.UserName = WebUtility.UrlEncode(optionsSnapshot.Username);
        }

        if (!string.IsNullOrEmpty(optionsSnapshot.Password))
        {
            uriBuilder.Password = WebUtility.UrlEncode(optionsSnapshot.Password);
        }

        string pathSuffix = $"{WebUtility.UrlEncode(optionsSnapshot.Name)}/{WebUtility.UrlEncode(optionsSnapshot.Environment)}";

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

    /// <summary>
    /// Extends the lease of the current HashiCorp Vault token; it does not generate a new token. A new token is only picked up when the configuration
    /// changes and <see cref="OnSettingsChanged" /> reconfigures the timer.
    /// </summary>
    internal async Task RefreshVaultTokenAsync(ConfigServerClientOptions optionsSnapshot, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(optionsSnapshot.Token))
        {
            return;
        }

        string obscuredToken = $"{optionsSnapshot.Token[..4]}[*]{optionsSnapshot.Token[^4..]}";

        try
        {
            using HttpClient httpClient = CreateHttpClient(optionsSnapshot);

            Uri uri = GetVaultRenewUri(optionsSnapshot);
            HttpRequestMessage message = await GetVaultRenewRequestMessageAsync(optionsSnapshot, uri, cancellationToken);

            LogRenewingVaultToken(obscuredToken, optionsSnapshot.TokenTtl, uri.ToMaskedString());
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

    private static Uri GetVaultRenewUri(ConfigServerClientOptions optionsSnapshot)
    {
        string baseUri = optionsSnapshot.Uri!.Split(',')[0].Trim();

        if (!baseUri.EndsWith('/'))
        {
            baseUri += '/';
        }

        return new Uri(baseUri + VaultRenewPath, UriKind.RelativeOrAbsolute);
    }

    private async Task<HttpRequestMessage> GetVaultRenewRequestMessageAsync(ConfigServerClientOptions optionsSnapshot, Uri requestUri,
        CancellationToken cancellationToken)
    {
        var uriWithoutUserInfo = new Uri(requestUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped));
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, uriWithoutUserInfo);

        if (!string.IsNullOrEmpty(optionsSnapshot.AccessTokenUri))
        {
            using HttpClient httpClient = CreateHttpClient(optionsSnapshot);
            var accessTokenUri = new Uri(optionsSnapshot.AccessTokenUri);

            string accessToken =
                await httpClient.GetAccessTokenAsync(accessTokenUri, optionsSnapshot.ClientId, optionsSnapshot.ClientSecret, cancellationToken);

            LogAccessTokenFetched(accessTokenUri.ToMaskedString());
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (!string.IsNullOrEmpty(optionsSnapshot.Token))
        {
            requestMessage.Headers.Add(VaultTokenHeader, optionsSnapshot.Token);
        }

        int renewTtlInSeconds = optionsSnapshot.TokenTtl / 1000;
        string json = $"{{\"increment\":{renewTtlInSeconds}}}";
        requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return requestMessage;
    }

    /// <summary>
    /// Creates an appropriately configured HttpClient that can be used in communicating with the Spring Cloud Config Server.
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
        HttpClientHandler? handler = _httpClientHandler;
        ObjectDisposedException.ThrowIf(handler == null, this);

        var httpClient = new HttpClient(handler, false);
        httpClient.ConfigureForSteeltoe(clientOptions.HttpTimeout);

        foreach ((string headerName, string headerValue) in clientOptions.Headers)
        {
            httpClient.DefaultRequestHeaders.Add(headerName, headerValue);
        }

        return httpClient;
    }

    private void ConfigureHttpClientHandler(ConfigServerClientOptions optionsSnapshot)
    {
        HttpClientHandler? httpClientHandler = _httpClientHandler;

        if (httpClientHandler == null)
        {
            return;
        }

        httpClientHandler.ClientCertificates.Clear();

        var clientCertificateConfigurer = new ClientCertificateHttpClientHandlerConfigurer(OptionsMonitorWrapper.Create(optionsSnapshot.ClientCertificate));
        clientCertificateConfigurer.Configure("ConfigServer", httpClientHandler);

        var validateCertificatesHandler =
            new ValidateCertificatesHttpClientHandlerConfigurer<ConfigServerClientOptions>(OptionsMonitorWrapper.Create(optionsSnapshot));

        validateCertificatesHandler.Configure(Options.DefaultName, httpClientHandler);
    }

    private static bool IsSocketError(Exception exception)
    {
        return exception is HttpRequestException && exception.InnerException is SocketException;
    }

    public void Dispose()
    {
        lock (_lifecycleLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            _changeTokenRegistration.Dispose();

            _configurationReloadTimer?.Dispose();
            _configurationReloadTimer = null;

            _vaultRenewTimer?.Dispose();
            _vaultRenewTimer = null;
        }

        if (_ownsHttpClientHandler)
        {
            _httpClientHandler?.Dispose();
        }

        _httpClientHandler = null;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Entering polling configuration reload cycle.")]
    private partial void LogEnteringConfigurationReloadCycle();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Polling configuration reload lock obtained.")]
    private partial void LogConfigurationReloadLockObtained();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Previous polling configuration reload cycle is still running, or already disposed; skipping this cycle.")]
    private partial void LogSkippingConfigurationReloadCycle();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to reload configuration during polling.")]
    private partial void LogFailedToReloadConfiguration(Exception exception);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Polling configuration reload cycle completed, releasing lock.")]
    private partial void LogConfigurationReloadCycleCompleted();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Entering Vault token renewal cycle.")]
    private partial void LogEnteringVaultRenewCycle();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Vault token renewal lock obtained.")]
    private partial void LogVaultRenewLockObtained();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Previous Vault token renewal cycle is still running, or already disposed; skipping this cycle.")]
    private partial void LogSkippingVaultRenewCycle();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to renew Vault token.")]
    private partial void LogFailedToRenewVaultToken(Exception exception);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Vault token renewal cycle completed, releasing lock.")]
    private partial void LogVaultRenewCycleCompleted();

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

    private sealed record DiscoveryLookupResult(string ConfigServerUri, string? Username, string? Password);
}
