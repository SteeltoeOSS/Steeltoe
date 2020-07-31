// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            get
            {
                return AppGroupName;
            }

            set
            {
                AppGroupName = value;
            }
        }

        // eureka:instance:instanceEnabledOnInit
        public virtual bool InstanceEnabledOnInit
        {
            get
            {
                return IsInstanceEnabledOnInit;
            }

            set
            {
                IsInstanceEnabledOnInit = value;
            }
        }

        // eureka:instance:port
        public virtual int Port
        {
            get
            {
                return NonSecurePort;
            }

            set
            {
                NonSecurePort = value;
            }
        }

        // eureka:instance:nonSecurePortEnabled
        public virtual bool NonSecurePortEnabled
        {
            get
            {
                return IsNonSecurePortEnabled;
            }

            set
            {
                IsNonSecurePortEnabled = value;
            }
        }

        // eureka:instance:vipAddress
        public virtual string VipAddress
        {
            get
            {
                return VirtualHostName;
            }

            set
            {
                VirtualHostName = value;
            }
        }

        // eureka:instance:secureVipAddress
        public virtual string SecureVipAddress
        {
            get
            {
                return SecureVirtualHostName;
            }

            set
            {
                SecureVirtualHostName = value;
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
                _thisHostName = ResolveHostName();
            }

            return _thisHostName;
        }
    }
}
