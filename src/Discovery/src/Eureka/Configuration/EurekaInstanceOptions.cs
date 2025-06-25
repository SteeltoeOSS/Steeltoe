// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;

// Workaround for Sonar bug, which incorrectly flags boolean expression as redundant.
#pragma warning disable S2589 // Boolean expressions should not be gratuitous

namespace Steeltoe.Discovery.Eureka.Configuration;

public sealed class EurekaInstanceOptions
{
    internal const string ConfigurationPrefix = "eureka:instance";
    internal const string DefaultStatusPageUrlPath = "/info";
    internal const string DefaultHealthCheckUrlPath = "/health";

    private bool UseAspNetCoreUrls => !Platform.IsCloudFoundry || IsContainerToContainerMethod() || IsForceHostNameMethod();

    internal TimeSpan LeaseRenewalInterval => TimeSpan.FromSeconds(LeaseRenewalIntervalInSeconds);
    internal TimeSpan LeaseExpirationDuration => TimeSpan.FromSeconds(LeaseExpirationDurationInSeconds);

    /// <summary>
    /// Gets or sets the unique ID (within the scope of the app name) of the instance to be registered with Eureka.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the name of the application to be registered with Eureka.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the name of the application group to be registered with Eureka.
    /// </summary>
    [ConfigurationKeyName("AppGroup")]
    public string? AppGroupName { get; set; }

    /// <summary>
    /// Gets the name/value pairs associated with the instance. This information is sent to Eureka server and can be used by other instances.
    /// </summary>
    public IDictionary<string, string?> MetadataMap { get; } = new Dictionary<string, string?>();

    /// <summary>
    /// Gets or sets the hostname on which the instance is registered.
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Gets or sets the IP address on which the instance is registered.
    /// </summary>
    public string? IPAddress { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to register with <see cref="IPAddress" /> instead of <see cref="HostName" />. Default value: false.
    /// </summary>
    public bool PreferIPAddress { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of VIP (Virtual Internet Protocol) addresses for the instance.
    /// </summary>
    /// <remarks>
    /// When using service discovery, VIP addresses are resolved into real addresses on outgoing HTTP requests.
    /// </remarks>
    public string? VipAddress { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of secure VIP (Virtual Internet Protocol) addresses for the instance.
    /// </summary>
    /// <remarks>
    /// When using service discovery, secure VIP addresses are resolved into real addresses on outgoing HTTP requests.
    /// </remarks>
    public string? SecureVipAddress { get; set; }

    /// <summary>
    /// Gets or sets the non-secure port number on which the instance should receive traffic.
    /// </summary>
    [ConfigurationKeyName("Port")]
    public int? NonSecurePort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the non-secure port should be enabled.
    /// </summary>
    [ConfigurationKeyName("NonSecurePortEnabled")]
    public bool IsNonSecurePortEnabled { get; set; }

    /// <summary>
    /// Gets or sets the secure port on which the instance should receive traffic.
    /// </summary>
    public int? SecurePort { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the secure port should be enabled.
    /// </summary>
    [ConfigurationKeyName("SecurePortEnabled")]
    public bool IsSecurePortEnabled { get; set; }

    /// <summary>
    /// Gets or sets how to register on Cloud Foundry. Can be "route", "direct", or "hostname".
    /// </summary>
    public string? RegistrationMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the instance should take traffic as soon as it is registered. Default value: true.
    /// </summary>
    /// <remarks>
    /// When set to <c>false</c>, call <see cref="EurekaApplicationInfoManager.UpdateInstance" /> after initialization to mark the instance as up.
    /// </remarks>
    [ConfigurationKeyName("InstanceEnabledOnInit")]
    public bool IsInstanceEnabledOnInit { get; set; } = true;

    /// <summary>
    /// Gets or sets how often (in seconds) the client sends heartbeats to Eureka to indicate that it is still alive. If the heartbeats are not received for
    /// the period specified in <see cref="LeaseExpirationDurationInSeconds" />, Eureka server will remove the instance from its view, thereby disallowing
    /// traffic to the instance. Note that the instance could still not take traffic if <see cref="EurekaHealthOptions.CheckEnabled" /> is <c>true</c>, which
    /// decides to make itself unavailable. Default value: 30.
    /// </summary>
    public int LeaseRenewalIntervalInSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the time (in seconds) that the Eureka server waits since it received the last heartbeat before it marks the instance as down and thereby
    /// disallowing traffic to the instance. Setting this value too high could mean that traffic is routed to the instance even when the instance is not
    /// alive anymore. Setting this value too low could mean the instance may be taken out of traffic because of temporary network glitches. This value must
    /// be higher than <see cref="LeaseRenewalIntervalInSeconds" />. Default value: 90.
    /// </summary>
    public int LeaseExpirationDurationInSeconds { get; set; } = 90;

    /// <summary>
    /// Gets or sets the relative path to the status page for the instance. The status page URL is then constructed out of the <see cref="HostName" /> and
    /// the type of communication - secure or non-secure, as specified in <see cref="SecurePort" /> and <see cref="NonSecurePort" />. It is normally used for
    /// informational purposes for other services to find out about the status of the instance. Users can provide a simple HTML page indicating what the
    /// current status of the instance is. Default value: /info.
    /// </summary>
    public string? StatusPageUrlPath { get; set; } = DefaultStatusPageUrlPath;

    /// <summary>
    /// Gets or sets the absolute URL to the status page for the instance. Users can provide the <see cref="StatusPageUrlPath" /> if the status page resides
    /// in the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full URL. If the full
    /// URL is provided, it takes precedence. It is normally used for informational purposes for other services to find out about the status of the instance.
    /// Users can provide a simple HTML page indicating what the current status of the instance is. The full URL should follow the format:
    /// http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime.
    /// </summary>
    public string? StatusPageUrl { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the home page URL for the instance. The home page URL is then constructed out of the <see cref="HostName" /> and
    /// the type of communication - secure or non-secure, as specified in <see cref="SecurePort" /> and <see cref="NonSecurePort" />. It is normally used for
    /// informational purposes for other services to use it as a landing page. Default value: /.
    /// </summary>
    public string? HomePageUrlPath { get; set; } = "/";

    /// <summary>
    /// Gets or sets the absolute URL to the home page for the instance. Users can provide the <see cref="HomePageUrlPath" /> if the home page resides in the
    /// same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full URL. If the full URL is
    /// provided, it takes precedence. It is normally used for informational purposes for other services to find out about the status of the instance. Users
    /// can provide a simple HTML page indicating what the current status of the instance is. The full URL should follow the format:
    /// http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime.
    /// </summary>
    public string? HomePageUrl { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the health check endpoint of the instance. The health check URL is then constructed out of the
    /// <see cref="HostName" /> and the type of communication - secure or non-secure, as specified in <see cref="SecurePort" /> and
    /// <see cref="NonSecurePort" />. It is normally used for making educated decisions based on the health of the instance. For example, it can be used to
    /// determine whether to proceed deployments to an entire farm or stop the deployments without causing further damage. Default value: /health.
    /// </summary>
    public string? HealthCheckUrlPath { get; set; } = DefaultHealthCheckUrlPath;

    /// <summary>
    /// Gets or sets the absolute URL for health checks of the instance. Users can provide the <see cref="HealthCheckUrlPath" /> if the health check endpoint
    /// resides in the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full URL. If
    /// the full URL is provided, it takes precedence. It is normally used for making educated decisions based on the health of the instance. For example, it
    /// can be used to determine whether to proceed deployments to an entire farm or stop the deployments without causing further damage. The full URL should
    /// follow the format: http://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime.
    /// </summary>
    public string? HealthCheckUrl { get; set; }

    /// <summary>
    /// Gets or sets the secure absolute URL for health checks of the instance. Users can provide the <see cref="HealthCheckUrlPath" /> if the health check
    /// endpoint resides in the same instance talking to Eureka. Otherwise, in case the instance is a proxy for some other server, users can provide the full
    /// URL. If the full URL is provided, it takes precedence. It is normally used for making educated decisions based on the health of the instance. For
    /// example, it can be used to determine whether to proceed deployments to an entire farm or stop the deployments without causing further damage. The
    /// full URL should follow the format: https://${eureka.hostname}:7001/ where the value ${eureka.hostname} is replaced at runtime.
    /// </summary>
    public string? SecureHealthCheckUrl { get; set; }

    /// <summary>
    /// Gets or sets the AWS auto-scaling group name associated with the instance. This information is specifically used in an AWS environment to
    /// automatically put an instance out of service after the instance is launched, and it has been disabled for traffic.
    /// </summary>
    [ConfigurationKeyName("AsgName")]
    public string? AutoScalingGroupName { get; set; }

    /// <summary>
    /// Gets the data center the instance is deployed to. This information is used to get some AWS-specific instance information if the instance is deployed
    /// in AWS.
    /// </summary>
    public DataCenterInfo DataCenterInfo { get; } = new()
    {
        Name = DataCenterName.MyOwn
    };

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="NetworkInterface.GetAllNetworkInterfaces" /> is used to determine <see cref="IPAddress" /> and
    /// <see cref="HostName" />. Default value: false.
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
        // Use the explicitly configured hostname (or IP address when PreferIPAddress is set to true).
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
