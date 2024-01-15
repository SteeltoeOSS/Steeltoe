// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Eureka;

public class EurekaClientConfiguration
{
    public const int DefaultRegistryFetchIntervalSeconds = 30;
    public const int DefaultInstanceInfoReplicationIntervalSeconds = 40;
    public const int DefaultEurekaServerConnectTimeoutSeconds = 5;
    public const int DefaultEurekaServerRetryCount = 3;
    public const string DefaultServerServiceUrl = "http://localhost:8761/eureka/";

    /// <summary>
    /// Gets or sets indicates how often(in seconds) to fetch the registry information from the eureka server. Configuration property:
    /// eureka:client:registryFetchIntervalSeconds.
    /// </summary>
    public int RegistryFetchIntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether this instance should register its information with eureka server for discovery by
    /// others. In some cases, you do not want your instances to be discovered whereas you just want to discover other instances. Configuration property:
    /// eureka:client:shouldRegisterWithEureka.
    /// </summary>
    public bool ShouldRegisterWithEureka { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the eureka client should disable fetching of delta and should rather resort to getting the
    /// full registry information. Note that the delta fetches can reduce the traffic tremendously, because the rate of change with the eureka server is
    /// normally much lower than the rate of fetches. The changes are effective at runtime at the next registry fetch cycle as specified by
    /// <see cref="RegistryFetchIntervalSeconds" /> Configuration property: eureka:client:shouldDisableDelta.
    /// </summary>
    public bool ShouldDisableDelta { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether to get the
    /// <em>
    /// applications
    /// </em>
    /// after filtering the applications for instances with only UP states. The changes are effective at runtime at the next registry fetch cycle as
    /// specified by <see cref="RegistryFetchIntervalSeconds" /> Configuration property: eureka:client:shouldFilterOnlyUpInstances.
    /// </summary>
    public bool ShouldFilterOnlyUpInstances { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether this client should fetch eureka registry information from eureka server. Configuration
    /// property: eureka:client:shouldFetchRegistry.
    /// </summary>
    public bool ShouldFetchRegistry { get; set; }

    /// <summary>
    /// Gets or sets indicates whether the client is only interested in the registry information for a single VIP. Configuration property:
    /// eureka:client:registryRefreshSingleVipAddress.
    /// </summary>
    public string RegistryRefreshSingleVipAddress { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether if set to true, local status updates via ApplicationInfoManager#setInstanceStatus(InstanceStatus)} will
    /// trigger on-demand (but rate limited) register/updates to remote eureka servers Configuration property:
    /// eureka:client:shouldOnDemandUpdateStatusChange.
    /// </summary>
    public bool ShouldOnDemandUpdateStatusChange { get; set; }

    // Configuration property: eureka:client:enabled
    public bool Enabled { get; set; } = true;

    // Configuration property: eureka:client:healthCheckEnabled
    public bool HealthCheckEnabled { get; set; }

    /// <summary>
    /// Gets or sets comma delimited list of URls to use in contacting the Eureka Server Configuration property: eureka:client:serviceUrl.
    /// </summary>
    public string EurekaServerServiceUrls { get; set; }

    /// <summary>
    /// Gets or sets indicates how long to wait (in seconds) before a connection to eureka server needs to timeout. Note that the connections in the client
    /// are pooled by and this setting affects the actual connection creation and also the wait time to get the connection from the pool. Configuration
    /// property: eureka:client:eurekaServer:connectTimeoutSeconds.
    /// </summary>
    public int EurekaServerConnectTimeoutSeconds { get; set; }

    public int EurekaServerRetryCount { get; set; }

    /// <summary>
    /// Gets or sets the proxy host to the eureka server if any. Configuration property: eureka:client:eurekaServer:proxyHost.
    /// </summary>
    public string ProxyHost { get; set; }

    /// <summary>
    /// Gets or sets the proxy port to the eureka server if any. Configuration property: eureka:client:eurekaServer:proxyPort <paramref name="value" />sets
    /// the proxy port value.
    /// </summary>
    public int ProxyPort { get; set; }

    /// <summary>
    /// Gets or sets the proxy username if any. Configuration property: eureka:client:eurekaServer:proxyUserName.
    /// </summary>
    public string ProxyUserName { get; set; }

    /// <summary>
    /// Gets or sets the proxy password if any. Configuration property: eureka:client:eurekaServer:proxyPassword.
    /// </summary>
    public string ProxyPassword { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the content fetched from eureka server has to be compressed whenever it is supported by the
    /// server.The registry information from the eureka server is compressed for optimum network traffic. Configuration property:
    /// eureka:client:eurekaServer:shouldGZipContent.
    /// </summary>
    public bool ShouldGZipContent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enables/Disables whether client validates server certificates Configuration property:
    /// eureka:client:validate_certificates.
    /// </summary>
    public bool ValidateCertificates { get; set; }

    public bool HealthContribEnabled { get; set; }

    public string HealthMonitoredApps { get; set; }

    public EurekaClientConfiguration()
    {
        RegistryFetchIntervalSeconds = DefaultRegistryFetchIntervalSeconds;
        ShouldGZipContent = true;
        EurekaServerConnectTimeoutSeconds = DefaultEurekaServerConnectTimeoutSeconds;
        ShouldRegisterWithEureka = true;
        ShouldDisableDelta = false;
        ShouldFilterOnlyUpInstances = true;
        ShouldFetchRegistry = true;
        ShouldOnDemandUpdateStatusChange = true;
        EurekaServerServiceUrls = DefaultServerServiceUrl;
        ValidateCertificates = true;
        EurekaServerRetryCount = DefaultEurekaServerRetryCount;
        HealthCheckEnabled = true;
        HealthContribEnabled = true;
    }
}
