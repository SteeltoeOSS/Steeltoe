// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Discovery.Eureka;

public sealed class EurekaClientOptions
{
    internal const string EurekaClientConfigurationPrefix = "eureka:client";
    internal const int DefaultRegistryFetchIntervalSeconds = 30;
    internal const string DefaultServerServiceUrl = "http://localhost:8761/eureka/";

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
    /// Gets or sets the time in seconds that service instance cache records should remain active. Configuration property: eureka:client:cacheTTL.
    /// </summary>
    public int CacheTtl { get; set; } = 15;

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
    public bool ShouldDisableDelta { get; set; }

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
    /// Gets or sets a value indicating whether local status updates via <see cref="EurekaApplicationInfoManager.InstanceStatus" />  will trigger on-demand (but
    /// rate limited) register/updates to remote Eureka servers. Configuration property: eureka:client:shouldOnDemandUpdateStatusChange.
    /// </summary>
    public bool ShouldOnDemandUpdateStatusChange { get; set; } = true;

    // Configuration property: eureka:client:enabled
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

    // Configuration property: eureka:client:eurekaServer
    public EurekaServerConfiguration EurekaServer { get; } = new();

    // Configuration property: eureka:client:health
    public EurekaHealthConfiguration Health { get; } = new();
}
