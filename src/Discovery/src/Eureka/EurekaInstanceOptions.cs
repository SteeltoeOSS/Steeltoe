// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Http;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public sealed class EurekaInstanceOptions
{
    internal const string EurekaInstanceConfigurationPrefix = "eureka:instance";
    internal const string DefaultStatusPageUrlPath = "/info";
    internal const string DefaultHealthCheckUrlPath = "/health";
    internal const int DefaultNonSecurePort = 80;
    internal const int DefaultSecurePort = 443;
    internal const int DefaultLeaseRenewalIntervalInSeconds = 30;
    internal const int DefaultLeaseExpirationDurationInSeconds = 90;
    internal const string DefaultAppName = "unknown";
    internal const string DefaultHomePageUrlPath = "/";

    private string? _ipAddress;
    private string? _hostName;
    private string? _thisHostAddress;

    internal InetUtils? NetUtils { get; set; }

    /// <summary>
    /// Gets or sets the unique ID (within the scope of the app name) of this instance to be registered with Eureka. Configuration property:
    /// eureka:instance:instanceId.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the name of the application to be registered with Eureka. Configuration property: eureka:instance:appName.
    /// </summary>
    public string? AppName { get; set; } = DefaultAppName;

    /// <summary>
    /// Gets or sets the name of the application group to be registered with Eureka. Configuration property: eureka:instance:appGroup.
    /// </summary>
    [ConfigurationKeyName("AppGroup")]
    public string? AppGroupName { get; set; }

    /// <summary>
    /// Gets the metadata name/value pairs associated with this instance. This information is sent to Eureka server and can be used by other instances.
    /// Configuration property: eureka:instance:metadataMap.
    /// </summary>
    public IDictionary<string, string> MetadataMap { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets configuration property: eureka:instance:hostName.
    /// </summary>
    public string? HostName
    {
        get => ResolveHostName(false);
        set => _hostName = value;
    }

    /// <summary>
    /// Gets or sets the IP address of the instance. This information is for academic purposes only, as the communication from other instances primarily
    /// happens using the information supplied in <see cref="ResolveHostName" />.
    /// </summary>
    public string? IPAddress
    {
        get => _ipAddress ?? _thisHostAddress;
        set => _ipAddress = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether usage of IP address should be preferred. Configuration property: eureka:instance:preferIPAddress.
    /// </summary>
    public bool PreferIPAddress { get; set; }

    /// <summary>
    /// Gets or sets the virtual host name defined for this instance. This is typically the way other instance would find this instance by using the virtual
    /// host name. Think of this as similar to the fully qualified domain name, that the users of your services will need to find this instance.
    /// Configuration property: eureka:instance:vipAddress.
    /// </summary>
    [ConfigurationKeyName("VipAddress")]
    public string? VirtualHostName { get; set; }

    /// <summary>
    /// Gets or sets the secure virtual host name defined for this instance. This is typically the way other instance would find this instance by using the
    /// virtual host name. Think of this as similar to the fully qualified domain name, that the users of your services will need to find this instance.
    /// Configuration property: eureka:instance:secureVipAddress.
    /// </summary>
    [ConfigurationKeyName("SecureVipAddress")]
    public string? SecureVirtualHostName { get; set; }

    /// <summary>
    /// Gets or sets the non-secure port on which the instance should receive traffic. Configuration property: eureka:instance:port.
    /// </summary>
    [ConfigurationKeyName("Port")]
    public int NonSecurePort { get; set; } = DefaultNonSecurePort;

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the non-secure port should be enabled for traffic or not. Set true if the non-secure port
    /// is enabled, false otherwise. Configuration property: eureka:instance:nonSecurePortEnabled.
    /// </summary>
    [ConfigurationKeyName("NonSecurePortEnabled")]
    public bool IsNonSecurePortEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the secure port on which the instance should receive traffic. Configuration property: eureka:instance:securePort.
    /// </summary>
    public int SecurePort { get; set; } = DefaultSecurePort;

    /// <summary>
    /// Gets or sets a value indicating whether the secure port should be enabled for traffic or not. Configuration property:
    /// eureka:instance:securePortEnabled.
    /// </summary>
    [ConfigurationKeyName("SecurePortEnabled")]
    public bool IsSecurePortEnabled { get; set; }

    /// <summary>
    /// Gets or sets configuration property: eureka:instance:registrationMethod, with fallback to: spring:cloud:discovery:registrationMethod.
    /// </summary>
    public string? RegistrationMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the instance should be enabled for taking traffic as soon as it is registered with eureka. Sometimes the
    /// application might need to do some pre-processing before it is ready to take traffic. Configuration property: eureka:instance:instanceEnabledOnInit.
    /// </summary>
    [ConfigurationKeyName("InstanceEnabledOnInit")]
    public bool IsInstanceEnabledOnInit { get; set; } = true;

    /// <summary>
    /// Gets or sets how often (in seconds) the Eureka client needs to send heartbeats to Eureka server to indicate that it is still alive. If the heartbeats
    /// are not received for the period specified in <see cref="LeaseExpirationDurationInSeconds" />, Eureka server will remove the instance from its view,
    /// thereby disallowing traffic to this instance. Note that the instance could still not take traffic if it implements HealthCheckCallback and then
    /// decides to make itself unavailable. Configuration property: eureka:instance:leaseRenewalIntervalInSeconds.
    /// </summary>
    public int LeaseRenewalIntervalInSeconds { get; set; } = DefaultLeaseRenewalIntervalInSeconds;

    /// <summary>
    /// Gets or sets the time in seconds that the Eureka server waits since it received the last heartbeat before it can remove this instance from its view
    /// and thereby disallowing traffic to this instance. Setting this value too long could mean that the traffic could be routed to the instance even though
    /// the instance is not alive. Setting this value too small could mean the instance may be taken out of traffic because of temporary network glitches.
    /// This value is to be set to at least higher than the value specified in <see cref="LeaseRenewalIntervalInSeconds" />. Configuration property:
    /// eureka:instance:leaseExpirationDurationInSeconds.
    /// </summary>
    public int LeaseExpirationDurationInSeconds { get; set; } = DefaultLeaseExpirationDurationInSeconds;

    /// <summary>
    /// Gets or sets the relative path to the status page for this instance. The status page URL is then constructed out of the
    /// <see cref="ResolveHostName" /> and the type of communication - secure or insecure, as specified in <see cref="SecurePort" /> and
    /// <see cref="NonSecurePort" />. It is normally used for informational purposes for other services to find out about the status of this instance. Users
    /// can provide a simple HTML page indicating what the current status of the instance is. Configuration property: eureka:instance:statusPageUrlPath.
    /// </summary>
    public string? StatusPageUrlPath { get; set; } = DefaultStatusPageUrlPath;

    /// <summary>
    /// Gets or sets the absolute URL to the status page for this instance. Users can provide the <see cref="StatusPageUrlPath" /> if the status page resides
    /// in the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full URL. If the full
    /// URL is provided it takes precedence. It is normally used for informational purposes for other services to find out about the status of this instance.
    /// Users can provide a simple HTML page indicating what the current status of the instance is. The full URL should follow the format:
    /// http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime. Configuration property: eureka:instance:statusPageUrl.
    /// </summary>
    public string? StatusPageUrl { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the home page URL for this instance. The home page URL is then constructed out of the
    /// <see cref="ResolveHostName" /> and the type of communication - secure or insecure, as specified in <see cref="SecurePort" /> and
    /// <see cref="NonSecurePort" />. It is normally used for informational purposes for other services to use it as a landing page. Configuration property:
    /// eureka:instance:homePageUrlPath.
    /// </summary>
    public string? HomePageUrlPath { get; set; } = DefaultHomePageUrlPath;

    /// <summary>
    /// Gets or sets gets the absolute URL to the home page for this instance. Users can provide the <see cref="HomePageUrlPath" /> if the home page resides
    /// in the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full URL. If the full
    /// URL is provided it takes precedence. It is normally used for informational purposes for other services to find out about the status of this instance.
    /// Users can provide a simple HTML page indicating what the current status of the instance is. The full URL should follow the format:
    /// http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime. Configuration property: eureka:instance:homePageUrl.
    /// </summary>
    public string? HomePageUrl { get; set; }

    /// <summary>
    /// Gets or sets gets the relative path to the health check endpoint for this instance. The health check URL is then constructed out of the
    /// <see cref="ResolveHostName" /> and the type of communication - secure or insecure, as specified in <see cref="SecurePort" /> and
    /// <see cref="NonSecurePort" />. It is normally used for making educated decisions based on the health of the instance. For example, it can be used to
    /// determine whether to proceed deployments to an entire farm or stop the deployments without causing further damage. Configuration property:
    /// eureka:instance:healthCheckUrlPath.
    /// </summary>
    public string? HealthCheckUrlPath { get; set; } = DefaultHealthCheckUrlPath;

    /// <summary>
    /// Gets or sets gets the absolute URL for health checks of this instance. Users can provide the <see cref="HealthCheckUrlPath" /> if the health check
    /// endpoint resides in the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full
    /// URL. If the full URL is provided it takes precedence. It is normally used for making educated decisions based on the health of the instance. For
    /// example, it can be used to determine whether to proceed deployments to an entire farm or stop the deployments without causing further damage. The
    /// full URL should follow the format: http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime. Configuration property:
    /// eureka:instance:healthCheckUrl.
    /// </summary>
    public string? HealthCheckUrl { get; set; }

    /// <summary>
    /// Gets or sets gets the secure absolute URL for health checks of this instance. Users can provide the <see cref="HealthCheckUrlPath" /> if the health
    /// check endpoint resides in the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide
    /// the full URL. If the full URL is provided it takes precedence. It is normally used for making educated decisions based on the health of the instance.
    /// For example, it can be used to determine whether to proceed deployments to an entire farm or stop the deployments without causing further damage. The
    /// full URL should follow the format: https://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime. Configuration
    /// property: eureka:instance:secureHealthCheckUrl.
    /// </summary>
    public string? SecureHealthCheckUrl { get; set; }

    /// <summary>
    /// Gets or sets the AWS auto-scaling group name associated with this instance. This information is specifically used in an AWS environment to
    /// automatically put an instance out of service after the instance is launched, and it has been disabled for traffic. Configuration property:
    /// eureka:instance:asgName.
    /// </summary>
    public string? AsgName { get; set; }

    /// <summary>
    /// Gets or sets the data center this instance is deployed to. This information is used to get some AWS-specific instance information if the instance is
    /// deployed in AWS.
    /// </summary>
    public DataCenterInfo? DataCenterInfo { get; set; } = new(DataCenterName.MyOwn);

    /// <summary>
    /// Gets the address resolution order. An instance's network addresses should be fully expressed in its <see cref="DataCenterInfo" />. For example, for
    /// instances in AWS, this will include the public hostname, public IP, private hostname and private IP, when available. The <see cref="InstanceInfo" />
    /// will further express a "default address", which is a field that can be configured by the registering instance to advertise its default address. This
    /// configuration allows for the expression of an ordered list of fields that can be used to resolve the default address. The exact field values will
    /// depend on the implementation details of the corresponding implementing <see cref="DataCenterInfo" /> types.
    /// </summary>
    public IList<string> DefaultAddressResolutionOrder { get; } = new List<string>();

    public bool UseNetUtils { get; set; }

    public EurekaInstanceOptions()
    {
        HostName = ResolveHostName(true);
        _thisHostAddress = GetHostAddress(true);
        IPAddress = _thisHostAddress;
        InstanceId = $"{HostName}:{AppName}:{NonSecurePort}";
    }

    internal string? GetHostAddress(bool refresh)
    {
        if (refresh || string.IsNullOrEmpty(_thisHostAddress))
        {
            if (UseNetUtils && NetUtils != null)
            {
                _thisHostAddress = NetUtils.FindFirstNonLoopbackAddress()?.ToString();
            }
            else
            {
                string? hostName = ResolveHostName(refresh);

                if (!string.IsNullOrEmpty(hostName))
                {
                    _thisHostAddress = DnsTools.ResolveHostAddress(hostName);
                }
            }
        }

        return _thisHostAddress;
    }

    /// <summary>
    /// Gets the hostname associated with this instance. This is the exact name that would be used by other instances to make calls.
    /// </summary>
    /// <param name="refresh">
    /// Indicates whether to refresh the current hostname.
    /// </param>
    internal string? ResolveHostName(bool refresh)
    {
        if (_hostName != null)
        {
            return _hostName;
        }

        if (refresh || string.IsNullOrEmpty(_hostName))
        {
            _hostName = DnsTools.ResolveHostName();
        }

        return _hostName;
    }

    internal void ApplyConfigUrls(List<Uri> addresses)
    {
        // Only use addresses from configuration if there are any and registration method hasn't been set.
        // If registration method has been set, the user probably wants to define their own behavior.
        if (addresses.Count != 0 && string.IsNullOrEmpty(RegistrationMethod))
        {
            foreach (Uri address in addresses)
            {
                if (address.Scheme == "http" && NonSecurePort == DefaultNonSecurePort)
                {
                    NonSecurePort = address.Port;
                }
                else if (address.Scheme == "https" && SecurePort == DefaultSecurePort)
                {
                    SecurePort = address.Port;
                    IsSecurePortEnabled = true;
                    IsNonSecurePortEnabled = false;
                }

                if (!ConfigurationUrlHelpers.WildcardHosts.Contains(address.Host))
                {
                    HostName = address.Host;
                }
            }
        }
    }

    internal void ApplyNetUtils()
    {
        if (UseNetUtils && NetUtils != null)
        {
            HostInfo host = NetUtils.FindFirstNonLoopbackHostInfo();
            HostName = host.Hostname;
            IPAddress = host.IPAddress;
        }
    }
}
