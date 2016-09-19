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
using System.Collections.Generic;
using Steeltoe.Discovery.Eureka.AppInfo;
using System.Net;
using System.Net.Sockets;

namespace Steeltoe.Discovery.Client
{
    public class EurekaInstanceOptions : AbstractOptions, IDiscoveryRegistrationOptions, IEurekaInstanceConfig
    {
        public const int Default_NonSecurePort = 8080;
        public const int Default_SecurePort = 443;
        public const int Default_LeaseRenewalIntervalInSeconds = 30;
        public const int Default_LeaseExpirationDurationInSeconds = 90;
        public const string Default_Appname = "unknown";
        public const string Default_StatusPageUrlPath = "/info";
        public const string Default_HomePageUrlPath = "/";
        public const string Default_HealthCheckUrlPath = "/health";

        public EurekaInstanceOptions()
        {
            _thisHostName = GetHostName(true);
            _thisHostAddress = GetHostAddress(true);

        }

        private string _appGroupName;
        public string AppGroupName
        {
            get
            {
                if (_appGroupName != null)
                {
                    return _appGroupName;
                }
                return GetString(Eureka?.Instance?.AppGroup, null);
            }

            set
            {
                _appGroupName = value;
            }
        }

        private string _appName;
        public string AppName
        {
            get
            {
                if (_appName != null)
                {
                    return _appName;
                }
                return GetAppName();
            }

            set
            {
                _appName = value;
            }
        }

        private string _asgName;
        public string ASGName
        {
            get
            {
                if (_asgName != null)
                {
                    return _asgName;
                }
                return GetString(Eureka?.Instance?.AsgName, null);
            }

            set
            {
                _asgName = value;
            }
        }

        private IDataCenterInfo _dataCenterInfo;
        public IDataCenterInfo DataCenterInfo
        {
            get
            {
                if (_dataCenterInfo == null)
                {
                    return new DataCenterInfo(DataCenterName.MyOwn);
                }
                return _dataCenterInfo;
            }

            set
            {
                _dataCenterInfo = value;
            }
        }

        private string[] _defaultAddressResolutionOrder;
        public string[] DefaultAddressResolutionOrder
        {
            get
            {
                return _defaultAddressResolutionOrder;
            }

            set
            {
                _defaultAddressResolutionOrder = value;
            }
        }

        private string _healthCheckUrl;
        public string HealthCheckUrl
        {
            get
            {
                if (_healthCheckUrl != null)
                {
                    return _healthCheckUrl;
                }

                return GetString(Eureka?.Instance?.HealthCheckUrl, null);
            }

            set
            {
                _healthCheckUrl = value;
            }
        }

        private string _healthCheckUrlPath = Default_HealthCheckUrlPath;
        public string HealthCheckUrlPath
        {
            get
            {
                if (!_healthCheckUrlPath.Equals(Default_HealthCheckUrlPath))
                {
                    return _healthCheckUrlPath;
                }
                return GetString(Eureka?.Instance?.HealthCheckUrlPath, Default_HealthCheckUrlPath);
            }

            set
            {
                _healthCheckUrlPath = value;
            }
        }

        private string _homePageUrl;
        public string HomePageUrl
        {
            get
            {
                if (_homePageUrl != null)
                {
                    return _homePageUrl;
                }
                return GetString(Eureka?.Instance?.HomePageUrl, null);
            }

            set
            {
                _homePageUrl = value;
            }
        }

        private string _homePageUrlPath = Default_HomePageUrlPath;
        public string HomePageUrlPath
        {
            get
            {
                if (!_homePageUrlPath.Equals(Default_HomePageUrlPath))
                {
                    return _homePageUrlPath;
                }
                return GetString(Eureka?.Instance?.HomePageUrlPath, Default_HomePageUrlPath);
            }

            set
            {
                _homePageUrlPath = value;
            }
        }

        private string _instanceId;
        public string InstanceId
        {
            get
            {
                if (_instanceId != null)
                {
                    return _instanceId;
                }

                return GetInstanceId();
            }

            set
            {
                _instanceId = value;
            }
        }

        private string _ipAddress;
        public string IpAddress
        {
            get
            {
                if (_ipAddress != null)
                {
                    return _ipAddress;
                }
                return _thisHostAddress;
            }

            set
            {
                _ipAddress = value;
            }
        }

        private bool _isInstanceEnabledOnInit = true;
        public bool IsInstanceEnabledOnInit
        {
            get
            {
                if (_isInstanceEnabledOnInit != true)
                {
                    return _isInstanceEnabledOnInit;
                }
                return GetBoolean(Eureka?.Instance?.InstanceEnabledOnInit, true);
            }

            set
            {
                _isInstanceEnabledOnInit = value;
            }
        }

        private bool _isNonSecurePortEnabled = true;
        public bool IsNonSecurePortEnabled
        {
            get
            {
                if (_isNonSecurePortEnabled != true)
                {
                    return _isNonSecurePortEnabled;
                }
                return GetBoolean(Eureka?.Instance?.NonSecurePortEnabled, true);
            }

            set
            {
                _isNonSecurePortEnabled = value;
            }
        }

        private int _leaseExpirationDurationInSeconds = Default_LeaseExpirationDurationInSeconds;
        public int LeaseExpirationDurationInSeconds
        {
            get
            {
                if (_leaseExpirationDurationInSeconds != Default_LeaseExpirationDurationInSeconds)
                {
                    return _leaseExpirationDurationInSeconds;
                }
                return GetInt(Eureka?.Instance?.LeaseExpirationDurationInSeconds, Default_LeaseExpirationDurationInSeconds);
            }

            set
            {
                _leaseExpirationDurationInSeconds = value;
            }
        }

        private int _leaseRenewalIntervalInSeconds = Default_LeaseRenewalIntervalInSeconds;
        public int LeaseRenewalIntervalInSeconds
        {
            get
            {
                if (_leaseRenewalIntervalInSeconds != Default_LeaseRenewalIntervalInSeconds)
                {
                    return _leaseRenewalIntervalInSeconds;
                }
                return GetInt(Eureka?.Instance?.LeaseRenewalIntervalInSeconds, Default_LeaseRenewalIntervalInSeconds);
            }

            set
            {
                _leaseRenewalIntervalInSeconds = value;
            }
        }

        private IDictionary<string, string> _metadataMap = new Dictionary<string, string>();
        public IDictionary<string, string> MetadataMap
        {
            get
            {
                if (_metadataMap != null && _metadataMap.Count > 0)
                {
                    return _metadataMap;
                }

                return GetDictionary(Eureka?.Instance?.MetadataMap, _metadataMap);
            }

            set
            {
                _metadataMap = value;
            }
        }

        private int _nonSecurePort = Default_NonSecurePort;
        public int NonSecurePort
        {
            get
            {
                if (_nonSecurePort != Default_NonSecurePort)
                {
                    return _nonSecurePort;
                }
                return GetInt(Eureka?.Instance?.Port, Default_NonSecurePort);
            }

            set
            {
                _nonSecurePort = value;
            }
        }

        private string _secureHealthCheckUrl;
        public string SecureHealthCheckUrl
        {
            get
            {
                if (_secureHealthCheckUrl != null)
                {
                    return _secureHealthCheckUrl;
                }
                return GetString(Eureka?.Instance?.SecureHealthCheckUrl, null);
            }

            set
            {
                _secureHealthCheckUrl = value;
            }
        }

        private int _securePort = Default_SecurePort;
        public int SecurePort
        {
            get
            {
                if (_securePort != Default_SecurePort)
                {
                    return _securePort;
                }
                return GetInt(Eureka?.Instance?.SecurePort, Default_SecurePort);
            }

            set
            {
                _securePort = value;
            }
        }

        private bool _securePortEnabled;
        public bool SecurePortEnabled
        {
            get
            {
                if (_securePortEnabled != false)
                {
                    return _securePortEnabled;
                }
                return GetBoolean(Eureka?.Instance?.SecurePortEnabled, false);
            }

            set
            {
                _securePortEnabled = value;
            }
        }

        private string _secureVirtualHostName;
        public string SecureVirtualHostName
        {
            get
            {
                if (_secureVirtualHostName != null)
                {
                    return _secureVirtualHostName;
                }
                return GetString(Eureka?.Instance?.SecureVipAddress, null);
            }

            set
            {
                _secureVirtualHostName = value;
            }
        }

        private string _statusPageUrl;
        public string StatusPageUrl
        {
            get
            {
                if (_statusPageUrl != null)
                {
                    return _statusPageUrl;
                }
                return GetString(Eureka?.Instance?.StatusPageUrl, null);
            }

            set
            {
                _statusPageUrl = value;
            }
        }

        private string _statusPageUrlPath = Default_StatusPageUrlPath;
        public string StatusPageUrlPath
        {
            get
            {
                if (!_statusPageUrlPath.Equals(Default_StatusPageUrlPath))
                {
                    return _statusPageUrlPath;
                }
                return GetString(Eureka?.Instance?.StatusPageUrlPath, Default_StatusPageUrlPath);
            }

            set
            {
                _statusPageUrlPath = value;
            }
        }

        private string _virtualHostName;
        public string VirtualHostName
        {
            get
            {
                if (_virtualHostName != null)
                {
                    return _virtualHostName;
                }
                return GetString(Eureka?.Instance?.VipAddress, null);
            }

            set
            {
                _virtualHostName = value;
            }
        }
        public string HostName
        {
            get
            {
                return _thisHostName;
            }
            set
            {
                _thisHostName = value;
            }
        }

        public string GetHostName(bool refresh)
        {
            // TODO: eureka:instance:hostname
            if (refresh || string.IsNullOrEmpty(_thisHostName))
                _thisHostName = Dns.GetHostName();
            return _thisHostName;
        }

        public EurekaConfig Eureka { get; set; }

        public SpringConfig Spring { get; set; }

        protected internal string GetHostAddress(bool refresh)
        {
            if (refresh || string.IsNullOrEmpty(_thisHostAddress))
            {
                string hostName = GetHostName(refresh);
                var task = Dns.GetHostAddressesAsync(hostName);
                task.Wait();
                if (task.Result != null && task.Result.Length > 0)
                {
                    foreach (var result in task.Result)
                    {
                        if (result.AddressFamily.Equals(AddressFamily.InterNetwork))
                        {
                            _thisHostAddress = result.ToString();
                            break;
                        }
                    }
                }
            }
            return _thisHostAddress;

        }

        protected internal IDictionary<string, string> GetDictionary(Dictionary<string, string> dict, IDictionary<string, string> def)
        {
            if (dict == null)
                return def;
            return dict;
        }

        protected internal string GetInstanceId()
        {

            if (!string.IsNullOrEmpty(Eureka?.Instance?.InstanceId))
            {
                return Eureka?.Instance?.InstanceId;
            }
            if (!string.IsNullOrEmpty(Spring?.Application?.Instance_id))
            {
                return Spring?.Application?.Instance_id;
            }
            return null;
        }

        protected internal string GetAppName()
        {
            if (!string.IsNullOrEmpty(Spring?.Application?.Name))
            {
                return Spring?.Application?.Name;
            }
            if (!string.IsNullOrEmpty(Eureka?.Instance?.AppName))
            {
                return Eureka?.Instance?.AppName;
            }
            return null;
        }

        private string _thisHostAddress;
        private string _thisHostName;
    }

    public class SpringConfig
    {
        public ApplicationConfig Application { get; set; }
    }

    public class ApplicationConfig
    {
        public string Name { get; set; }
        public string Instance_id { get; set; }
    }

    public class InstanceConfig
    {
        /// <summary>
        /// Configuration property: eureka:instance:instanceId
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:appName
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:appGroup
        /// </summary>
        public string AppGroup { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:instanceEnabledOnInit
        /// </summary>
        public string InstanceEnabledOnInit { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:port
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:securePort
        /// </summary>
        public string SecurePort { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:nonSecurePortEnabled
        /// </summary>
        public string NonSecurePortEnabled { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:securePortEnabled
        /// </summary>
        public string SecurePortEnabled { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:leaseRenewalIntervalInSeconds
        /// </summary>
        public string LeaseRenewalIntervalInSeconds { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:leaseExpirationDurationInSeconds
        /// </summary>
        public string LeaseExpirationDurationInSeconds { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:vipAddress
        /// </summary>
        public string VipAddress { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:secureVipAddress
        /// </summary>
        public string SecureVipAddress { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:asgName
        /// </summary>
        public string AsgName { get; set; }


        /// <summary>
        /// Configuration property: eureka:instance:metadataMap
        /// </summary>
        public Dictionary<string, string> MetadataMap { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:statusPageUrlPath
        /// </summary>
        public string StatusPageUrlPath { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:statusPageUrl
        /// </summary>
        public string StatusPageUrl { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:homePageUrlPath
        /// </summary>
        public string HomePageUrlPath { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:homePageUrl
        /// </summary>
        public string HomePageUrl { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:healthCheckUrlPath
        /// </summary>
        public string HealthCheckUrlPath { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:healthCheckUrl
        /// </summary>
        public string HealthCheckUrl { get; set; }

        /// <summary>
        /// Configuration property: eureka:instance:secureHealthCheckUrl
        /// </summary>
        public string SecureHealthCheckUrl { get; set; }
    }

}
