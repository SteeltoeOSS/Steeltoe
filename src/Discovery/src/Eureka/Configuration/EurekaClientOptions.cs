// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Http.HttpClientPooling;

namespace Steeltoe.Discovery.Eureka.Configuration;

public sealed class EurekaClientOptions : IValidateCertificatesOptions
{
    internal const string ConfigurationPrefix = "eureka:client";
    internal const int DefaultRegistryFetchIntervalSeconds = 30;
    internal const string DefaultServerServiceUrl = "http://localhost:8761/eureka/";

    internal TimeSpan RegistryFetchInterval => TimeSpan.FromSeconds(RegistryFetchIntervalSeconds);

    /// <summary>
    /// Gets or sets the URI to use to obtain an OAuth2 access token. Configuration property: eureka:client:accessTokenUri.
    /// </summary>
    public string? AccessTokenUri { get; set; }

    /// <summary>
    /// Gets or sets the secret to use to obtain an OAuth2 access token. Configuration property: eureka:client:clientSecret.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the client ID to use to obtain an OAuth2 access token. Configuration property: eureka:client:clientId.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets how often (in seconds) to fetch the registry information from the Eureka server. Configuration property:
    /// eureka:client:registryFetchIntervalSeconds.
    /// </summary>
    public int RegistryFetchIntervalSeconds { get; set; } = DefaultRegistryFetchIntervalSeconds;

    /// <summary>
    /// Gets or sets a value indicating whether this instance should register its information with Eureka server for discovery by others. In some cases, you
    /// do not want your instances to be discovered whereas you just want to discover other instances. Configuration property:
    /// eureka:client:shouldRegisterWithEureka.
    /// </summary>
    public bool ShouldRegisterWithEureka { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the Eureka client should disable fetching of deltas and should rather resort to getting the full registry
    /// information. Note that the delta fetches can reduce the traffic tremendously, because the rate of change with the Eureka server is normally much
    /// lower than the rate of fetches. The changes are effective at runtime at the next registry fetch cycle as specified by
    /// <see cref="RegistryFetchIntervalSeconds" />. Configuration property: eureka:client:shouldDisableDelta.
    /// </summary>
    [ConfigurationKeyName("ShouldDisableDelta")]
    public bool IsFetchDeltaDisabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include only instances with UP status after fetching the list of applications. The changes are effective
    /// at runtime at the next registry fetch cycle as specified by <see cref="RegistryFetchIntervalSeconds" />. Configuration property:
    /// eureka:client:shouldFilterOnlyUpInstances.
    /// </summary>
    public bool ShouldFilterOnlyUpInstances { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this client should fetch Eureka registry information from Eureka server. Configuration property:
    /// eureka:client:shouldFetchRegistry.
    /// </summary>
    public bool ShouldFetchRegistry { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the client is only interested in the registry information for a single VIP. Configuration property:
    /// eureka:client:registryRefreshSingleVipAddress.
    /// </summary>
    public string? RegistryRefreshSingleVipAddress { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Eureka service discovery is enabled. Configuration property: eureka:client:enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a comma-delimited list of URls to use in contacting the Eureka Server. Configuration property: eureka:client:serviceUrl.
    /// </summary>
    [ConfigurationKeyName("ServiceUrl")]
    public string? EurekaServerServiceUrls { get; set; } = DefaultServerServiceUrl;

    /// <summary>
    /// Gets or sets a value indicating whether the client validates server certificates. Configuration property: eureka:client:validate_certificates.
    /// </summary>
    [ConfigurationKeyName("Validate_Certificates")]
    public bool ValidateCertificates { get; set; } = true;

    /// <summary>
    /// Gets Eureka server settings. Configuration property: eureka:client:eurekaServer.
    /// </summary>
    public EurekaServerOptions EurekaServer { get; } = new();

    /// <summary>
    /// Gets Eureka health settings. Configuration property: eureka:client:health.
    /// </summary>
    public EurekaHealthOptions Health { get; } = new();
}
