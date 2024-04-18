// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Configuration.ConfigServer;

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
    internal const string DefaultUri = "http://localhost:8888";

    /// <summary>
    /// Default environment used when accessing configuration data.
    /// </summary>
    internal const string DefaultEnvironment = "Production";

    /// <summary>
    /// Default fail-fast setting.
    /// </summary>
    internal const bool DefaultFailFast = false;

    /// <summary>
    /// Default Config Server provider enabled setting.
    /// </summary>
    internal const bool DefaultProviderEnabled = true;

    /// <summary>
    /// Default certificate validation enabled setting.
    /// </summary>
    internal const bool DefaultCertificateValidation = true;

    /// <summary>
    /// Default number of retries to be attempted.
    /// </summary>
    internal const int DefaultMaxRetryAttempts = 6;

    /// <summary>
    /// Default initial retry interval in milliseconds.
    /// </summary>
    internal const int DefaultInitialRetryInterval = 1000;

    /// <summary>
    /// Default multiplier for next retry interval.
    /// </summary>
    internal const double DefaultRetryMultiplier = 1.1;

    /// <summary>
    /// Default initial retry interval in milliseconds.
    /// </summary>
    internal const int DefaultMaxRetryInterval = 2000;

    /// <summary>
    /// Default retry enabled setting.
    /// </summary>
    internal const bool DefaultRetryEnabled = false;

    /// <summary>
    /// Default timeout in milliseconds.
    /// </summary>
    internal const int DefaultTimeoutMilliseconds = 60 * 1000;

    /// <summary>
    /// Default Vault Token time-to-live setting.
    /// </summary>
    internal const int DefaultVaultTokenTtl = 300000;

    /// <summary>
    /// Default Vault Token renewal rate.
    /// </summary>
    internal const int DefaultVaultTokenRenewRate = 60000;

    /// <summary>
    /// Default Disable Vault Token renewal.
    /// </summary>
    internal const bool DefaultDisableTokenRenewal = false;

    /// <summary>
    /// Default address used by provider to obtain a OAuth Access Token.
    /// </summary>
    internal const string? DefaultAccessTokenUri = null;

    /// <summary>
    /// Default client id used by provider to obtain a OAuth Access Token.
    /// </summary>
    internal const string? DefaultClientId = null;

    /// <summary>
    /// Default client secret used by provider to obtain a OAuth Access Token.
    /// </summary>
    internal const string? DefaultClientSecret = null;

    /// <summary>
    /// Default discovery first enabled setting.
    /// </summary>
    internal const bool DefaultDiscoveryEnabled = false;

    /// <summary>
    /// Default discovery first service id setting.
    /// </summary>
    internal const string DefaultConfigserverServiceId = "configserver";

    /// <summary>
    /// Default health check enabled setting.
    /// </summary>
    internal const bool DefaultHealthEnabled = true;

    /// <summary>
    /// Default health check time-to-live setting, in milliseconds.
    /// </summary>
    internal const long DefaultHealthTimeToLive = 60 * 5 * 1000;

    private string? _username;
    private string? _password;

    /// <summary>
    /// Gets or sets the Config Server address.
    /// </summary>
    public string? Uri { get; set; } = DefaultUri;

    /// <summary>
    /// Gets or sets a value indicating whether the Config Server provider is enabled.
    /// </summary>
    public bool Enabled { get; set; } = DefaultProviderEnabled;

    /// <summary>
    /// Gets or sets the environment used when accessing configuration data.
    /// </summary>
    public string? Environment { get; set; } = DefaultEnvironment;

    /// <summary>
    /// Gets or sets the application name used when accessing configuration data.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the label used when accessing configuration data.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the frequency with which app should check Config Server for changes in configuration.
    /// </summary>
    public TimeSpan PollingInterval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether fail-fast behavior is enabled.
    /// </summary>
    public bool FailFast { get; set; } = DefaultFailFast;

    /// <summary>
    /// Gets or sets a value indicating whether the provider validates server certificates.
    /// </summary>
    public bool ValidateCertificates { get; set; } = DefaultCertificateValidation;

    /// <summary>
    /// Gets or sets a value indicating whether retries are enabled on failures.
    /// </summary>
    public bool RetryEnabled { get; set; } = DefaultRetryEnabled;

    /// <summary>
    /// Gets or sets initial retry interval in milliseconds.
    /// </summary>
    public int RetryInitialInterval { get; set; } = DefaultInitialRetryInterval;

    /// <summary>
    /// Gets or sets max retry interval in milliseconds.
    /// </summary>
    public int RetryMaxInterval { get; set; } = DefaultMaxRetryInterval;

    /// <summary>
    /// Gets or sets the multiplier for next retry interval.
    /// </summary>
    public double RetryMultiplier { get; set; } = DefaultRetryMultiplier;

    /// <summary>
    /// Gets or sets the max number of retries the client will attempt.
    /// </summary>
    public int RetryAttempts { get; set; } = DefaultMaxRetryAttempts;

    /// <summary>
    /// Gets or sets a value indicating whether discovery first behavior is enabled.
    /// </summary>
    public bool DiscoveryEnabled { get; set; } = DefaultDiscoveryEnabled;

    /// <summary>
    /// Gets or sets a value of the service ID used during discovery first behavior.
    /// </summary>
    public string? DiscoveryServiceId { get; set; } = DefaultConfigserverServiceId;

    /// <summary>
    /// Gets or sets a value indicating whether health check is enabled.
    /// </summary>
    public bool HealthEnabled { get; set; } = DefaultHealthEnabled;

    /// <summary>
    /// Gets or sets a value for the health check cache time-to-live.
    /// </summary>
    public long HealthTimeToLive { get; set; } = DefaultHealthTimeToLive;

    /// <summary>
    /// Gets unescaped <see cref="UriComponents.HttpRequestUrl" />s.
    /// </summary>
    public IList<string> RawUris => GetRawUris();

    /// <summary>
    /// Gets or sets the token used for Vault.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the request timeout in milliseconds.
    /// </summary>
    public int Timeout { get; set; } = DefaultTimeoutMilliseconds;

    /// <summary>
    /// Gets or sets the address used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string? AccessTokenUri { get; set; } = DefaultAccessTokenUri;

    /// <summary>
    /// Gets or sets the client ID used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string? ClientId { get; set; } = DefaultClientId;

    /// <summary>
    /// Gets or sets the client secret used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string? ClientSecret { get; set; } = DefaultClientSecret;

    public X509Certificate2? ClientCertificate { get; set; }

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
    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the username used when accessing the Config Server.
    /// </summary>
    public string? Username
    {
        get => GetUserName(Uri);
        set => _username = value;
    }

    /// <summary>
    /// Gets or sets the password used when accessing the Config Server.
    /// </summary>
    public string? Password
    {
        get => GetPassword(Uri);
        set => _password = value;
    }

    internal static bool IsMultiServerConfiguration(string uris)
    {
        return uris.Contains(CommaDelimiter);
    }

    internal static string? GetRawUri(string uri)
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

    private IList<string> GetRawUris()
    {
        if (!string.IsNullOrEmpty(Uri))
        {
            string[] uris = Uri.Split(CommaDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return uris.Select(GetRawUri).Where(uri => !string.IsNullOrEmpty(uri)).Cast<string>().ToList();
        }

        return [];
    }

    internal IList<string> GetUris()
    {
        return !string.IsNullOrEmpty(Uri) ? Uri.Split(CommaDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : [];
    }

    internal string? GetPassword(string? uri)
    {
        if (!string.IsNullOrEmpty(_password))
        {
            return _password;
        }

        return GetUserPassElement(uri, 1);
    }

    internal string? GetUserName(string? uri)
    {
        if (!string.IsNullOrEmpty(_username))
        {
            return _username;
        }

        return GetUserPassElement(uri, 0);
    }

    private static string? GetUserPassElement(string? uri, int index)
    {
        if (!string.IsNullOrEmpty(uri) && !IsMultiServerConfiguration(uri))
        {
            string userInfo = new Uri(uri).UserInfo;

            if (!string.IsNullOrEmpty(userInfo))
            {
                string[] segments = userInfo.Split(ColonDelimiter);

                if (segments.Length > index)
                {
                    return segments[index];
                }
            }
        }

        return null;
    }
}
