// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaInstanceOptions : EurekaInstanceConfig, IDiscoveryRegistrationOptions
    {
        public const string EUREKA_INSTANCE_CONFIGURATION_PREFIX = "eureka:instance";

        public new const string Default_StatusPageUrlPath = "/info";
        public new const string Default_HealthCheckUrlPath = "/health";

        internal const int DEFAULT_NONSECUREPORT = 80;
        internal const int DEFAULT_SECUREPORT = 443;

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
                _thisHostName = ResolveHostName();
            }

            return _thisHostName;
        }

        public void ApplyConfigUrls(List<Uri> addresses, string wildcard_hostname)
        {
            // try to pull some values out of server config to override defaults, but only if registration method hasn't been set
            // if registration method has been set, the user probably wants to define their own behavior
            if (addresses.Any() && string.IsNullOrEmpty(RegistrationMethod))
            {
                foreach (var address in addresses)
                {
                    if (address.Scheme == "http" && Port == DEFAULT_NONSECUREPORT)
                    {
                        Port = address.Port;
                    }
                    else if (address.Scheme == "https" && SecurePort == DEFAULT_SECUREPORT)
                    {
                        SecurePort = address.Port;
                    }

                    if (!address.Host.Equals(wildcard_hostname) && !address.Host.Equals("0.0.0.0"))
                    {
                        HostName = address.Host;
                    }
                }
            }
        }

        public void SetInstanceId(IConfiguration config)
        {
            var defaultId = GetHostName(false) + ":" + EurekaInstanceOptions.Default_Appname + ":" + EurekaInstanceOptions.Default_NonSecurePort;
            if (defaultId.Equals(InstanceId))
            {
                var springInstanceId = config.GetValue<string>(EurekaPostConfigurer.SPRING_APPLICATION_INSTANCEID_KEY);
                if (!string.IsNullOrEmpty(springInstanceId))
                {
                    InstanceId = springInstanceId;
                }
                else
                {
                    if (SecurePortEnabled)
                    {
                        InstanceId = GetHostName(false) + ":" + AppName + ":" + SecurePort;
                    }
                    else
                    {
                        InstanceId = GetHostName(false) + ":" + AppName + ":" + NonSecurePort;
                    }
                }
            }
        }
    }
}
