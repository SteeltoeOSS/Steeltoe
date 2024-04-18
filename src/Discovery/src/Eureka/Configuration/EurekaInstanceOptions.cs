// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka.Configuration;

public sealed class EurekaInstanceOptions
{
    internal const string ConfigurationPrefix = "eureka:instance";
    internal const string DefaultStatusPageUrlPath = "/info";
    internal const string DefaultHealthCheckUrlPath = "/health";
    internal const int DefaultLeaseRenewalIntervalInSeconds = 30;
    internal const int DefaultLeaseExpirationDurationInSeconds = 90;
    internal const string DefaultAppName = "unknown";
    internal const string DefaultHomePageUrlPath = "/";

    private bool UseAspNetCoreUrls => !Platform.IsCloudFoundry || IsContainerToContainerMethod() || IsForceHostNameMethod();

    internal TimeSpan LeaseRenewalInterval => TimeSpan.FromSeconds(LeaseRenewalIntervalInSeconds);
    internal TimeSpan LeaseExpirationDuration => TimeSpan.FromSeconds(LeaseExpirationDurationInSeconds);

    /// <summary>
    /// Gets or sets the unique ID (within the scope of the app name) of this instance to be registered with Eureka. Configuration property:
    /// eureka:instance:instanceId.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the name of the application to be registered with Eureka. Configuration property: eureka:instance:appName.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the name of the application group to be registered with Eureka. Configuration property: eureka:instance:appGroup.
    /// </summary>
    [ConfigurationKeyName("AppGroup")]
    public string? AppGroupName { get; set; }

    /// <summary>
    /// Gets the metadata name/value pairs associated with this instance. This information is sent to Eureka server and can be used by other instances.
    /// Configuration property: eureka:instance:metadataMap.
    /// </summary>
    public IDictionary<string, string?> MetadataMap { get; } = new Dictionary<string, string?>();

    /// <summary>
    /// Gets or sets the hostname of this instance. Configuration property: eureka:instance:hostName.
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the instance.
    /// </summary>
    public string? IPAddress { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether usage of <see cref="IPAddress" />> should be preferred over <see cref="HostName" />. Configuration property:
    /// eureka:instance:preferIPAddress.
    /// </summary>
    public bool PreferIPAddress { get; set; }

    /// <summary>
    /// Gets or sets the Virtual Internet Protocol address(es) for this instance. Multiple values can be specified as a comma-separated list. When using
    /// service discovery, virtual addresses are resolved into real addresses on outgoing HTTP requests. Configuration property: eureka:instance:vipAddress.
    /// </summary>
    public string? VipAddress { get; set; }

    /// <summary>
    /// Gets or sets the Secure Virtual Internet Protocol address(es) for this instance. Multiple values can be specified as a comma-separated list. When
    /// using service discovery, secure virtual addresses are resolved into real addresses on outgoing HTTP requests. Configuration property:
    /// eureka:instance:secureVipAddress.
    /// </summary>
    public string? SecureVipAddress { get; set; }

    /// <summary>
    /// Gets or sets the non-secure port on which the instance should receive traffic. Configuration property: eureka:instance:port.
    /// </summary>
    [ConfigurationKeyName("Port")]
    public int? NonSecurePort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the secure port should be enabled for traffic or not. Configuration property:
    /// eureka:instance:nonSecurePortEnabled.
    /// </summary>
    [ConfigurationKeyName("NonSecurePortEnabled")]
    public bool IsNonSecurePortEnabled { get; set; }

    /// <summary>
    /// Gets or sets the secure port on which the instance should receive traffic. Configuration property: eureka:instance:securePort.
    /// </summary>
    public int? SecurePort { get; set; }

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
    /// Gets or sets how often (in seconds) the Eureka client sends heartbeats to Eureka server to indicate that it is still alive. If the heartbeats are not
    /// received for the period specified in <see cref="LeaseExpirationDurationInSeconds" />, Eureka server will remove the instance from its view, thereby
    /// disallowing traffic to this instance. Note that the instance could still not take traffic if it implements HealthCheckCallback and then decides to
    /// make itself unavailable. Configuration property: eureka:instance:leaseRenewalIntervalInSeconds.
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
    /// Gets or sets the relative path to the status page for this instance. The status page URL is then constructed out of the <see cref="HostName" /> and
    /// the type of communication - secure or non-secure, as specified in <see cref="SecurePort" /> and <see cref="NonSecurePort" />. It is normally used for
    /// informational purposes for other services to find out about the status of this instance. Users can provide a simple HTML page indicating what the
    /// current status of the instance is. Configuration property: eureka:instance:statusPageUrlPath.
    /// </summary>
    public string? StatusPageUrlPath { get; set; } = DefaultStatusPageUrlPath;

    /// <summary>
    /// Gets or sets the absolute URL to the status page for this instance. Users can provide the <see cref="StatusPageUrlPath" /> if the status page resides
    /// in the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full URL. If the full
    /// URL is provided, it takes precedence. It is normally used for informational purposes for other services to find out about the status of this
    /// instance. Users can provide a simple HTML page indicating what the current status of the instance is. The full URL should follow the format:
    /// http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime. Configuration property: eureka:instance:statusPageUrl.
    /// </summary>
    public string? StatusPageUrl { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the home page URL for this instance. The home page URL is then constructed out of the <see cref="HostName" /> and
    /// the type of communication - secure or non-secure, as specified in <see cref="SecurePort" /> and <see cref="NonSecurePort" />. It is normally used for
    /// informational purposes for other services to use it as a landing page. Configuration property: eureka:instance:homePageUrlPath.
    /// </summary>
    public string? HomePageUrlPath { get; set; } = DefaultHomePageUrlPath;

    /// <summary>
    /// Gets or sets the absolute URL to the home page for this instance. Users can provide the <see cref="HomePageUrlPath" /> if the home page resides in
    /// the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full URL. If the full
    /// URL is provided, it takes precedence. It is normally used for informational purposes for other services to find out about the status of this
    /// instance. Users can provide a simple HTML page indicating what the current status of the instance is. The full URL should follow the format:
    /// http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime. Configuration property: eureka:instance:homePageUrl.
    /// </summary>
    public string? HomePageUrl { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the health check endpoint for this instance. The health check URL is then constructed out of the
    /// <see cref="HostName" /> and the type of communication - secure or non-secure, as specified in <see cref="SecurePort" /> and
    /// <see cref="NonSecurePort" />. It is normally used for making educated decisions based on the health of the instance. For example, it can be used to
    /// determine whether to proceed deployments to an entire farm or stop the deployments without causing further damage. Configuration property:
    /// eureka:instance:healthCheckUrlPath.
    /// </summary>
    public string? HealthCheckUrlPath { get; set; } = DefaultHealthCheckUrlPath;

    /// <summary>
    /// Gets or sets the absolute URL for health checks of this instance. Users can provide the <see cref="HealthCheckUrlPath" /> if the health check
    /// endpoint resides in the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full
    /// URL. If the full URL is provided, it takes precedence. It is normally used for making educated decisions based on the health of the instance. For
    /// example, it can be used to determine whether to proceed deployments to an entire farm or stop the deployments without causing further damage. The
    /// full URL should follow the format: http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime. Configuration property:
    /// eureka:instance:healthCheckUrl.
    /// </summary>
    public string? HealthCheckUrl { get; set; }

    /// <summary>
    /// Gets or sets the secure absolute URL for health checks of this instance. Users can provide the <see cref="HealthCheckUrlPath" /> if the health check
    /// endpoint resides in the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full
    /// URL. If the full URL is provided, it takes precedence. It is normally used for making educated decisions based on the health of the instance. For
    /// example, it can be used to determine whether to proceed deployments to an entire farm or stop the deployments without causing further damage. The
    /// full URL should follow the format: https://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime. Configuration
    /// property: eureka:instance:secureHealthCheckUrl.
    /// </summary>
    public string? SecureHealthCheckUrl { get; set; }

    /// <summary>
    /// Gets or sets the AWS auto-scaling group name associated with this instance. This information is specifically used in an AWS environment to
    /// automatically put an instance out of service after the instance is launched, and it has been disabled for traffic. Configuration property:
    /// eureka:instance:asgName.
    /// </summary>
    [ConfigurationKeyName("AsgName")]
    public string? AutoScalingGroupName { get; set; }

    /// <summary>
    /// Gets the data center this instance is deployed to. This information is used to get some AWS-specific instance information if the instance is deployed
    /// in AWS.
    /// </summary>
    public DataCenterInfo DataCenterInfo { get; } = new()
    {
        Name = DataCenterName.MyOwn
    };

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="NetworkInterface.GetAllNetworkInterfaces" /> is used to determine <see cref="IPAddress" /> and
    /// <see cref="HostName" /> .
    /// </summary>
    public bool UseNetworkInterfaces { get; set; }

    internal bool IsGoRouterMethod()
    {
        // Use the gorouter in CloudFoundry.
        return (RegistrationMethod == null && Platform.IsCloudFoundry) || string.Equals(RegistrationMethod, "route", StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsContainerToContainerMethod()
    {
        // Use container-to-container networking on Cloud Foundry.
        return string.Equals(RegistrationMethod, "direct", StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsForceHostNameMethod()
    {
        // Use the explicitly configured host name (or IP address when PreferIPAddress is set to true).
        return string.Equals(RegistrationMethod, "hostname", StringComparison.OrdinalIgnoreCase);
    }

    internal void SetPortsFromListenAddresses(IEnumerable<string> listenOnAddresses, string source, ILogger<EurekaInstanceOptions> logger)
    {
        if (UseAspNetCoreUrls)
        {
            int? listenHttpPort = null;
            int? listenHttpsPort = null;

            foreach (string address in listenOnAddresses)
            {
                BindingAddress bindingAddress = BindingAddress.Parse(address);

                if (bindingAddress is { Scheme: "http", Port: > 0 } && listenHttpPort == null)
                {
                    listenHttpPort = bindingAddress.Port;
                }
                else if (bindingAddress is { Scheme: "https", Port: > 0 } && listenHttpsPort == null)
                {
                    listenHttpsPort = bindingAddress.Port;
                }
            }

            int? nonSecurePort = IsNonSecurePortEnabled ? NonSecurePort : null;
            int? securePort = IsSecurePortEnabled ? SecurePort : null;

            if (nonSecurePort != listenHttpPort)
            {
                if (listenHttpPort != null)
                {
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (nonSecurePort == null)
                    {
                        logger.LogDebug("Activating non-secure port {NonSecurePort} from {Source}.", listenHttpPort, source);
                    }
                    else
                    {
                        logger.LogDebug("Changing non-secure port to {NonSecurePort} from {Source}.", listenHttpPort, source);
                    }

                    NonSecurePort = listenHttpPort.Value;
                    IsNonSecurePortEnabled = true;
                }
                else if (nonSecurePort != null)
                {
                    logger.LogDebug("Deactivating non-secure port from {Source}.", source);
                    IsNonSecurePortEnabled = false;
                }
            }

            if (securePort != listenHttpsPort)
            {
                if (listenHttpsPort != null)
                {
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (securePort == null)
                    {
                        logger.LogDebug("Activating secure port {SecurePort} from {Source}.", listenHttpsPort, source);
                    }
                    else
                    {
                        logger.LogDebug("Changing secure port to {SecurePort} from {Source}.", listenHttpsPort, source);
                    }

                    SecurePort = listenHttpsPort.Value;
                    IsSecurePortEnabled = true;
                }
                else if (securePort != null)
                {
                    logger.LogDebug("Deactivating secure port from {Source}.", source);
                    IsSecurePortEnabled = false;
                }
            }
        }
    }
}
