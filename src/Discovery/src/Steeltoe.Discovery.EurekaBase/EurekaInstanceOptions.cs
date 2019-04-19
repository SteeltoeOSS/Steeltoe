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

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaInstanceOptions : EurekaInstanceConfig, IDiscoveryRegistrationOptions
    {
        public const string EUREKA_INSTANCE_CONFIGURATION_PREFIX = "eureka:instance";

        public new const string Default_StatusPageUrlPath = "/info";
        public new const string Default_HealthCheckUrlPath = "/health";

        public EurekaInstanceOptions()
        {
            this.StatusPageUrlPath = Default_StatusPageUrlPath;
            this.HealthCheckUrlPath = Default_HealthCheckUrlPath;
            this.IsInstanceEnabledOnInit = true;
            this.VirtualHostName = null;
            this.SecureVirtualHostName = null;
            this.InstanceId = GetHostName(false) + ":" + AppName + ":" + NonSecurePort;
        }

        // eureka:instance:appGroup
        public virtual string AppGroup
        {
            get
            {
                return this.AppGroupName;
            }

            set
            {
                this.AppGroupName = value;
            }
        }

        // eureka:instance:instanceEnabledOnInit
        public virtual bool InstanceEnabledOnInit
        {
            get
            {
                return this.IsInstanceEnabledOnInit;
            }

            set
            {
                this.IsInstanceEnabledOnInit = value;
            }
        }

        // eureka:instance:port
        public virtual int Port
        {
            get
            {
                return this.NonSecurePort;
            }

            set
            {
                this.NonSecurePort = value;
            }
        }

        // eureka:instance:nonSecurePortEnabled
        public virtual bool NonSecurePortEnabled
        {
            get
            {
                return this.IsNonSecurePortEnabled;
            }

            set
            {
                this.IsNonSecurePortEnabled = value;
            }
        }

        // eureka:instance:vipAddress
        public virtual string VipAddress
        {
            get
            {
                return this.VirtualHostName;
            }

            set
            {
                this.VirtualHostName = value;
            }
        }

        // eureka:instance:secureVipAddress
        public virtual string SecureVipAddress
        {
            get
            {
                return this.SecureVirtualHostName;
            }

            set
            {
                this.SecureVirtualHostName = value;
            }
        }

        // spring:cloud:discovery:registrationMethod changed to  eureka:instance:registrationMethod
        public virtual string RegistrationMethod { get; set; }

        private string _ipAddress;

        public override string IpAddress
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

        private string _hostName;

        public override string HostName
        {
            get
            {
                return GetHostName(false);
            }

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
                _thisHostName = ResolveHostName();
            }

            return _thisHostName;
        }
    }
}
