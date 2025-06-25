// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Http.HttpClientPooling;

namespace Steeltoe.Discovery.Eureka.Configuration;

public sealed class EurekaClientOptions : IValidateCertificatesOptions
{
    internal const string ConfigurationPrefix = "eureka:client";

    internal TimeSpan RegistryFetchInterval => TimeSpan.FromSeconds(RegistryFetchIntervalSeconds);

    /// <summary>
    /// Gets or sets the URL to obtain OAuth2 access token from, before connecting to the Eureka server.
    /// </summary>
    public string? AccessTokenUri { get; set; }

    /// <summary>
    /// Gets or sets the secret for obtaining access token.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the client ID for obtaining access token.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets how often (in seconds) to fetch registry information from the Eureka server. Default value: 30.
    /// </summary>
    public int RegistryFetchIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to register the running app as a service instance. Default value: true.
    /// </summary>
    public bool ShouldRegisterWithEureka { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to fetch the full registry each time or fetch only deltas. Note that the delta fetches can reduce the traffic
    /// tremendously, because the rate of change in the Eureka server is normally much lower than the rate of fetches. Default value: false.
    /// </summary>
    [ConfigurationKeyName("ShouldDisableDelta")]
    public bool IsFetchDeltaDisabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include only instances with UP status after fetching the list of applications. Default value: true.
    /// </summary>
    public bool ShouldFilterOnlyUpInstances { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to periodically fetch registry information from the Eureka server. Default value: true.
    /// </summary>
    public bool ShouldFetchRegistry { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to only fetch registry information for the specified VIP address. Default value: false.
    /// </summary>
    public string? RegistryRefreshSingleVipAddress { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable the Eureka client. Default value: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a comma-separated list of Eureka server endpoints. Default value: http://localhost:8761/eureka/.
    /// </summary>
    [ConfigurationKeyName("ServiceUrl")]
    public string? EurekaServerServiceUrls { get; set; } = "http://localhost:8761/eureka/";

    /// <summary>
    /// Gets or sets a value indicating whether the client validates server certificates. Default value: true.
    /// </summary>
    [ConfigurationKeyName("Validate_Certificates")]
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Gets Eureka server settings.
    /// </summary>
    public EurekaServerOptions EurekaServer { get; } = new();

    /// <summary>
    /// Gets Eureka health settings.
    /// </summary>
    public EurekaHealthOptions Health { get; } = new();
}
