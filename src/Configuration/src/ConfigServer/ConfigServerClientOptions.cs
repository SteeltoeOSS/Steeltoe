// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.Http.HttpClientPooling;

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// Holds settings used to configure the Spring Cloud Config Server provider.
/// </summary>
public sealed class ConfigServerClientOptions : IValidateCertificatesOptions
{
    private const char CommaDelimiter = ',';
    internal const string ConfigurationPrefix = "spring:cloud:config";

    internal CertificateOptions ClientCertificate { get; } = new();
    internal TimeSpan HttpTimeout => TimeSpan.FromMilliseconds(Timeout);
    internal bool IsMultiServerConfiguration => Uri != null && Uri.Contains(CommaDelimiter);

    /// <summary>
    /// Gets or sets a value indicating whether the Config Server provider is enabled. Default value: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether fail-fast behavior is enabled. Default value: false.
    /// </summary>
    public bool FailFast { get; set; }

    /// <summary>
    /// Gets or sets the environment used when accessing configuration data. Default value: "Production".
    /// </summary>
    [ConfigurationKeyName("Env")]
    public string? Environment { get; set; } = "Production";

    /// <summary>
    /// Gets or sets a comma-delimited list of labels to request from the server.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the application name used when accessing configuration data.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a comma-delimited list of Config Server addresses. Default value: "http://localhost:8888".
    /// </summary>
    public string? Uri { get; set; } = "http://localhost:8888";

    /// <summary>
    /// Gets or sets the username used when accessing the Config Server.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password used when accessing the Config Server.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the HashiCorp Vault authentication token.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the request timeout (in milliseconds). Default value: 60_000 (1 minute).
    /// </summary>
    public int Timeout { get; set; } = 60_000;

    /// <summary>
    /// Gets or sets the frequency with which app should check Config Server for changes in configuration.
    /// </summary>
    public TimeSpan PollingInterval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider validates server certificates. Default value: true.
    /// </summary>
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the provider validates server certificates. Default value: true.
    /// </summary>
    [ConfigurationKeyName("Validate_Certificates")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool ValidateCertificatesAlt
    {
        get => ValidateCertificates;
        set => ValidateCertificates = value;
    }

    /// <summary>
    /// Gets retry settings.
    /// </summary>
    public ConfigServerRetryOptions Retry { get; } = new();

    /// <summary>
    /// Gets service discovery settings.
    /// </summary>
    public ConfigServerDiscoveryOptions Discovery { get; } = new();

    /// <summary>
    /// Gets health check settings.
    /// </summary>
    public ConfigServerHealthOptions Health { get; } = new();

    /// <summary>
    /// Gets or sets the address used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string? AccessTokenUri { get; set; }

    /// <summary>
    /// Gets or sets the client secret used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the client ID used by the provider to obtain a OAuth Access Token.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the HashiCorp Vault token time-to-live (in milliseconds). Default value: 300_000 (5 minutes).
    /// </summary>
    public int TokenTtl { get; set; } = 300_000;

    /// <summary>
    /// Gets or sets the vault token renew rate (in milliseconds). Default value: 60_000 (1 minute).
    /// </summary>
    public int TokenRenewRate { get; set; } = 60_000;

    /// <summary>
    /// Gets or sets a value indicating whether periodic HashiCorp Vault token renewal should occur. Default value: false.
    /// </summary>
    public bool DisableTokenRenewal { get; set; }

    /// <summary>
    /// Gets headers that will be added to the Config Server request.
    /// </summary>
    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

    internal IList<string> GetUris()
    {
        return !string.IsNullOrEmpty(Uri) ? Uri.Split(CommaDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : [];
    }
}
