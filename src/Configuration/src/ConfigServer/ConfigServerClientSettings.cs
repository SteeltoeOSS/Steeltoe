// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

/// <summary>
/// Holds the settings used to configure the Spring Cloud Config Server provider <see cref="ConfigServerConfigurationProvider" />.
/// </summary>
public sealed class ConfigServerClientSettings
{
    private const char ColonDelimiter = ':';
    private const char CommaDelimiter = ',';

    /// <summary>
    /// Default Config Server address used by provider.
    /// </summary>
    public const string DefaultUri = "http://localhost:8888";

    /// <summary>
    /// Default environment used when accessing configuration data.
    /// </summary>
    public const string DefaultEnvironment = "Production";

    /// <summary>
    /// Default fail-fast setting.
    /// </summary>
    public const bool DefaultFailFast = false;

    /// <summary>
    /// Default Config Server provider enabled setting.
    /// </summary>
    public const bool DefaultProviderEnabled = true;

    /// <summary>
    /// Default certificate validation enabled setting.
    /// </summary>
    public const bool DefaultCertificateValidation = true;

    /// <summary>
    /// Default number of retries to be attempted.
    /// </summary>
    public const int DefaultMaxRetryAttempts = 6;

    /// <summary>
    /// Default initial retry interval in milliseconds.
    /// </summary>
    public const int DefaultInitialRetryInterval = 1000;

    /// <summary>
    /// Default multiplier for next retry interval.
    /// </summary>
    public const double DefaultRetryMultiplier = 1.1;

    /// <summary>
    /// Default initial retry interval in milliseconds.
    /// </summary>
    public const int DefaultMaxRetryInterval = 2000;

    /// <summary>
    /// Default retry enabled setting.
    /// </summary>
    public const bool DefaultRetryEnabled = false;

    /// <summary>
    /// Default timeout in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMilliseconds = 6 * 1000;

    /// <summary>
    /// Default Vault Token time-to-live setting.
    /// </summary>
    public const int DefaultVaultTokenTtl = 300000;

    /// <summary>
    /// Default Vault Token renewal rate.
    /// </summary>
    public const int DefaultVaultTokenRenewRate = 60000;

    /// <summary>
    /// Default Disable Vault Token renewal.
    /// </summary>
    public const bool DefaultDisableTokenRenewal = false;

    /// <summary>
    /// Default address used by provider to obtain a OAuth Access Token.
    /// </summary>
    public const string DefaultAccessTokenUri = null;

    /// <summary>
    /// Default client id used by provider to obtain a OAuth Access Token.
    /// </summary>
    public const string DefaultClientId = null;

    /// <summary>
    /// Default client secret used by provider to obtain a OAuth Access Token.
    /// </summary>
    public const string DefaultClientSecret = null;

    /// <summary>
    /// Default discovery first enabled setting.
    /// </summary>
    public const bool DefaultDiscoveryEnabled = false;

    /// <summary>
    /// Default discovery first service id setting.
    /// </summary>
    public const string DefaultConfigserverServiceId = "configserver";

    /// <summary>
    /// Default health check enabled setting.
    /// </summary>
    public const bool DefaultHealthEnabled = true;

    /// <summary>
    /// Default health check time-to-live setting, in milliseconds.
    /// </summary>
    public const long DefaultHealthTimeToLive = 60 * 5 * 1000;

    private string _username;
    private string _password;

    /// <summary>
    /// Gets or sets the Config Server address.
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Config Server provider is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the environment used when accessing configuration data.
    /// </summary>
    public string Environment { get; set; }

    /// <summary>
    /// Gets or sets the application name used when accessing configuration data.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the label used when accessing configuration data.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the frequency with which app should check Config Server for changes in configuration.
    /// </summary>
    public TimeSpan PollingInterval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether fail-fast behavior is enabled.
    /// </summary>
    public bool FailFast { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider validates server certificates.
    /// </summary>
    public bool ValidateCertificates { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether retries are enabled on failures.
    /// </summary>
    public bool RetryEnabled { get; set; }

    /// <summary>
    /// Gets or sets initial retry interval in milliseconds.
    /// </summary>
    public int RetryInitialInterval { get; set; }

    /// <summary>
    /// Gets or sets max retry interval in milliseconds.
    /// </summary>
    public int RetryMaxInterval { get; set; }

    /// <summary>
    /// Gets or sets the multiplier for next retry interval.
    /// </summary>
    public double RetryMultiplier { get; set; }

    /// <summary>
    /// Gets or sets the max number of retries the client will attempt.
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether discovery first behavior is enabled.
    /// </summary>
    public bool DiscoveryEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value of the service ID used during discovery first behavior.
    /// </summary>
    public string DiscoveryServiceId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether health check is enabled.
    /// </summary>
    public bool HealthEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value for the health check cache time-to-live.
    /// </summary>
    public long HealthTimeToLive { get; set; }

    /// <summary>
    /// Gets unescaped <see cref="UriComponents.HttpRequestUrl" />s.
    /// </summary>
    public string[] RawUris => GetRawUris();

    /// <summary>
    /// Gets or sets the token used for Vault.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Gets or sets the request timeout in milliseconds.
    /// </summary>
    public int Timeout { get; set; }

    /// <summary>
    /// Gets or sets the address used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string AccessTokenUri { get; set; } = DefaultAccessTokenUri;

    /// <summary>
    /// Gets or sets the client ID used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string ClientId { get; set; } = DefaultClientId;

    /// <summary>
    /// Gets or sets the client secret used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string ClientSecret { get; set; } = DefaultClientSecret;

    public X509Certificate2 ClientCertificate { get; set; }

    /// <summary>
    /// Gets or sets the vault token time-to-live in milliseconds.
    /// </summary>
    public int TokenTtl { get; set; } = DefaultVaultTokenTtl;

    /// <summary>
    /// Gets or sets the vault token renew rate in milliseconds.
    /// </summary>
    public int TokenRenewRate { get; set; } = DefaultVaultTokenRenewRate;

    public bool DisableTokenRenewal { get; set; } = DefaultDisableTokenRenewal;

    /// <summary>
    /// Gets headers that will be added to the Config Server request.
    /// </summary>
    public Dictionary<string, string> Headers { get; } = new();

    /// <summary>
    /// Gets or sets the username used when accessing the Config Server.
    /// </summary>
    public string Username
    {
        get => GetUserName(Uri);
        set => _username = value;
    }

    /// <summary>
    /// Gets or sets the password used when accessing the Config Server.
    /// </summary>
    public string Password
    {
        get => GetPassword(Uri);
        set => _password = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerClientSettings" /> class.
    /// </summary>
    /// <remarks>
    /// Initializes the Config Server client settings with defaults.
    /// </remarks>
    public ConfigServerClientSettings()
    {
        ValidateCertificates = DefaultCertificateValidation;
        FailFast = DefaultFailFast;
        Environment = DefaultEnvironment;
        Enabled = DefaultProviderEnabled;
        Uri = DefaultUri;
        RetryEnabled = DefaultRetryEnabled;
        RetryInitialInterval = DefaultInitialRetryInterval;
        RetryMaxInterval = DefaultMaxRetryInterval;
        RetryAttempts = DefaultMaxRetryAttempts;
        RetryMultiplier = DefaultRetryMultiplier;
        Timeout = DefaultTimeoutMilliseconds;
        DiscoveryEnabled = DefaultDiscoveryEnabled;
        DiscoveryServiceId = DefaultConfigserverServiceId;
        HealthEnabled = DefaultHealthEnabled;
        HealthTimeToLive = DefaultHealthTimeToLive;
    }

    internal static bool IsMultiServerConfiguration(string uris)
    {
        return uris.Contains(CommaDelimiter);
    }

    internal string GetRawUri(string uri)
    {
        try
        {
            var tempUri = new Uri(uri);
            return tempUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    private string[] GetRawUris()
    {
        if (!string.IsNullOrEmpty(Uri))
        {
            string[] uris = Uri.Split(CommaDelimiter, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < uris.Length; i++)
            {
                string uri = GetRawUri(uris[i]);

                if (string.IsNullOrEmpty(uri))
                {
                    return Array.Empty<string>();
                }

                uris[i] = uri;
            }

            return uris;
        }

        return Array.Empty<string>();
    }

    internal string[] GetUris()
    {
        if (!string.IsNullOrEmpty(Uri))
        {
            return Uri.Split(CommaDelimiter, StringSplitOptions.RemoveEmptyEntries);
        }

        return Array.Empty<string>();
    }

    internal string GetPassword(string uri)
    {
        if (!string.IsNullOrEmpty(_password))
        {
            return _password;
        }

        return GetUserPassElement(uri, 1);
    }

    internal string GetUserName(string uri)
    {
        if (!string.IsNullOrEmpty(_username))
        {
            return _username;
        }

        return GetUserPassElement(uri, 0);
    }

    private static string GetUserInfo(string uri)
    {
        if (!string.IsNullOrEmpty(uri))
        {
            var u = new Uri(uri);
            return u.UserInfo;
        }

        return null;
    }

    private static string GetUserPassElement(string uri, int index)
    {
        if (!IsMultiServerConfiguration(uri))
        {
            string userInfo = GetUserInfo(uri);

            if (!string.IsNullOrEmpty(userInfo))
            {
                string[] info = userInfo.Split(ColonDelimiter);

                if (info.Length > index)
                {
                    return info[index];
                }
            }
        }

        return null;
    }
}
