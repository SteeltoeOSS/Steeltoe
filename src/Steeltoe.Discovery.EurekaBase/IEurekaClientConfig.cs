// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Steeltoe.Discovery.Eureka
{
    public interface IEurekaClientConfig
    {
        /// <summary>
        /// Gets or sets indicates how often(in seconds) to fetch the registry information from the eureka server.
        /// Configuration property: eureka:client:registryFetchIntervalSeconds
        /// </summary>
        int RegistryFetchIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets indicates how often(in seconds) to replicate instance changes to be replicated to the eureka server.
        /// Configuration property: eureka:client:instanceInfoReplicationIntervalSeconds
        /// </summary>
        int InstanceInfoReplicationIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the proxy host to the eureka server if any.
        /// Configuration property: eureka:client:eurekaServer:proxyHost
        /// </summary>
        string ProxyHost { get; set; }

        /// <summary>
        /// Gets or sets the proxy port to the eureka server if any.
        /// Configuration property: eureka:client:eurekaServer:proxyPort
        /// <paramref name="value"/>sets the proxy port value
        /// </summary>
        int ProxyPort { get; set; }

        /// <summary>
        ///  Gets or sets the proxy user name if any.
        ///  Configuration property: eureka:client:eurekaServer:proxyUserName
        /// </summary>
        string ProxyUserName { get; set; }

        /// <summary>
        /// Gets or sets the proxy password if any.
        ///  Configuration property: eureka:client:eurekaServer:proxyPassword
        /// </summary>
        string ProxyPassword { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates whether the content fetched from eureka server has to be compressed whenever it is
        /// supported by the server.The registry information from the eureka server is compressed
        /// for optimum network traffic.
        ///  Configuration property: eureka:client:eurekaServer:shouldGZipContent
        /// </summary>
        bool ShouldGZipContent { get; set; }

        /// <summary>
        /// Gets or sets indicates how long to wait (in seconds) before a connection to eureka
        /// server needs to timeout.
        /// Note that the connections in the client are pooled by and this setting affects the actual
        /// connection creation and also the wait time to get the connection from the pool.
        ///  Configuration property: eureka:client:eurekaServer:connectTimeoutSeconds
        /// </summary>
        int EurekaServerConnectTimeoutSeconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates whether or not this instance should register its information with eureka server for discovery by others.
        ///
        /// In some cases, you do not want your instances to be discovered whereas you just want do discover other instances.
        ///
        ///  Configuration property: eureka:client:shouldRegisterWithEureka
        /// </summary>
        bool ShouldRegisterWithEureka { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates whether server can redirect a client request to a backup server/cluster.If set to false, the server
        /// will handle the request directly, If set to true, it may send HTTP redirect to the client, with a new server location.
        ///  Configuration property: eureka:client:allowRedirects
        /// </summary>
       [Obsolete("Eureka client does not support this feature, will be removed in next release")]
        bool AllowRedirects { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates whether the eureka client should disable fetching of delta and should rather resort to getting the full
        /// registry information.
        ///
        /// Note that the delta fetches can reduce the traffic tremendously, because the rate of change with the eureka server is
        /// normally much lower than therate of fetches.
        ///
        /// The changes are effective at runtime at the next registry fetch cycle as specified by <see cref="RegistryFetchIntervalSeconds"/>
        ///
        ///  Configuration property: eureka:client:shouldDisableDelta
        /// </summary>
        bool ShouldDisableDelta { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates whether to get the <em>applications</em> after filtering the applications for instances with only UP states.
        /// The changes are effective at runtime at the next registry fetch cycle as specified by <see cref="RegistryFetchIntervalSeconds"/>
        ///
        ///  Configuration property: eureka:client:shouldFilterOnlyUpInstances
        /// </summary>
        bool ShouldFilterOnlyUpInstances { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates whether this client should fetch eureka registry information from eureka server.
        ///  Configuration property: eureka:client:shouldFetchRegistry
        /// </summary>
        bool ShouldFetchRegistry { get; set; }

        /// <summary>
        /// Gets or sets indicates whether the client is only interested in the registry information for a single VIP.
        ///  Configuration property: eureka:client:registryRefreshSingleVipAddress
        /// </summary>
        string RegistryRefreshSingleVipAddress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether if set to true, local status updates via ApplicationInfoManager#setInstanceStatus(InstanceStatus)}
        /// will trigger on-demand (but rate limited) register/updates to remote eureka servers
        ///  Configuration property: eureka:client:shouldOnDemandUpdateStatusChange
        /// </summary>
        bool ShouldOnDemandUpdateStatusChange { get; set; }

        /// <summary>
        /// Gets or sets comma delimited list of URls to use in contacting the Eureka Server
        ///  Configuration property: eureka:client:serviceUrl
        /// </summary>
        string EurekaServerServiceUrls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether enables/Disables whether client validates server certificates
        /// Configuration property: eureka:client:validate_certificates
        /// </summary>
        bool ValidateCertificates { get; set; }

        // TODO: Add this breaking change during 3.0
        // int EurekaServerRetryCount { get; set; }

        // TODO: Add this breaking change during 3.0
        // bool HealthEnabled { get; set; }

        // TODO: Add this breaking change during 3.0
        // bool HealthCheck { get; set; }

        // TODO: Add this breaking change during 3.0
        // string HealthMonitoredApps { get; set; }
    }
}
