// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Security;
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
public class ConfigServerConfigurationProvider : ConfigurationProvider, IConfigurationSource
{
    private const string ArrayPattern = @"(\[[0-9]+\])*$";
    private const string VaultRenewPath = "vault/v1/auth/token/renew-self";
    private const string VaultTokenHeader = "X-Vault-Token";
    private const string DelimiterString = ".";
    private const char DelimiterChar = '.';
    private const char EscapeChar = '\\';
    private const string EscapeString = "\\";

    /// <summary>
    /// The prefix (<see cref="IConfigurationSection" /> under which all Spring Cloud Config Server configuration settings (
    /// <see cref="ConfigServerClientSettings" /> are found. (e.g. spring:cloud:config:env, spring:cloud:config:uri, spring:cloud:config:enabled, etc.)
    /// </summary>
    public const string Prefix = "spring:cloud:config";

    public const string TokenHeader = "X-Config-Token";
    public const string StateHeader = "X-Config-State";

    private static readonly char[] CommaDelimit =
    {
        ','
    };

    private static readonly string[] EmptyLabels =
    {
        string.Empty
    };

    internal ConfigServerDiscoveryService ConfigServerDiscoveryService;

    protected ConfigServerClientSettings settings; // Current settings
    protected HttpClient httpClient;
    protected ILogger logger;
    protected ILoggerFactory loggerFactory;
    protected IConfiguration configuration;
    protected Timer tokenRenewTimer;
    protected Timer refreshTimer;
    protected bool hasConfiguration;

    internal JsonSerializerOptions SerializerOptions { get; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    internal IDictionary<string, string> Properties => Data;

    internal ILogger Logger => logger;

    /// <summary>
    /// Gets the configuration settings the provider uses when accessing the server.
    /// </summary>
    public virtual ConfigServerClientSettings Settings => settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class with default configuration settings.
    /// <see cref="ConfigServerClientSettings" />.
    /// </summary>
    /// <param name="logFactory">
    /// optional logging factory.
    /// </param>
    public ConfigServerConfigurationProvider(ILoggerFactory logFactory = null)
        : this(new ConfigServerClientSettings(), logFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class.
    /// </summary>
    /// <param name="settings">
    /// the configuration settings the provider uses when accessing the server.
    /// </param>
    /// <param name="logFactory">
    /// optional logging factory.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, ILoggerFactory logFactory = null)
    {
        ArgumentGuard.NotNull(settings);

        logFactory ??= BootstrapLoggerFactory.Instance;
        Initialize(settings, logFactory: logFactory);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class.
    /// </summary>
    /// <param name="settings">
    /// the configuration settings the provider uses when accessing the server.
    /// </param>
    /// <param name="httpClient">
    /// a HttpClient the provider uses to make requests of the server.
    /// </param>
    /// <param name="logFactory">
    /// optional logging factory.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerClientSettings settings, HttpClient httpClient, ILoggerFactory logFactory = null)
    {
        ArgumentGuard.NotNull(settings);
        ArgumentGuard.NotNull(httpClient);

        logFactory ??= BootstrapLoggerFactory.Instance;

        Initialize(settings, httpClient: httpClient, logFactory: logFactory);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class from a <see cref="ConfigServerConfigurationSource" />.
    /// </summary>
    /// <param name="source">
    /// the <see cref="ConfigServerConfigurationSource" /> the provider uses when accessing the server.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerConfigurationSource source)
    {
        _ = source.Configuration as IConfigurationRoot;
        Initialize(source);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerConfigurationProvider" /> class from a <see cref="ConfigServerConfigurationSource" />.
    /// </summary>
    /// <param name="source">
    /// the <see cref="ConfigServerConfigurationSource" /> the provider uses when accessing the server.
    /// </param>
    /// <param name="httpClient">
    /// the httpClient to use.
    /// </param>
    public ConfigServerConfigurationProvider(ConfigServerConfigurationSource source, HttpClient httpClient)
    {
        ArgumentGuard.NotNull(httpClient);

        Initialize(source, httpClient);
    }

    internal void Initialize(ConfigServerConfigurationSource source, HttpClient httpClient = null, ILoggerFactory logFactory = null)
    {
        ConfigServerClientSettings newSettings = source.DefaultSettings;
        IConfiguration configurationValue = WrapWithPlaceholderResolver(source.Configuration);
        Initialize(newSettings, configurationValue, httpClient, logFactory);
    }

    internal void Initialize(ConfigServerClientSettings settings, IConfiguration configuration = null, HttpClient httpClient = null,
        ILoggerFactory logFactory = null)
    {
        loggerFactory = logFactory ?? new NullLoggerFactory();
        logger = loggerFactory.CreateLogger<ConfigServerConfigurationProvider>();

        if (configuration != null)
        {
            this.configuration = configuration;
            hasConfiguration = true;
        }
        else
        {
            this.configuration = new ConfigurationBuilder().Build();
            hasConfiguration = false;
        }

        this.settings = settings;
        this.httpClient = httpClient ?? GetHttpClient(this.settings);

        OnSettingsChanged();
    }

    private void OnSettingsChanged()
    {
        TimeSpan existingPollingInterval = settings.PollingInterval;

        if (hasConfiguration)
        {
            ConfigurationSettingsHelper.Initialize(Prefix, settings, configuration);
            configuration.GetReloadToken().RegisterChangeCallback(_ => OnSettingsChanged(), null);
        }

        if (settings.PollingInterval == TimeSpan.Zero)
        {
            refreshTimer?.Dispose();
        }
        else if (refreshTimer == null)
        {
            refreshTimer = new Timer(_ => DoLoad(), null, TimeSpan.Zero, settings.PollingInterval);
        }
        else if (existingPollingInterval != settings.PollingInterval)
        {
            refreshTimer.Change(TimeSpan.Zero, settings.PollingInterval);
        }
    }

    /// <summary>
    /// Loads configuration data from the Spring Cloud Configuration Server as specified by the <see cref="Settings" />.
    /// </summary>
    public override void Load()
    {
        LoadInternal();
    }

    [Obsolete("Will be removed in next release, use the ConfigServerConfigurationSource")]
    public virtual IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var config = new ConfigurationBuilder();

        foreach (IConfigurationSource s in builder.Sources)
        {
            if (s == this)
            {
                break;
            }

            config.Add(s);
        }

        configuration = WrapWithPlaceholderResolver(config.Build());
        ConfigurationSettingsHelper.Initialize(Prefix, settings, configuration);
        return this;
    }

    internal ConfigEnvironment LoadInternal(bool updateDictionary = true)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("Config Server client disabled, did not fetch configuration!");
            return null;
        }

        if (IsDiscoveryFirstEnabled())
        {
            ConfigServerDiscoveryService ??= new ConfigServerDiscoveryService(configuration, settings, loggerFactory);
            DiscoverServerInstances();
        }

        // Adds client settings (e.g spring:cloud:config:uri, etc) to the Data dictionary
        AddConfigServerClientSettings();

        if (settings.RetryEnabled && settings.FailFast)
        {
            int attempts = 0;
            int backOff = settings.RetryInitialInterval;

            do
            {
                logger.LogInformation("Fetching config from server at: {0}", settings.Uri);

                try
                {
                    return DoLoad(updateDictionary);
                }
                catch (ConfigServerException e)
                {
                    logger.LogInformation("Failed fetching config from server at: {0}, Exception: {1}", settings.Uri, e);
                    attempts++;

                    if (attempts < settings.RetryAttempts)
                    {
                        Thread.CurrentThread.Join(backOff);
                        int nextBackOff = (int)(backOff * settings.RetryMultiplier);
                        backOff = Math.Min(nextBackOff, settings.RetryMaxInterval);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            while (true);
        }

        logger.LogInformation("Fetching config from server at: {0}", settings.Uri);
        return DoLoad(updateDictionary);
    }

    internal ConfigEnvironment DoLoad(bool updateDictionary = true)
    {
        Exception error = null;

        // Get arrays of config server uris to check
        string[] uris = settings.GetUris();

        try
        {
            foreach (string label in GetLabels())
            {
                Task<ConfigEnvironment> task = null;

                if (uris.Length > 1)
                {
                    logger.LogInformation("Multiple Config Server Uris listed.");

                    // Invoke config servers
                    task = RemoteLoadAsync(uris, label);
                }
                else
                {
                    // Single, server make Config Server URI from settings
#pragma warning disable CS0618 // Type or member is obsolete
                    string path = GetConfigServerUri(label);

                    // Invoke config server
                    task = RemoteLoadAsync(path);
#pragma warning restore CS0618 // Type or member is obsolete
                }

                // Wait for results from server
                ConfigEnvironment env = task.GetAwaiter().GetResult();

                // Update config Data dictionary with any results
                if (env != null)
                {
                    logger.LogInformation("Located environment: {name}, {profiles}, {label}, {version}, {state}", env.Name, env.Profiles, env.Label,
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

        logger.LogWarning("Could not locate PropertySource: " + error);

        if (settings.FailFast)
        {
            throw new ConfigServerException("Could not locate PropertySource, fail fast property is set, failing", error);
        }

        return null;
    }

    internal string[] GetLabels()
    {
        if (string.IsNullOrWhiteSpace(settings.Label))
        {
            return EmptyLabels;
        }

        return settings.Label.Split(CommaDelimit, StringSplitOptions.RemoveEmptyEntries);
    }

    internal void DiscoverServerInstances()
    {
        IEnumerable<IServiceInstance> instances = ConfigServerDiscoveryService.GetConfigServerInstances();

        if (!instances.Any())
        {
            if (settings.FailFast)
            {
                throw new ConfigServerException("Could not locate config server via discovery, are you missing a Discovery service assembly?");
            }

            return;
        }

        UpdateSettingsFromDiscovery(instances, settings);
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
            string uris = endpoints.ToString(0, endpoints.Length - 1);
            settings.Uri = uris;
        }
    }

    internal async Task ProvideRuntimeReplacementsAsync(IDiscoveryClient discoveryClientFromDi, ILoggerFactory loggerFactory)
    {
        if (ConfigServerDiscoveryService is not null)
        {
            await ConfigServerDiscoveryService.ProvideRuntimeReplacementsAsync(discoveryClientFromDi, loggerFactory);
        }
    }

    internal async Task ShutdownAsync()
    {
        if (ConfigServerDiscoveryService is not null)
        {
            await ConfigServerDiscoveryService.ShutdownAsync();
        }
    }

    /// <summary>
    /// Create the HttpRequestMessage that will be used in accessing the Spring Cloud Configuration server.
    /// </summary>
    /// <param name="requestUri">
    /// the Uri used when accessing the server.
    /// </param>
    /// <param name="username">
    /// username to use if required.
    /// </param>
    /// <param name="password">
    /// password to use if required.
    /// </param>
    /// <returns>
    /// The HttpRequestMessage built from the path.
    /// </returns>
    protected internal virtual HttpRequestMessage GetRequestMessage(string requestUri, string username, string password)
    {
        HttpRequestMessage request = string.IsNullOrEmpty(settings.AccessTokenUri)
            ? HttpClientHelper.GetRequestMessage(HttpMethod.Get, requestUri, username, password)
            : HttpClientHelper.GetRequestMessage(HttpMethod.Get, requestUri, FetchAccessToken);

        if (!string.IsNullOrEmpty(settings.Token) && !ConfigServerClientSettings.IsMultiServerConfig(settings.Uri))
        {
            if (!settings.DisableTokenRenewal)
            {
                RenewToken(settings.Token);
            }

            request.Headers.Add(TokenHeader, settings.Token);
        }

        return request;
    }

    /// <summary>
    /// Create the HttpRequestMessage that will be used in accessing the Spring Cloud Configuration server.
    /// </summary>
    /// <param name="requestUri">
    /// the Uri used when accessing the server.
    /// </param>
    /// <returns>
    /// The HttpRequestMessage built from the path.
    /// </returns>
    [Obsolete("Will be removed in next release. See GetRequestMessage(string, string, string)")]
    protected internal virtual HttpRequestMessage GetRequestMessage(string requestUri)
    {
        return GetRequestMessage(requestUri, settings.Username, settings.Password);
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
    protected internal virtual void AddConfigServerClientSettings(IDictionary<string, string> data)
    {
        CultureInfo culture = CultureInfo.InvariantCulture;
        data["spring:cloud:config:enabled"] = settings.Enabled.ToString(culture);
        data["spring:cloud:config:failFast"] = settings.FailFast.ToString(culture);
        data["spring:cloud:config:env"] = settings.Environment;
        data["spring:cloud:config:label"] = settings.Label;
        data["spring:cloud:config:name"] = settings.Name;
        data["spring:cloud:config:password"] = settings.Password;
        data["spring:cloud:config:uri"] = settings.Uri;
        data["spring:cloud:config:username"] = settings.Username;
        data["spring:cloud:config:token"] = settings.Token;
        data["spring:cloud:config:timeout"] = settings.Timeout.ToString(culture);
        data["spring:cloud:config:validate_certificates"] = settings.ValidateCertificates.ToString(culture);
        data["spring:cloud:config:retry:enabled"] = settings.RetryEnabled.ToString(culture);
        data["spring:cloud:config:retry:maxAttempts"] = settings.RetryAttempts.ToString(culture);
        data["spring:cloud:config:retry:initialInterval"] = settings.RetryInitialInterval.ToString(culture);
        data["spring:cloud:config:retry:maxInterval"] = settings.RetryMaxInterval.ToString(culture);
        data["spring:cloud:config:retry:multiplier"] = settings.RetryMultiplier.ToString(culture);

        data["spring:cloud:config:access_token_uri"] = settings.AccessTokenUri;
        data["spring:cloud:config:client_secret"] = settings.ClientSecret;
        data["spring:cloud:config:client_id"] = settings.ClientId;
        data["spring:cloud:config:tokenTtl"] = settings.TokenTtl.ToString(culture);
        data["spring:cloud:config:tokenRenewRate"] = settings.TokenRenewRate.ToString(culture);
        data["spring:cloud:config:disableTokenRenewal"] = settings.DisableTokenRenewal.ToString(culture);

        data["spring:cloud:config:discovery:enabled"] = settings.DiscoveryEnabled.ToString(culture);
        data["spring:cloud:config:discovery:serviceId"] = settings.DiscoveryServiceId.ToString(culture);

        data["spring:cloud:config:health:enabled"] = settings.HealthEnabled.ToString(culture);
        data["spring:cloud:config:health:timeToLive"] = settings.HealthTimeToLive.ToString(culture);
    }

    protected internal async Task<ConfigEnvironment> RemoteLoadAsync(string[] requestUris, string label)
    {
        // Get client if not already set
        httpClient ??= GetHttpClient(settings);

        Exception error = null;

        foreach (string requestUri in requestUris)
        {
            error = null;

            // Get a config server uri and username passwords to use
            string trimUri = requestUri.Trim();
            string serverUri = settings.GetRawUri(trimUri);
            string username = settings.GetUserName(trimUri);
            string password = settings.GetPassword(trimUri);

            // Make Config Server URI from settings
            string path = GetConfigServerUri(serverUri, label);

            // Get the request message
            HttpRequestMessage request = GetRequestMessage(path, username, password);

            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(settings.ValidateCertificates, out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            // Invoke config server
            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);

                // Log status
                string message = $"Config Server returned status: {response.StatusCode} invoking path: {requestUri}";
                logger.LogInformation(WebUtility.UrlEncode(message));

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

                return await response.Content.ReadFromJsonAsync<ConfigEnvironment>(SerializerOptions).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                error = e;
                logger.LogError(e, "Config Server exception, path: {requestUri}", WebUtility.UrlEncode(requestUri));

                if (IsContinueExceptionType(e))
                {
                    continue;
                }

                throw;
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(settings.ValidateCertificates, prevProtocols, prevValidator);
            }
        }

        if (error != null)
        {
            throw error;
        }

        return null;
    }

    /// <summary>
    /// Asynchronously calls the Spring Cloud Configuration Server using the provided Uri and returning a a task that can be used to obtain the results.
    /// </summary>
    /// <param name="requestUri">
    /// the Uri used in accessing the Spring Cloud Configuration Server.
    /// </param>
    /// <returns>
    /// The task object representing the asynchronous operation.
    /// </returns>
    [Obsolete("Will be removed in next release. See RemoteLoadAsync(string[], string)")]
    protected internal virtual async Task<ConfigEnvironment> RemoteLoadAsync(string requestUri)
    {
        // Get client if not already set
        httpClient ??= GetHttpClient(settings);

        // Get the request message
        HttpRequestMessage request = GetRequestMessage(requestUri);

        // If certificate validation is disabled, inject a callback to handle properly
        HttpClientHelper.ConfigureCertificateValidation(settings.ValidateCertificates, out SecurityProtocolType prevProtocols,
            out RemoteCertificateValidationCallback prevValidator);

        // Invoke config server
        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                // Log status
                string message = $"Config Server returned status: {response.StatusCode} invoking path: {requestUri}";

                logger.LogInformation(WebUtility.UrlEncode(message));

                // Throw if status >= 400
                if (response.StatusCode >= HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException(message);
                }

                return null;
            }

            return await response.Content.ReadFromJsonAsync<ConfigEnvironment>(SerializerOptions).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // Log and rethrow
            logger.LogError("Config Server exception: {0}, path: {1}", e, WebUtility.UrlEncode(requestUri));
            throw;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(settings.ValidateCertificates, prevProtocols, prevValidator);
        }
    }

    /// <summary>
    /// Create the Uri that will be used in accessing the Configuration Server.
    /// </summary>
    /// <param name="baseRawUri">
    /// base server uri to use.
    /// </param>
    /// <param name="label">
    /// a label to add.
    /// </param>
    /// <returns>
    /// The request URI for the Configuration Server.
    /// </returns>
    protected internal virtual string GetConfigServerUri(string baseRawUri, string label)
    {
        ArgumentGuard.NotNullOrEmpty(baseRawUri);

        string path = $"{settings.Name}/{settings.Environment}";

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
    /// Create the Uri that will be used in accessing the Configuration Server.
    /// </summary>
    /// <param name="label">
    /// a label to add.
    /// </param>
    /// <returns>
    /// The request URI for the Configuration Server.
    /// </returns>
    [Obsolete("Will be removed in next release. See GetConfigServerUri(string, string)")]
    protected internal virtual string GetConfigServerUri(string label)
    {
        return GetConfigServerUri(settings.RawUri, label);
    }

    /// <summary>
    /// Adds values from a PropertySource to the Configuration Data dictionary managed by this provider.
    /// </summary>
    /// <param name="source">
    /// a property source to add.
    /// </param>
    [Obsolete("Will be removed in next release.")]
    protected internal virtual void AddPropertySource(PropertySource source)
    {
        AddPropertySource(source, Data);
    }

    /// <summary>
    /// Adds values from a PropertySource to the provided dictionary.
    /// </summary>
    /// <param name="source">
    /// a property source to add.
    /// </param>
    /// <param name="data">
    /// the dictionary to add the property source to.
    /// </param>
    protected internal void AddPropertySource(PropertySource source, IDictionary<string, string> data)
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
                data[key] = value;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Config Server exception, property: {0}={1}", kvp.Key, kvp.Value.GetType());
            }
        }
    }

    protected internal virtual string ConvertKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }

        string[] split = Split(key);
        var sb = new StringBuilder();

        foreach (string part in split)
        {
            string keyPart = ConvertArrayKey(part);
            sb.Append(keyPart);
            sb.Append(ConfigurationPath.KeyDelimiter);
        }

        return sb.ToString(0, sb.Length - 1);
    }

    protected internal virtual string[] Split(string source)
    {
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

            if (!readEscapeChar && source[i] == DelimiterChar)
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
            return src.Replace(EscapeString + DelimiterString, DelimiterString).Replace(EscapeString + EscapeString, EscapeString);
        }
    }

    protected internal virtual string ConvertArrayKey(string key)
    {
        return Regex.Replace(key, ArrayPattern, match =>
        {
            string result = match.Value.Replace("[", ":").Replace("]", string.Empty);
            return result;
        });
    }

    protected internal virtual string ConvertValue(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Encode the username password for a http request.
    /// </summary>
    /// <param name="user">
    /// the username.
    /// </param>
    /// <param name="password">
    /// the password.
    /// </param>
    /// <returns>
    /// Encoded user + password.
    /// </returns>
    protected internal string GetEncoded(string user, string password)
    {
        return HttpClientHelper.GetEncodedUserPassword(user, password);
    }

    protected internal virtual void RenewToken(string token)
    {
        tokenRenewTimer ??= new Timer(RefreshVaultToken, null, TimeSpan.FromMilliseconds(settings.TokenRenewRate),
            TimeSpan.FromMilliseconds(settings.TokenRenewRate));
    }

    /// <summary>
    /// Conduct the OAuth2 client_credentials grant flow returning a task that can be used to obtain the results.
    /// </summary>
    /// <returns>
    /// The task object representing asynchronous operation.
    /// </returns>
    protected internal string FetchAccessToken()
    {
        if (string.IsNullOrEmpty(settings.AccessTokenUri))
        {
            return null;
        }

        return HttpClientHelper.GetAccessTokenAsync(settings.AccessTokenUri, settings.ClientId, settings.ClientSecret, settings.Timeout,
            settings.ValidateCertificates, httpClient, logger).GetAwaiter().GetResult();
    }

    // fire and forget
#pragma warning disable S3168 // "async" methods should not return "void"
    protected internal async void RefreshVaultToken(object state)
#pragma warning restore S3168 // "async" methods should not return "void"
    {
        if (string.IsNullOrEmpty(Settings.Token))
        {
            return;
        }

        string obscuredToken = $"{Settings.Token.Substring(0, 4)}[*]{Settings.Token.Substring(Settings.Token.Length - 4)}";

        // If certificate validation is disabled, inject a callback to handle properly
        HttpClientHelper.ConfigureCertificateValidation(settings.ValidateCertificates, out SecurityProtocolType prevProtocols,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            httpClient ??= GetHttpClient(Settings);

            string uri = GetVaultRenewUri();
            HttpRequestMessage message = GetVaultRenewMessage(uri);

            logger.LogInformation("Renewing Vault token {0} for {1} milliseconds at Uri {2}", obscuredToken, Settings.TokenTtl, uri);

            using HttpResponseMessage response = await httpClient.SendAsync(message).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning("Renewing Vault token {0} returned status: {1}", obscuredToken, response.StatusCode);
            }
        }
        catch (Exception e)
        {
            logger.LogError("Unable to renew Vault token {0}. Is the token invalid or expired? - {1}", obscuredToken, e);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(settings.ValidateCertificates, prevProtocols, prevValidator);
        }
    }

    protected internal virtual string GetVaultRenewUri()
    {
        string rawUri = Settings.RawUris[0];

        if (!rawUri.EndsWith("/"))
        {
            rawUri += "/";
        }

        return rawUri + VaultRenewPath;
    }

    protected internal virtual HttpRequestMessage GetVaultRenewMessage(string requestUri)
    {
        HttpRequestMessage request = HttpClientHelper.GetRequestMessage(HttpMethod.Post, requestUri, FetchAccessToken);

        if (!string.IsNullOrEmpty(Settings.Token))
        {
            request.Headers.Add(VaultTokenHeader, Settings.Token);
        }

        int renewTtlSeconds = Settings.TokenTtl / 1000;
        string json = $"{{\"increment\":{renewTtlSeconds}}}";

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Content = content;
        return request;
    }

    protected internal bool IsDiscoveryFirstEnabled()
    {
        IConfigurationSection clientConfigSection = configuration.GetSection(Prefix);
        return clientConfigSection.GetValue("discovery:enabled", settings.DiscoveryEnabled);
    }

    /// <summary>
    /// Creates an appropriately configured HttpClient that will be used in communicating with the Spring Cloud Configuration Server.
    /// </summary>
    /// <param name="settings">
    /// the settings used in configuring the HttpClient.
    /// </param>
    /// <returns>
    /// The HttpClient used by the provider.
    /// </returns>
    protected static HttpClient GetHttpClient(ConfigServerClientSettings settings)
    {
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
        var root = configuration as IConfigurationRoot;

        if (root.Providers.LastOrDefault() is PlaceholderResolverProvider)
        {
            return configuration;
        }

        return new ConfigurationRoot(new List<IConfigurationProvider>
        {
            new PlaceholderResolverProvider(new List<IConfigurationProvider>(root.Providers))
        });
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
