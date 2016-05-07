//
// Copyright 2015 the original author or authors.
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
//

using SteelToe.Discovery.Eureka;


namespace SteelToe.Discovery.Client
{

    public class EurekaClientOptions : AbstractOptions, IDiscoveryClientOptions, IEurekaClientConfig
    {
        public const int Default_RegistryFetchIntervalSeconds = 30;
        public const int Default_InstanceInfoReplicationIntervalSeconds = 30;
        public const int Default_EurekaServerConnectTimeoutSeconds = 5;
        public const string Default_ServerServiceUrl = "http://localhost:8761/eureka/";

        public EurekaClientOptions()
        {
            Enabled = true;
            RegistryFetchIntervalSeconds = Default_RegistryFetchIntervalSeconds;
            InstanceInfoReplicationIntervalSeconds = Default_InstanceInfoReplicationIntervalSeconds;
            ShouldGZipContent = true;
            EurekaServerConnectTimeoutSeconds = Default_EurekaServerConnectTimeoutSeconds;
            ShouldRegisterWithEureka = true;
            AllowRedirects = false;
            ShouldDisableDelta = false;
            ShouldFilterOnlyUpInstances = true;
            ShouldFetchRegistry = true;
            ShouldOnDemandUpdateStatusChange = true;
            EurekaServerServiceUrls = Default_ServerServiceUrl;
            ValidateCertificates = true;
        }

        private bool _enabled;
        public bool Enabled
        {
            get
            {
                return GetBoolean(Eureka?.Client?.Enabled, _enabled);
            }

            set
            {
                _enabled = value;
            }
        }

        private bool _allowRedirects;
        public bool AllowRedirects
        {
            get
            {
                return GetBoolean(Eureka?.Client?.AllowRedirects, _allowRedirects);
            }

            set
            {
                _allowRedirects = value;
            }
        }

        private int _eurekaServerConnectTimeoutSeconds;
        public int EurekaServerConnectTimeoutSeconds
        {
            get
            {
                return GetInt(Eureka?.Client?.EurekaServer?.ConnectTimeoutSeconds, _eurekaServerConnectTimeoutSeconds);
            }

            set
            {
                _eurekaServerConnectTimeoutSeconds = value;
            }
        }

        private string _eurekaServerServiceUrls;
        public string EurekaServerServiceUrls
        {
            get
            {
                return GetString(Eureka?.Client?.ServiceUrl, _eurekaServerServiceUrls);
            }

            set
            {
                _eurekaServerServiceUrls = value;
            }
        }

        private int _instanceInfoReplicationIntervalSeconds;
        public int InstanceInfoReplicationIntervalSeconds
        {
            get
            {
                return GetInt(Eureka?.Client?.InstanceInfoReplicationIntervalSeconds, _instanceInfoReplicationIntervalSeconds);
            }

            set
            {
                _instanceInfoReplicationIntervalSeconds = value;
            }
        }

        private string _proxyHost;
        public string ProxyHost
        {
            get
            {
                return GetString(Eureka?.Client?.EurekaServer?.ProxyHost, _proxyHost);
            }

            set
            {
                _proxyHost = value;
            }
        }

        private string _proxyPassword;
        public string ProxyPassword
        {
            get
            {
                return GetString(Eureka?.Client?.EurekaServer?.ProxyPassword, _proxyPassword);
            }

            set
            {
                _proxyPassword = value;
            }
        }

        private int _proxyPort;
        public int ProxyPort
        {
            get
            {
                return GetInt(Eureka?.Client?.EurekaServer?.ProxyPort, _proxyPort);
            }

            set
            {
                _proxyPort = value;
            }
        }

        private string _proxyUserName;
        public string ProxyUserName
        {
            get
            {
                return GetString(Eureka?.Client?.EurekaServer?.ProxyUserName, _proxyUserName);
            }

            set
            {
                _proxyUserName = value;
            }
        }

        private int _registryFetchIntervalSeconds;
        public int RegistryFetchIntervalSeconds
        {
            get
            {
                return GetInt(Eureka?.Client?.RegistryFetchIntervalSeconds, _registryFetchIntervalSeconds);
            }

            set
            {
                _registryFetchIntervalSeconds = value;
            }
        }

        private string _registryRefreshSingleVipAddress;
        public string RegistryRefreshSingleVipAddress
        {
            get
            {
                return GetString(Eureka?.Client?.RegistryRefreshSingleVipAddress, _registryRefreshSingleVipAddress);
            }

            set
            {
                _registryRefreshSingleVipAddress = value;
            }
        }

        private bool _shouldDisableDelta;
        public bool ShouldDisableDelta
        {
            get
            {
                return GetBoolean(Eureka?.Client?.ShouldDisableDelta, _shouldDisableDelta);
            }

            set
            {
                _shouldDisableDelta = value;
            }
        }

        private bool _shouldFetchRegistry;
        public bool ShouldFetchRegistry
        {
            get
            {
                return GetBoolean(Eureka?.Client?.ShouldFetchRegistry, _shouldFetchRegistry);
            }

            set
            {
                _shouldFetchRegistry = value;
            }
        }

        private bool _shouldFilterOnlyUpInstances;
        public bool ShouldFilterOnlyUpInstances
        {
            get
            {
                return GetBoolean(Eureka?.Client?.ShouldFilterOnlyUpInstances, _shouldFilterOnlyUpInstances);
            }

            set
            {
                _shouldFilterOnlyUpInstances = value;
            }
        }

        private bool _shouldGZipContent;
        public bool ShouldGZipContent
        {
            get
            {
                return GetBoolean(Eureka?.Client.EurekaServer?.ShouldGZipContent, _shouldGZipContent);
            }

            set
            {
                _shouldGZipContent = value;
            }
        }

        private bool _shouldOnDemandUpdateStatusChange;
        public bool ShouldOnDemandUpdateStatusChange
        {
            get
            {
                return GetBoolean(Eureka?.Client.ShouldOnDemandUpdateStatusChange, _shouldOnDemandUpdateStatusChange);
            }

            set
            {
                _shouldOnDemandUpdateStatusChange = value;
            }
        }

        private bool _shouldRegisterWithEureka;
        public bool ShouldRegisterWithEureka
        {
            get
            {
                return GetBoolean(Eureka?.Client?.ShouldRegisterWithEureka, _shouldRegisterWithEureka);
            }

            set
            {
                _shouldRegisterWithEureka = value;
            }
        }

        private bool __validateCertificates;
        public bool ValidateCertificates
        {
            get
            {
                return GetBoolean(Eureka?.Client?.Validate_Certificates, __validateCertificates);
            }

            set
            {
                __validateCertificates = value;
            }
        }

        public EurekaConfig Eureka { get; set; }

    }

    public class EurekaConfig
    {
        public ClientConfig Client { get; set; }
        public InstanceConfig Instance { get; set; }
    }

    public class EurekaServerConfig
    {

        /// <summary>
        /// Configuration property: eureka:client:eurekaServer:proxyHost
        /// </summary>
        public string ProxyHost { get; set; }
        /// <summary>
        /// Configuration property: eureka:client:eurekaServer:proxyPort
        /// </summary>
        public string ProxyPort { get; set; }
        /// <summary>
        ///  Configuration property: eureka:client:eurekaServer:proxyUserName
        /// </summary>
        public string ProxyUserName { get; set; }
        /// <summary>
        ///  Configuration property: eureka:client:eurekaServer:proxyPassword
        /// </summary>
        public string ProxyPassword { get; set; }
        /// <summary>
        ///  Configuration property: eureka:client:eurekaServer:shouldGZipContent
        /// </summary>
        public string ShouldGZipContent { get; set; }
        /// <summary>
        ///  Configuration property: eureka:client:eurekaServer:connectTimeoutSeconds
        /// </summary>
        public string ConnectTimeoutSeconds { get; set; }

    }

    public class ClientConfig
    {
        /// <summary>
        ///  Configuration property: eureka:client:enabled
        /// </summary>
        public string Enabled { get; set; }

        /// <summary>
        ///  Configuration property: eureka:client:allowRedirects
        /// </summary>
        public string AllowRedirects { get; set; }

        /// <summary>
        ///  Configuration property: eureka:client:shouldDisableDelta
        /// </summary>
        public string ShouldDisableDelta { get; set; }


        /// <summary>
        ///  Configuration property: eureka:client:shouldFilterOnlyUpInstances
        /// </summary>
        public string ShouldFilterOnlyUpInstances { get; set; }

        /// <summary>
        ///  Configuration property: eureka:client:shouldFetchRegistry
        /// </summary>
        public string ShouldFetchRegistry { get; set; }

        /// <summary>
        ///  Configuration property: eureka:client:shouldRegisterWithEureka
        /// </summary>
        public string ShouldRegisterWithEureka { get; set; }

        /// <summary>
        ///  Configuration property: eureka:client:registryRefreshSingleVipAddress
        /// </summary>
        public string RegistryRefreshSingleVipAddress { get; set; }

        /// <summary>
        ///  Configuration property: eureka:client:shouldOnDemandUpdateStatusChange
        /// </summary>
        public string ShouldOnDemandUpdateStatusChange { get; set; }

        /// <summary>
        /// Configuration property: eureka:client:registryFetchIntervalSeconds
        /// </summary>
        public string RegistryFetchIntervalSeconds { get; set; }

        /// <summary>
        /// Configuration property: eureka:client:instanceInfoReplicationIntervalSeconds
        /// </summary>
        public string InstanceInfoReplicationIntervalSeconds { get; set; }

        /// <summary>
        ///  Configuration property: eureka:client:serviceUrl
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Configuration property: eureka:client:validate_certificates
        /// </summary>
        public string Validate_Certificates { get; set; }

        public EurekaServerConfig EurekaServer { get; set; }
    }
}
