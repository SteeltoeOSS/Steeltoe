// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

/// <summary>
/// Holds the settings used to configure the Spring Cloud Config Server provider <see cref="ConfigServerConfigurationProvider" />.
/// </summary>
public class ConfigServerClientSettings
{
    /// <summary>
    /// Default Config Server address used by provider.
    /// </summary>
    public const string DefaultUri = "http://localhost:8888";

    /// <summary>
    /// Default environment used when accessing configuration data.
    /// </summary>
    public const string DefaultEnvironment = "Production";

    /// <summary>
    /// Default fail fast setting.
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
    /// Default Vault Token Time to Live setting.
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
    /// Default health check time to live in milliseconds setting.
    /// </summary>
    public const long DefaultHealthTimeToLive = 60 * 5 * 1000;

    private static readonly char[] ColonDelimit =
    {
        ':'
    };

    private static readonly char[] CommaDelimit =
    {
        ','
    };

    private string _username;
    private string _password;

    /// <summary>
    /// Gets or sets the Config Server address.
    /// </summary>
    public virtual string Uri { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enables/Disables the Config Server provider.
    /// </summary>
    public virtual bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the environment used when accessing configuration data.
    /// </summary>
    public virtual string Environment { get; set; }

    /// <summary>
    /// Gets or sets the application name used when accessing configuration data.
    /// </summary>
    public virtual string Name { get; set; }

    /// <summary>
    /// Gets or sets the label used when accessing configuration data.
    /// </summary>
    public virtual string Label { get; set; }

    /// <summary>
    /// Gets or sets the frequency with which app should check config server for changes in configuration.
    /// </summary>
    public virtual TimeSpan PollingInterval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enables/Disables failfast behavior.
    /// </summary>
    public virtual bool FailFast { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enables/Disables whether provider validates server certificates.
    /// </summary>
    public virtual bool ValidateCertificates { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enables/Disables config server client retry on failures.
    /// </summary>
    public virtual bool RetryEnabled { get; set; }

    /// <summary>
    /// Gets or sets initial retry interval in milliseconds.
    /// </summary>
    public virtual int RetryInitialInterval { get; set; }

    /// <summary>
    /// Gets or sets max retry interval in milliseconds.
    /// </summary>
    public virtual int RetryMaxInterval { get; set; }

    /// <summary>
    /// Gets or sets multiplier for next retry interval.
    /// </summary>
    public virtual double RetryMultiplier { get; set; }

    /// <summary>
    /// Gets or sets the max number of retries the client will attempt.
    /// </summary>
    public virtual int RetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether discovery first behavior is enabled.
    /// </summary>
    public virtual bool DiscoveryEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value of the service id used during discovery first behavior.
    /// </summary>
    public virtual string DiscoveryServiceId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether health check is enabled.
    /// </summary>
    public virtual bool HealthEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value for the health check cache time to live.
    /// </summary>
    public virtual long HealthTimeToLive { get; set; }

    /// <summary>
    /// Gets returns HttpRequestUrls, unescaped.
    /// </summary>
    public virtual string[] RawUris => GetRawUris();

    /// <summary>
    /// Gets or sets returns the token use for Vault.
    /// </summary>
    public virtual string Token { get; set; }

    /// <summary>
    /// Gets or sets returns the request timeout in milliseconds.
    /// </summary>
    public virtual int Timeout { get; set; }

    /// <summary>
    /// Gets or sets address used by provider to obtain a OAuth Access Token.
    /// </summary>
    public virtual string AccessTokenUri { get; set; } = DefaultAccessTokenUri;

    /// <summary>
    /// Gets or sets client id used by provider to obtain a OAuth Access Token.
    /// </summary>
    public virtual string ClientId { get; set; } = DefaultClientId;

    /// <summary>
    /// Gets or sets client secret used by provider to obtain a OAuth Access Token.
    /// </summary>
    public virtual string ClientSecret { get; set; } = DefaultClientSecret;

    public virtual X509Certificate2 ClientCertificate { get; set; }

    /// <summary>
    /// Gets or sets vault token Time to Live setting in Milliseconds.
    /// </summary>
    public virtual int TokenTtl { get; set; } = DefaultVaultTokenTtl;

    /// <summary>
    /// Gets or sets vault token renew rate in Milliseconds.
    /// </summary>
    public virtual int TokenRenewRate { get; set; } = DefaultVaultTokenRenewRate;

    public virtual bool DisableTokenRenewal { get; set; } = DefaultDisableTokenRenewal;

    /// <summary>
    /// Gets or sets headers that will be added to the config server request.
    /// </summary>
    public virtual Dictionary<string, string> Headers { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerClientSettings" /> class.
    /// </summary>
    /// <remarks>
    /// Initialize Config Server client settings with defaults.
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

    internal static bool IsMultiServerConfig(string uris)
    {
        return uris.Contains(",");
    }

    internal string GetRawUri(string uri)
    {
        try
        {
            var ri = new Uri(uri);
            return ri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    internal string[] GetRawUris()
    {
        if (!string.IsNullOrEmpty(Uri))
        {
            string[] uris = Uri.Split(CommaDelimit, StringSplitOptions.RemoveEmptyEntries);

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
            return Uri.Split(CommaDelimit, StringSplitOptions.RemoveEmptyEntries);
        }

        return Array.Empty<string>();
    }

    internal string GetPassword()
    {
        return GetPassword(Uri);
    }

    internal string GetPassword(string uri)
    {
        if (!string.IsNullOrEmpty(_password))
        {
            return _password;
        }

        return GetUserPassElement(uri, 1);
    }

    internal string GetUserName()
    {
        return GetUserName(Uri);
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
        if (IsMultiServerConfig(uri))
        {
            return null;
        }

        string result = null;
        string userInfo = GetUserInfo(uri);

        if (!string.IsNullOrEmpty(userInfo))
        {
            string[] info = userInfo.Split(ColonDelimit);

            if (info.Length > index)
            {
                result = info[index];
            }
        }

        return result;
    }

#pragma warning disable S4275 // Getters and setters should access the expected fields
    /// <summary>
    /// Gets or sets the username used when accessing the Config Server.
    /// </summary>
    public virtual string Username
    {
        get => GetUserName();
        set => _username = value;
    }

    /// <summary>
    /// Gets or sets the password used when accessing the Config Server.
    /// </summary>
    public virtual string Password
    {
        get => GetPassword();
        set => _password = value;
    }
#pragma warning restore S4275 // Getters and setters should access the expected fields
}
