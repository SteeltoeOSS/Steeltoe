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

using SteelToe.Discovery.Eureka.AppInfo;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SteelToe.Discovery.Eureka
{
    public class EurekaInstanceConfig : IEurekaInstanceConfig
    {
        public const int Default_NonSecurePort = 80;
        public const int Default_SecurePort = 443;
        public const int Default_LeaseRenewalIntervalInSeconds = 30;
        public const int Default_LeaseExpirationDurationInSeconds = 90;
        public const string Default_Appname = "unknown";
        public const string Default_StatusPageUrlPath = "/Status";  // TODO: /info for spring
        public const string Default_HomePageUrlPath = "/";
        public const string Default_HealthCheckUrlPath = "/healthcheck"; // TODO: /health for spring

        private string thisHostAddress;
        private string thisHostName;

        public EurekaInstanceConfig()
        {
            thisHostName = GetHostName(true);
            thisHostAddress = GetHostAddress(true);

            IsInstanceEnabledOnInit = false;
            NonSecurePort = Default_NonSecurePort;
            SecurePort = Default_SecurePort;
            IsNonSecurePortEnabled = true;
            SecurePortEnabled = false;
            LeaseRenewalIntervalInSeconds = Default_LeaseRenewalIntervalInSeconds;
            LeaseExpirationDurationInSeconds = Default_LeaseExpirationDurationInSeconds;
            VirtualHostName = thisHostName + ":" + NonSecurePort;
            SecureVirtualHostName = thisHostName + ":" + SecurePort;
            IpAddress = thisHostAddress;
            AppName = Default_Appname;
            StatusPageUrlPath = Default_StatusPageUrlPath;
            HomePageUrlPath = Default_HomePageUrlPath;
            HealthCheckUrlPath = Default_HealthCheckUrlPath;
            MetadataMap = new Dictionary<string, string>();
            DataCenterInfo = new DataCenterInfo(DataCenterName.MyOwn);
        }
        public string InstanceId { get; set; }
        public string AppName { get; set; }
        public string AppGroupName { get; set; }
        public bool IsInstanceEnabledOnInit { get; set; }
        public int NonSecurePort { get; set; }
        public int SecurePort { get; set; }
        public bool IsNonSecurePortEnabled { get; set; }
        public bool SecurePortEnabled { get; set; }
        public int LeaseRenewalIntervalInSeconds { get; set; }
        public int LeaseExpirationDurationInSeconds { get; set; }
        public string VirtualHostName { get; set; }
        public string SecureVirtualHostName { get; set; }
        public string ASGName { get; set; }
        public IDictionary<string, string> MetadataMap { get; set; }
        public IDataCenterInfo DataCenterInfo { get; set; }
        public string IpAddress { get; set; }
        public string StatusPageUrlPath { get; set; }
        public string StatusPageUrl { get; set; }
        public string HomePageUrlPath { get; set; }
        public string HomePageUrl { get; set; }
        public string HealthCheckUrlPath { get; set; }
        public string HealthCheckUrl { get; set; }
        public string SecureHealthCheckUrl { get; set; }
        public string[] DefaultAddressResolutionOrder { get; set; }
        public string GetHostName(bool refresh)
        {
            if (refresh || string.IsNullOrEmpty(thisHostName))
                thisHostName = Dns.GetHostName();

            return thisHostName;

        }
        public string HostName
        {
            get
            {
                return thisHostName;
            }
            set
            {
                thisHostName = value;
            }
        }

        internal string GetHostAddress(bool refresh)
        {
            if (refresh || string.IsNullOrEmpty(thisHostAddress))
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
                            thisHostAddress = result.ToString();
                            break;
                        }
                    }
                }
            }
            return thisHostAddress;

        }

    }
}
