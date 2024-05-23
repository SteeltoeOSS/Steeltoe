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

    private string? _username;
    private string? _password;

    /// <summary>
    /// Gets or sets the Config Server address. Default value: "http://localhost:8888".
    /// </summary>
    public string? Uri { get; set; } = "http://localhost:8888";

    /// <summary>
    /// Gets or sets a value indicating whether the Config Server provider is enabled. Default value: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the environment used when accessing configuration data. Default value: "Production".
    /// </summary>
    public string? Environment { get; set; } = "Production";

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
    /// Gets or sets a value indicating whether fail-fast behavior is enabled. Default value: false.
    /// </summary>
    public bool FailFast { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider validates server certificates. Default value: true.
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether retries are enabled on failures. Default value: false.
    /// </summary>
    public bool RetryEnabled { get; set; }

    /// <summary>
    /// Gets or sets initial retry interval in milliseconds. Default value: 1000.
    /// </summary>
    public int RetryInitialInterval { get; set; } = 1000;

    /// <summary>
    /// Gets or sets max retry interval in milliseconds. Default value: 2000.
    /// </summary>
    public int RetryMaxInterval { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the multiplier for next retry interval. Default value: 1.1.
    /// </summary>
    public double RetryMultiplier { get; set; } = 1.1;

    /// <summary>
    /// Gets or sets the max number of retries the client will attempt. Default value: 6.
    /// </summary>
    public int RetryAttempts { get; set; } = 6;

    /// <summary>
    /// Gets or sets a value indicating whether discovery first behavior is enabled. Default value: false.
    /// </summary>
    public bool DiscoveryEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value of the service ID used during discovery first behavior. Default value: "configserver".
    /// </summary>
    public string? DiscoveryServiceId { get; set; } = "configserver";

    /// <summary>
    /// Gets or sets a value indicating whether health check is enabled. Default value: true.
    /// </summary>
    public bool HealthEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value for the health check cache time-to-live. Default value: 300_000 (5 minutes).
    /// </summary>
    public long HealthTimeToLive { get; set; } = 300_000;

    /// <summary>
    /// Gets unescaped <see cref="UriComponents.HttpRequestUrl" />s.
    /// </summary>
    public IList<string> RawUris => GetRawUris();

    /// <summary>
    /// Gets or sets the token used for Vault.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the request timeout in milliseconds. Default value: 60_000 (1 minute).
    /// </summary>
    public int Timeout { get; set; } = 60_000;

    /// <summary>
    /// Gets or sets the address used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string? AccessTokenUri { get; set; }

    /// <summary>
    /// Gets or sets the client ID used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string? ClientSecret { get; set; }

    public X509Certificate2? ClientCertificate { get; set; }

    /// <summary>
    /// Gets or sets the vault token time-to-live in milliseconds. Default value: 300_000 (5 minutes).
    /// </summary>
    public int TokenTtl { get; set; } = 300_000;

    /// <summary>
    /// Gets or sets the vault token renew rate in milliseconds. Default value: 60_000 (1 minute).
    /// </summary>
    public int TokenRenewRate { get; set; } = 60_000;

    public bool DisableTokenRenewal { get; set; }

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
