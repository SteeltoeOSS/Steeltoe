// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Net;
using Steeltoe.Discovery.Eureka.AppInfo;
using System.Collections.Generic;

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
#pragma warning disable S1699 // Constructors should only call non-overridable methods
            _thisHostName = GetHostName(true);
            _thisHostAddress = GetHostAddress(true);
#pragma warning restore S1699 // Constructors should only call non-overridable methods

            IsInstanceEnabledOnInit = false;
            NonSecurePort = Default_NonSecurePort;
            SecurePort = Default_SecurePort;
            IsNonSecurePortEnabled = true;
            SecurePortEnabled = false;
            LeaseRenewalIntervalInSeconds = Default_LeaseRenewalIntervalInSeconds;
            LeaseExpirationDurationInSeconds = Default_LeaseExpirationDurationInSeconds;
            VirtualHostName = $"{_thisHostName}:{NonSecurePort}";
            SecureVirtualHostName = $"{_thisHostName}:{SecurePort}";
            IpAddress = _thisHostAddress;
            AppName = Default_Appname;
            StatusPageUrlPath = Default_StatusPageUrlPath;
            HomePageUrlPath = Default_HomePageUrlPath;
            HealthCheckUrlPath = Default_HealthCheckUrlPath;
            MetadataMap = new Dictionary<string, string>();
            DataCenterInfo = new DataCenterInfo(DataCenterName.MyOwn);
            PreferIpAddress = false;
        }

        public void ApplyNetUtils()
        {
            if (UseNetUtils && NetUtils != null)
            {
                var host = NetUtils.FindFirstNonLoopbackHostInfo();
                if (host.Hostname != null)
                {
                    _thisHostName = host.Hostname;
                }

                IpAddress = host.IpAddress;
            }
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
            get => _thisHostName;

            set => _thisHostName = value;
        }

        public virtual string IpAddress { get; set; }

        public virtual string AppGroupName { get; set; }

        public virtual bool IsInstanceEnabledOnInit { get; set; }

        public virtual int NonSecurePort { get; set; }

        public virtual bool IsNonSecurePortEnabled { get; set; }

        public virtual string VirtualHostName { get; set; }

        public virtual string SecureVirtualHostName { get; set; }

        public virtual IDataCenterInfo DataCenterInfo { get; set; }

        public virtual IEnumerable<string> DefaultAddressResolutionOrder { get; set; }

        public bool UseNetUtils { get; set; }

        public InetUtils NetUtils { get; set; }

        public virtual string GetHostName(bool refresh)
        {
            if (refresh || string.IsNullOrEmpty(_thisHostName))
            {
                if (UseNetUtils && NetUtils != null)
                {
                    return NetUtils.FindFirstNonLoopbackHostInfo().Hostname;
                }
                else
                {
                    _thisHostName = DnsTools.ResolveHostName();
                }
            }

            return _thisHostName;
        }

        internal virtual string GetHostAddress(bool refresh)
        {
            if (refresh || string.IsNullOrEmpty(_thisHostAddress))
            {
                if (UseNetUtils && NetUtils != null)
                {
                    _thisHostAddress = NetUtils.FindFirstNonLoopbackAddress().ToString();
                }
                else
                {
                    var hostName = GetHostName(refresh);
                    if (!string.IsNullOrEmpty(hostName))
                    {
                        _thisHostAddress = DnsTools.ResolveHostAddress(hostName);
                    }
                }
            }

            return _thisHostAddress;
        }
    }
}
