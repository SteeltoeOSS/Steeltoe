// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Net;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public class EurekaInstanceConfiguration : IEurekaInstanceConfig
{
    public const int DefaultNonSecurePort = 80;
    public const int DefaultSecurePort = 443;
    public const int DefaultLeaseRenewalIntervalInSeconds = 30;
    public const int DefaultLeaseExpirationDurationInSeconds = 90;
    public const string DefaultAppName = "unknown";
    public const string DefaultStatusPageUrlPath = "/Status";
    public const string DefaultHomePageUrlPath = "/";
    public const string DefaultHealthCheckUrlPath = "/healthcheck";

    protected string thisHostAddress;

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
    public virtual string AsgName { get; set; }

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

    // eureka:instance:preferIPAddress
    public virtual bool PreferIPAddress { get; set; }

    // eureka:instance:hostName
    public virtual string HostName { get; set; }

    public virtual string IPAddress { get; set; }

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

    public EurekaInstanceConfiguration()
    {
#pragma warning disable S1699 // Constructors should only call non-overridable methods
        HostName = ResolveHostName(true);
        thisHostAddress = GetHostAddress(true);
#pragma warning restore S1699 // Constructors should only call non-overridable methods

        IsInstanceEnabledOnInit = false;
        NonSecurePort = DefaultNonSecurePort;
        SecurePort = DefaultSecurePort;
        IsNonSecurePortEnabled = true;
        SecurePortEnabled = false;
        LeaseRenewalIntervalInSeconds = DefaultLeaseRenewalIntervalInSeconds;
        LeaseExpirationDurationInSeconds = DefaultLeaseExpirationDurationInSeconds;
        VirtualHostName = $"{HostName}:{NonSecurePort}";
        SecureVirtualHostName = $"{HostName}:{SecurePort}";
        IPAddress = thisHostAddress;
        AppName = DefaultAppName;
        StatusPageUrlPath = DefaultStatusPageUrlPath;
        HomePageUrlPath = DefaultHomePageUrlPath;
        HealthCheckUrlPath = DefaultHealthCheckUrlPath;
        MetadataMap = new Dictionary<string, string>();
        DataCenterInfo = new DataCenterInfo(DataCenterName.MyOwn);
        PreferIPAddress = false;
    }

    public void ApplyNetUtils()
    {
        if (UseNetUtils && NetUtils != null)
        {
            HostInfo host = NetUtils.FindFirstNonLoopbackHostInfo();

            if (host.Hostname != null)
            {
                HostName = host.Hostname;
            }

            IPAddress = host.IPAddress;
        }
    }

    public virtual string ResolveHostName(bool refresh)
    {
        if (refresh || string.IsNullOrEmpty(HostName))
        {
            if (UseNetUtils && NetUtils != null)
            {
                return NetUtils.FindFirstNonLoopbackHostInfo().Hostname;
            }

            HostName = DnsTools.ResolveHostName();
        }

        return HostName;
    }

    internal virtual string GetHostAddress(bool refresh)
    {
        if (refresh || string.IsNullOrEmpty(thisHostAddress))
        {
            if (UseNetUtils && NetUtils != null)
            {
                thisHostAddress = NetUtils.FindFirstNonLoopbackAddress().ToString();
            }
            else
            {
                string hostName = ResolveHostName(refresh);

                if (!string.IsNullOrEmpty(hostName))
                {
                    thisHostAddress = DnsTools.ResolveHostAddress(hostName);
                }
            }
        }

        return thisHostAddress;
    }
}
