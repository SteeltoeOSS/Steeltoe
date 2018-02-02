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

using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaInstanceConfig : IEurekaInstanceConfig
    {
        public const int Default_NonSecurePort = 80;
        public const int Default_SecurePort = 443;
        public const int Default_LeaseRenewalIntervalInSeconds = 30;
        public const int Default_LeaseExpirationDurationInSeconds = 90;
        public const string Default_Appname = "unknown";
        public const string Default_StatusPageUrlPath = "/Status";
        public const string Default_HomePageUrlPath = "/";
        public const string Default_HealthCheckUrlPath = "/healthcheck";

        protected string _thisHostAddress;
        protected string _thisHostName;

        public EurekaInstanceConfig()
        {
            _thisHostName = GetHostName(true);
            _thisHostAddress = GetHostAddress(true);

            IsInstanceEnabledOnInit = false;
            NonSecurePort = Default_NonSecurePort;
            SecurePort = Default_SecurePort;
            IsNonSecurePortEnabled = true;
            SecurePortEnabled = false;
            LeaseRenewalIntervalInSeconds = Default_LeaseRenewalIntervalInSeconds;
            LeaseExpirationDurationInSeconds = Default_LeaseExpirationDurationInSeconds;
            VirtualHostName = _thisHostName + ":" + NonSecurePort;
            SecureVirtualHostName = _thisHostName + ":" + SecurePort;
            IpAddress = _thisHostAddress;
            AppName = Default_Appname;
            StatusPageUrlPath = Default_StatusPageUrlPath;
            HomePageUrlPath = Default_HomePageUrlPath;
            HealthCheckUrlPath = Default_HealthCheckUrlPath;
            MetadataMap = new Dictionary<string, string>();
            DataCenterInfo = new DataCenterInfo(DataCenterName.MyOwn);
            PreferIpAddress = false;
        }

        // eureka:instance:instanceId, spring:application:instance_id, null
        public virtual string InstanceId { get; set; }

        // eureka:instance:appName, spring:application:name, null
        public virtual string AppName { get; set; }

        // eureka:instance:securePort
        public virtual int SecurePort { get; set; }

        // eureka:instance:securePortEnabled
        public virtual bool SecurePortEnabled { get; set; }

        // eureka:instance:leaseRenewalIntervalInSeconds
        public virtual int LeaseRenewalIntervalInSeconds { get; set; }

        // eureka:instance:leaseExpirationDurationInSeconds
        public virtual int LeaseExpirationDurationInSeconds { get; set; }

        // eureka:instance:asgName, null
        public virtual string ASGName { get; set; }

        // eureka:instance:metadataMap
        public virtual IDictionary<string, string> MetadataMap { get; set; }

        // eureka:instance:statusPageUrlPath
        public virtual string StatusPageUrlPath { get; set; }

        // eureka:instance:statusPageUrl
        public virtual string StatusPageUrl { get; set; }

        // eureka:instance:homePageUrlPath
        public virtual string HomePageUrlPath { get; set; }

        // eureka:instance:homePageUrl
        public virtual string HomePageUrl { get; set; }

        // eureka:instance:healthCheckUrlPath
        public virtual string HealthCheckUrlPath { get; set; }

        // eureka:instance:healthCheckUrl
        public virtual string HealthCheckUrl { get; set; }

        // eureka:instance:secureHealthCheckUrl
        public virtual string SecureHealthCheckUrl { get; set; }

        // eureka:instance:preferIpAddress
        public virtual bool PreferIpAddress { get; set; }

        // eureka:instance:hostName
        public virtual string HostName
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

        public virtual string IpAddress { get; set; }

        public virtual string AppGroupName { get; set; }

        public virtual bool IsInstanceEnabledOnInit { get; set; }

        public virtual int NonSecurePort { get; set; }

        public virtual bool IsNonSecurePortEnabled { get; set; }

        public virtual string VirtualHostName { get; set; }

        public virtual string SecureVirtualHostName { get; set; }

        public virtual IDataCenterInfo DataCenterInfo { get; set; }

        public virtual string[] DefaultAddressResolutionOrder { get; set; }

        public virtual string GetHostName(bool refresh)
        {
            if (refresh || string.IsNullOrEmpty(_thisHostName))
            {
                _thisHostName = ResolveHostName();
            }

            return _thisHostName;
        }

        internal virtual string GetHostAddress(bool refresh)
        {
            if (refresh || string.IsNullOrEmpty(_thisHostAddress))
            {
                string hostName = GetHostName(refresh);
                if (!string.IsNullOrEmpty(hostName))
                {
                    _thisHostAddress = ResolveHostAddress(hostName);
                }
            }

            return _thisHostAddress;
        }

        protected virtual string ResolveHostAddress(string hostName)
        {
            string result = null;
            try
            {
                var results = Dns.GetHostAddresses(hostName);
                if (results != null && results.Length > 0)
                {
                    foreach (var addr in results)
                    {
                        if (addr.Equals(AddressFamily.InterNetwork))
                        {
                            result = addr.ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return result;
        }

        protected virtual string ResolveHostName()
        {
            string result = null;
            try
            {
                result = Dns.GetHostName();
                if (!string.IsNullOrEmpty(result))
                {
                    var response = Dns.GetHostEntry(result);
                    if (response != null)
                    {
                        return response.HostName;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return result;
        }
    }
}
