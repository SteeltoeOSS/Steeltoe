// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Net;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public class EurekaInstanceConfiguration
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

    /// <summary>
    /// Gets or sets the unique Id (within the scope of the appName) of this instance to be registered with eureka. Configuration property:
    /// eureka:instance:instanceId.
    /// </summary>
    public virtual string InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the name of the application to be registered with eureka. Configuration property: eureka:instance:name.
    /// </summary>
    public virtual string AppName { get; set; }

    /// <summary>
    /// Gets or sets the secure port on which the instance should receive traffic. Configuration property: eureka:instance:securePort.
    /// </summary>
    public virtual int SecurePort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the <c>secure</c> port should be enabled for traffic or not. Set true if the <c>secure</c>
    /// port is enabled, false otherwise. Configuration property: eureka:instance:securePortEnabled.
    /// </summary>
    public virtual bool SecurePortEnabled { get; set; }

    /// <summary>
    /// Gets or sets indicates how often (in seconds) the eureka client needs to send heartbeats to eureka server to indicate that it is still alive. If the
    /// heartbeats are not received for the period specified in <see cref="LeaseExpirationDurationInSeconds" />, eureka server will remove the instance from
    /// its view, there by disallowing traffic to this instance. Note that the instance could still not take traffic if it implements HealthCheckCallback and
    /// then decides to make itself unavailable. Configuration property: eureka:instance:leaseRenewalIntervalInSeconds.
    /// </summary>
    public virtual int LeaseRenewalIntervalInSeconds { get; set; }

    /// <summary>
    /// Gets or sets indicates the time in seconds that the eureka server waits since it received the last heartbeat before it can remove this instance from
    /// its view and there by disallowing traffic to this instance. Setting this value too long could mean that the traffic could be routed to the instance
    /// even though the instance is not alive. Setting this value too small could mean, the instance may be taken out of traffic because of temporary network
    /// glitches.This value to be set to at least higher than the value specified in <see cref="LeaseRenewalIntervalInSeconds" /> Configuration property:
    /// eureka:instance:leaseExpirationDurationInSeconds.
    /// </summary>
    public virtual int LeaseExpirationDurationInSeconds { get; set; }

    /// <summary>
    /// Gets or sets the AWS auto-scaling group name associated with this instance. This information is specifically used in an AWS environment to
    /// automatically put an instance out of service after the instance is launched, and it has been disabled for traffic. Configuration property:
    /// eureka:instance:asgName.
    /// </summary>
    public virtual string AsgName { get; set; }

    /// <summary>
    /// Gets or sets the metadata name/value pairs associated with this instance. This information is sent to eureka server and can be used by other
    /// instances. Configuration property: eureka:instance:metadataMap.
    /// </summary>
    public virtual IDictionary<string, string> MetadataMap { get; set; }

    /// <summary>
    /// Gets or sets the relative status page <em>Path</em> for this instance. The status page URL is then constructed out of the
    /// <see cref="ResolveHostName" /> and the type of communication - secure or insecure as specified in <see cref="SecurePort" /> and
    /// <see cref="NonSecurePort" />. It is normally used for informational purposes for other services to find about the status of this instance. Users can
    /// provide a simple <c>HTML</c> indicating what is the current status of the instance. Configuration property: eureka:instance:statusPageUrlPath.
    /// </summary>
    public virtual string StatusPageUrlPath { get; set; }

    /// <summary>
    /// Gets or sets the absolute status page for this instance. The users can provide the StatusPageUrlPath if the status page resides in the same instance
    /// talking to eureka, else in the cases where the instance is a proxy for some other server, users can provide the full URL. If the full URL is provided
    /// it takes precedence. It is normally used for informational purposes for other services to find about the status of this instance. Users can provide a
    /// simple <c>HTML</c> indicating what is the current status of the instance. The full URL should follow the format http://${eureka.hostname}:7001/ where
    /// the value ${eureka.hostname} is replaced at runtime. Configuration property: eureka:instance:statusPageUrl.
    /// </summary>
    public virtual string StatusPageUrl { get; set; }

    /// <summary>
    /// Gets or sets gets the relative home page URL <em>Path</em> for this instance. The home page URL is then constructed out of the
    /// <see cref="ResolveHostName" /> and the type of communication - secure or insecure as specified in <see cref="SecurePort" /> and
    /// <see cref="NonSecurePort" /> It is normally used for informational purposes for other services to use it as a landing page. Configuration property:
    /// eureka:instance:homePageUrlPath.
    /// </summary>
    public virtual string HomePageUrlPath { get; set; }

    /// <summary>
    /// Gets or sets gets the absolute home page URL for this instance. The users can provide the path if the home page resides in the same instance talking
    /// to eureka, else in the cases where the instance is a proxy for some other server, users can provide the full URL. If the full URL is provided it
    /// takes precedence. It is normally used for informational purposes for other services to find about the status of this instance. Users can provide a
    /// simple <c>HTML</c> indicating what is the current status of the instance. The full URL should follow the format http://${eureka.hostname}:7001/ where
    /// the value ${eureka.hostname} is replaced at runtime. Configuration property: eureka:instance:homePageUrl.
    /// </summary>
    public virtual string HomePageUrl { get; set; }

    /// <summary>
    /// Gets or sets gets the relative health check URL <em>Path</em> for this instance. The health check page URL is then constructed out of the
    /// <see cref="ResolveHostName" /> and the type of communication - secure or insecure as specified in <see cref="SecurePort" /> and
    /// <see cref="NonSecurePort" /> It is normally used for making educated decisions based on the health of the instance - for example, it can be used to
    /// determine whether to proceed deployments to an entire farm or stop the deployments without causing further damage. Configuration property:
    /// eureka:instance:healthCheckUrlPath.
    /// </summary>
    public virtual string HealthCheckUrlPath { get; set; }

    /// <summary>
    /// Gets or sets gets the absolute health check page URL for this instance. The users can provide the path if the health check page resides in the same
    /// instance talking to eureka, else in the cases where the instance is a proxy for some other server, users can provide the full URL. If the full URL is
    /// provided it takes precedence. It is normally used for making educated decisions based on the health of the instance - for example, it can be used to
    /// determine whether to/ proceed deployments to an entire farm or stop the deployments without causing further damage.  The full URL should follow the
    /// format http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime. Configuration property:
    /// eureka:instance:healthCheckUrl.
    /// </summary>
    public virtual string HealthCheckUrl { get; set; }

    /// <summary>
    /// Gets or sets gets the absolute secure health check page URL for this instance. The users can provide the path if the health check page resides in the
    /// same instance talking to eureka, else in the cases where the instance is a proxy for some other server, users can provide the full URL. If the full
    /// URL is provided it takes precedence. It is normally used for making educated decisions based on the health of the instance - for example, it can be
    /// used to determine whether to/ proceed deployments to an entire farm or stop the deployments without causing further damage.  The full URL should
    /// follow the format http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime. Configuration property:
    /// eureka:instance:secureHealthCheckUrl.
    /// </summary>
    public virtual string SecureHealthCheckUrl { get; set; }

    // eureka:instance:preferIPAddress
    public virtual bool PreferIPAddress { get; set; }

    // eureka:instance:hostName
    public virtual string HostName { get; set; }

    /// <summary>
    /// Gets or sets the IPAddress of the instance. This information is for academic purposes only as the communication from other instances primarily happen
    /// using the information supplied in <see cref="ResolveHostName" />.
    /// </summary>
    public virtual string IPAddress { get; set; }

    /// <summary>
    /// Gets or sets the name of the application group to be registered with eureka. Configuration property: eureka:instance:appGroup.
    /// </summary>
    public virtual string AppGroupName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the instance should be enabled for taking traffic as soon as it is registered with eureka.
    /// Sometimes the application might need to do some pre-processing before it is ready to take traffic. Configuration property:
    /// eureka:instance:instanceEnabledOnInit.
    /// </summary>
    public virtual bool IsInstanceEnabledOnInit { get; set; }

    /// <summary>
    /// Gets or sets the non-secure port on which the instance should receive traffic. Configuration property: eureka:instance:port.
    /// </summary>
    public virtual int NonSecurePort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the non-secure port should be enabled for traffic or not. Set true if the non-secure port
    /// is enabled, false otherwise. Configuration property: eureka:instance:nonSecurePortEnabled.
    /// </summary>
    public virtual bool IsNonSecurePortEnabled { get; set; }

    /// <summary>
    /// Gets or sets the virtual host name defined for this instance. This is typically the way other instance would find this instance by using the virtual
    /// host name. Think of this as similar to the fully qualified domain name, that the users of your services will need to find this instance.
    /// Configuration property: eureka:instance:vipAddress.
    /// </summary>
    public virtual string VirtualHostName { get; set; }

    /// <summary>
    /// Gets or sets the secure virtual host name defined for this instance. This is typically the way other instance would find this instance by using the
    /// virtual host name. Think of this as similar to the fully qualified domain name, that the users of your services will need to find this instance.
    /// Configuration property: eureka:instance:secureVipAddress.
    /// </summary>
    public virtual string SecureVirtualHostName { get; set; }

    /// <summary>
    /// Gets or sets the data center this instance is deployed. This information is used to get some AWS specific instance information if the instance is
    /// deployed in AWS.
    /// </summary>
    public virtual DataCenterInfo DataCenterInfo { get; set; }

    /// <summary>
    /// Gets or sets an instance's network addresses should be fully expressed in it's <see cref="DataCenterInfo" /> For example for instances in AWS, this
    /// will include the publicHostname, publicIP, privateHostname and privateIP, when available. The <see cref="InstanceInfo" /> will further express a
    /// "default address", which is a field that can be configured by the registering instance to advertise its default address. This configuration allowed
    /// for the expression of an ordered list of fields that can be used to resolve the default address. The exact field values will depend on the
    /// implementation details of the corresponding implementing DataCenterInfo types.
    /// </summary>
    public virtual IEnumerable<string> DefaultAddressResolutionOrder { get; set; }

    public bool UseNetUtils { get; set; }

    internal InetUtils NetUtils { get; set; }

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

    /// <summary>
    /// Gets the hostname associated with this instance. This is the exact name that would be used by other instances to make calls.
    /// </summary>
    /// <param name="refresh">
    /// refresh hostname.
    /// </param>
    /// <returns>
    /// hostname.
    /// </returns>
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
