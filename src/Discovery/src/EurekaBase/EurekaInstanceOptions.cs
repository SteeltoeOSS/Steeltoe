// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Discovery;
using Steeltoe.Common.Net;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaInstanceOptions : EurekaInstanceConfig, IDiscoveryRegistrationOptions
    {
        public const string EUREKA_INSTANCE_CONFIGURATION_PREFIX = "eureka:instance";

        public new const string Default_StatusPageUrlPath = "/info";
        public new const string Default_HealthCheckUrlPath = "/health";

        public EurekaInstanceOptions()
        {
            StatusPageUrlPath = Default_StatusPageUrlPath;
            HealthCheckUrlPath = Default_HealthCheckUrlPath;
            IsInstanceEnabledOnInit = true;
            VirtualHostName = null;
            SecureVirtualHostName = null;
            InstanceId = GetHostName(false) + ":" + AppName + ":" + NonSecurePort;
        }

        // eureka:instance:appGroup
        public virtual string AppGroup
        {
            get => AppGroupName;

            set => AppGroupName = value;
        }

        // eureka:instance:instanceEnabledOnInit
        public virtual bool InstanceEnabledOnInit
        {
            get => IsInstanceEnabledOnInit;

            set => IsInstanceEnabledOnInit = value;
        }

        // eureka:instance:port
        public virtual int Port
        {
            get => NonSecurePort;

            set => NonSecurePort = value;
        }

        // eureka:instance:nonSecurePortEnabled
        public virtual bool NonSecurePortEnabled
        {
            get => IsNonSecurePortEnabled;

            set => IsNonSecurePortEnabled = value;
        }

        // eureka:instance:vipAddress
        public virtual string VipAddress
        {
            get => VirtualHostName;

            set => VirtualHostName = value;
        }

        // eureka:instance:secureVipAddress
        public virtual string SecureVipAddress
        {
            get => SecureVirtualHostName;

            set => SecureVirtualHostName = value;
        }

        // spring:cloud:discovery:registrationMethod changed to  eureka:instance:registrationMethod
        public virtual string RegistrationMethod { get; set; }

        private string _ipAddress;

        public override string IpAddress
        {
            get => _ipAddress ?? _thisHostAddress;

            set => _ipAddress = value;
        }

        private string _hostName;

        public override string HostName
        {
            // _hostName is accessed by GetHostName()
#pragma warning disable S4275 // Getters and setters should access the expected fields
            get => GetHostName(false);
#pragma warning restore S4275 // Getters and setters should access the expected fields

            set
            {
                if (!value.Equals(_thisHostName))
                {
                    _hostName = value;
                }
            }
        }

        public override string GetHostName(bool refresh)
        {
            if (_hostName != null)
            {
                return _hostName;
            }

            if (refresh || string.IsNullOrEmpty(_thisHostName))
            {
                _thisHostName = DnsTools.ResolveHostName();
            }

            return _thisHostName;
        }
    }
}
