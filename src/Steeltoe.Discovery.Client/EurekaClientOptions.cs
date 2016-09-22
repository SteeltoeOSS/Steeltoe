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

using Steeltoe.Discovery.Eureka;


namespace Steeltoe.Discovery.Client
{

    public class EurekaClientOptions : AbstractOptions, IDiscoveryClientOptions, IEurekaClientConfig
    {
        public const int Default_RegistryFetchIntervalSeconds = 30;
        public const int Default_InstanceInfoReplicationIntervalSeconds = 30;
        public const int Default_EurekaServerConnectTimeoutSeconds = 5;
        public const string Default_ServerServiceUrl = "http://localhost:8761/eureka/";

        public EurekaClientOptions()
        {
        }

        private bool _enabled = true;
        public bool Enabled
        {
            get
            {
                if (_enabled != true)
                {
                    return _enabled;
                }

                return GetBoolean(Eureka?.Client?.Enabled, true);
            }

            set
            {
                _enabled = value;
            }
        }

        private bool _allowRedirects = false;
        public bool AllowRedirects
        {
            get
            {
                if (_allowRedirects != false)
                {
                    return _allowRedirects;
                }
                return GetBoolean(Eureka?.Client?.AllowRedirects, false);
            }

            set
            {
                _allowRedirects = value;
            }
        }

        private int _eurekaServerConnectTimeoutSeconds = Default_EurekaServerConnectTimeoutSeconds;
        public int EurekaServerConnectTimeoutSeconds
        {
            get
            {
                if (_eurekaServerConnectTimeoutSeconds != Default_EurekaServerConnectTimeoutSeconds)
                {
                    return _eurekaServerConnectTimeoutSeconds;
                }
                return GetInt(Eureka?.Client?.EurekaServer?.ConnectTimeoutSeconds, Default_EurekaServerConnectTimeoutSeconds);
            }

            set
            {
                _eurekaServerConnectTimeoutSeconds = value;
            }
        }

        private string _eurekaServerServiceUrls = Default_ServerServiceUrl;
        public string EurekaServerServiceUrls
        {
            get
            {
                if (!_eurekaServerServiceUrls.Equals(Default_ServerServiceUrl))
                {
                    return _eurekaServerServiceUrls;
                }
                return GetString(Eureka?.Client?.ServiceUrl, Default_ServerServiceUrl);
            }

            set
            {
                _eurekaServerServiceUrls = value;
            }
        }

        private int _instanceInfoReplicationIntervalSeconds = Default_InstanceInfoReplicationIntervalSeconds;
        public int InstanceInfoReplicationIntervalSeconds
        {
            get
            {
                if (_instanceInfoReplicationIntervalSeconds != Default_InstanceInfoReplicationIntervalSeconds)
                {
                    return _instanceInfoReplicationIntervalSeconds;
                }
                return GetInt(Eureka?.Client?.InstanceInfoReplicationIntervalSeconds, Default_InstanceInfoReplicationIntervalSeconds);
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
                if (_proxyHost != null)
                {
                    return _proxyHost;
                }
                return GetString(Eureka?.Client?.EurekaServer?.ProxyHost, null);
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
                if (_proxyPassword != null)
                {
                    return _proxyPassword;
                }
                return GetString(Eureka?.Client?.EurekaServer?.ProxyPassword, null);
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
                if (_proxyPort != 0)
                {
                    return _proxyPort;
                }
                return GetInt(Eureka?.Client?.EurekaServer?.ProxyPort, 0);
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
                if (_proxyUserName != null)
                {
                    return _proxyUserName;
                }
                return GetString(Eureka?.Client?.EurekaServer?.ProxyUserName, null);
            }

            set
            {
                _proxyUserName = value;
            }
        }

        private int _registryFetchIntervalSeconds = Default_RegistryFetchIntervalSeconds;
        public int RegistryFetchIntervalSeconds
        {
            get
            {
                if (_registryFetchIntervalSeconds != Default_RegistryFetchIntervalSeconds)
                {
                    return Default_RegistryFetchIntervalSeconds;
                }
                return GetInt(Eureka?.Client?.RegistryFetchIntervalSeconds, Default_RegistryFetchIntervalSeconds);
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
                if (_registryRefreshSingleVipAddress != null)
                {
                    return _registryRefreshSingleVipAddress;
                }
                return GetString(Eureka?.Client?.RegistryRefreshSingleVipAddress, null);
            }

            set
            {
                _registryRefreshSingleVipAddress = value;
            }
        }

        private bool _shouldDisableDelta = false;
        public bool ShouldDisableDelta
        {
            get
            {
                if (_shouldDisableDelta != false)
                {
                    return _shouldDisableDelta;
                }
                return GetBoolean(Eureka?.Client?.ShouldDisableDelta, false);
            }

            set
            {
                _shouldDisableDelta = value;
            }
        }

        private bool _shouldFetchRegistry = true;
        public bool ShouldFetchRegistry
        {
            get
            {
                if (_shouldFetchRegistry != true)
                {
                    return _shouldFetchRegistry;
                }
                return GetBoolean(Eureka?.Client?.ShouldFetchRegistry, true);
            }

            set
            {
                _shouldFetchRegistry = value;
            }
        }

        private bool _shouldFilterOnlyUpInstances = true;
        public bool ShouldFilterOnlyUpInstances
        {
            get
            {
                if (_shouldFilterOnlyUpInstances != true)
                {
                    return _shouldFilterOnlyUpInstances;
                }
                return GetBoolean(Eureka?.Client?.ShouldFilterOnlyUpInstances, true);
            }

            set
            {
                _shouldFilterOnlyUpInstances = value;
            }
        }

        private bool _shouldGZipContent = true;
        public bool ShouldGZipContent
        {
            get
            {
                if (_shouldGZipContent != true)
                {
                    return _shouldGZipContent;
                }
                return GetBoolean(Eureka?.Client.EurekaServer?.ShouldGZipContent, true);
            }

            set
            {
                _shouldGZipContent = value;
            }
        }

        private bool _shouldOnDemandUpdateStatusChange = true;
        public bool ShouldOnDemandUpdateStatusChange
        {
            get
            {
                if (_shouldOnDemandUpdateStatusChange != true)
                {
                    return _shouldOnDemandUpdateStatusChange;
                }
                return GetBoolean(Eureka?.Client.ShouldOnDemandUpdateStatusChange, true);
            }

            set
            {
                _shouldOnDemandUpdateStatusChange = value;
            }
        }

        private bool _shouldRegisterWithEureka = true;
        public bool ShouldRegisterWithEureka
        {
            get
            {
                if (_shouldRegisterWithEureka != true)
                {
                    return _shouldRegisterWithEureka;
                }
                return GetBoolean(Eureka?.Client?.ShouldRegisterWithEureka, true);
            }

            set
            {
                _shouldRegisterWithEureka = value;
            }
        }

        private bool _validateCertificates = true;
        public bool ValidateCertificates
        {
            get
            {
                if (_validateCertificates != true)
                {
                    return _validateCertificates;
                }
                return GetBoolean(Eureka?.Client?.Validate_Certificates, true);
            }

            set
            {
                _validateCertificates = value;
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
