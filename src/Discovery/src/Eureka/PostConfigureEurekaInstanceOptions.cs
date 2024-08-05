// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Discovery.Eureka;

internal sealed class PostConfigureEurekaInstanceOptions : IPostConfigureOptions<EurekaInstanceOptions>
{
    private const string SpringCloudDiscoveryRegistrationMethodKey = "spring:cloud:discovery:registrationMethod";

    private static readonly AssemblyLoader AssemblyLoader = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IApplicationInstanceInfo _appInfo;
    private readonly InetUtils _inetUtils;

    public PostConfigureEurekaInstanceOptions(IServiceProvider serviceProvider, IConfiguration configuration, IApplicationInstanceInfo appInfo,
        InetUtils inetUtils)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(appInfo);
        ArgumentNullException.ThrowIfNull(inetUtils);

        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _appInfo = appInfo;
        _inetUtils = inetUtils;
    }

    public void PostConfigure(string? name, EurekaInstanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        SetRegistrationMethod(options);
        SetHostNameAndIpAddress(options);
        SetPorts(options);
        SetAppName(options);
        SetVipAddresses(options);
        SetInstanceId(options);
        SetMetadata(options);

        if (AssemblyLoader.IsAssemblyLoaded("Steeltoe.Management.Endpoint"))
        {
            SetPathsFromEndpointOptions(options);
        }
    }

    private void SetRegistrationMethod(EurekaInstanceOptions options)
    {
        options.RegistrationMethod ??= _configuration.GetValue<string?>(SpringCloudDiscoveryRegistrationMethodKey);
    }

    private void SetHostNameAndIpAddress(EurekaInstanceOptions options)
    {
        if (!options.IsForceHostNameMethod())
        {
            string? firstAppUri = _appInfo.Uris?.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstAppUri))
            {
                options.HostName = firstAppUri;
            }
        }

        HostInfo? hostInfo = options.UseNetworkInterfaces ? _inetUtils.FindFirstNonLoopbackHostInfo() : null;
        options.HostName ??= hostInfo != null ? hostInfo.Hostname : DnsTools.ResolveHostName();

        if (!string.IsNullOrWhiteSpace(_appInfo.InternalIP))
        {
            options.IPAddress = _appInfo.InternalIP;
        }

        if (string.IsNullOrWhiteSpace(options.IPAddress))
        {
            if (hostInfo != null && !string.IsNullOrWhiteSpace(hostInfo.IPAddress))
            {
                options.IPAddress = hostInfo.IPAddress;
            }
            else if (!string.IsNullOrWhiteSpace(options.HostName))
            {
                options.IPAddress = DnsTools.ResolveHostAddress(options.HostName);
            }
        }

        if (options.IsContainerToContainerMethod())
        {
            options.PreferIPAddress = true;
        }

        if (options.PreferIPAddress && !string.IsNullOrWhiteSpace(options.IPAddress))
        {
            options.HostName = options.IPAddress;
        }
    }

    private void SetPorts(EurekaInstanceOptions options)
    {
        if (options.IsGoRouterMethod())
        {
            options.NonSecurePort = 80;
            options.IsNonSecurePortEnabled = true;
            options.SecurePort = 443;
            options.IsSecurePortEnabled = true;
        }

        if (options.NonSecurePort == null && options.SecurePort == null)
        {
            var optionsLogger = _serviceProvider.GetRequiredService<ILogger<EurekaInstanceOptions>>();
            ICollection<string> addresses = _configuration.GetListenAddresses();
            options.SetPortsFromListenAddresses(addresses, "binding probe", optionsLogger);
        }
    }

    private void SetAppName(EurekaInstanceOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AppName))
        {
            string? springAppName = _appInfo.GetApplicationNameInContext(SteeltoeComponent.Discovery);
            options.AppName = !string.IsNullOrWhiteSpace(springAppName) ? springAppName : "UNKNOWN";
        }
    }

    private void SetVipAddresses(EurekaInstanceOptions options)
    {
        options.VipAddress ??= options.AppName;
        options.SecureVipAddress ??= options.AppName;
    }

    private void SetInstanceId(EurekaInstanceOptions options)
    {
        if (options.IsGoRouterMethod())
        {
            string? firstAppUri = _appInfo.Uris?.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstAppUri))
            {
                options.InstanceId = $"{firstAppUri}:{_appInfo.InstanceId}";
            }
        }
        else if (options.IsContainerToContainerMethod())
        {
            options.InstanceId = $"{_appInfo.InternalIP}:{_appInfo.InstanceId}";
        }
        else if (options.IsForceHostNameMethod())
        {
            options.InstanceId = $"{options.HostName}:{_appInfo.InstanceId}";
        }

        options.InstanceId ??= _appInfo.InstanceId;

        if (string.IsNullOrWhiteSpace(options.InstanceId))
        {
            int? portNumber = null;

            if (options.IsSecurePortEnabled)
            {
                portNumber = options.SecurePort;
            }
            else if (options.IsNonSecurePortEnabled)
            {
                portNumber = options.NonSecurePort;
            }

            if (portNumber is null or <= 0)
            {
                // The port number is dynamically assigned by ASP.NET once the app has fully started.
                // Pick a random number (outside the valid range) to prevent registering duplicate instances in Eureka.
                portNumber = Random.Shared.Next(90_000, 99_999);
            }

            options.InstanceId = $"{options.HostName}:{options.AppName}:{portNumber}";
        }
    }

    private void SetMetadata(EurekaInstanceOptions options)
    {
        if (Platform.IsCloudFoundry)
        {
            options.MetadataMap["cfAppGuid"] = _appInfo.ApplicationId;
            options.MetadataMap["cfInstanceIndex"] = _appInfo.InstanceIndex.ToString(CultureInfo.InvariantCulture);
            options.MetadataMap["instanceId"] = _appInfo.InstanceId;
            options.MetadataMap["zone"] = "unknown";
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SetPathsFromEndpointOptions(EurekaInstanceOptions instanceOptions)
    {
        var managementOptionsMonitor = _serviceProvider.GetRequiredService<IOptionsMonitor<ManagementOptions>>();
        string basePath = $"{managementOptionsMonitor.CurrentValue.Path}/";

        if (instanceOptions is { HealthCheckUrlPath: EurekaInstanceOptions.DefaultHealthCheckUrlPath, HealthCheckUrl: null })
        {
            var healthEndpointOptionsMonitor = _serviceProvider.GetRequiredService<IOptionsMonitor<HealthEndpointOptions>>();
            HealthEndpointOptions healthEndpointOptions = healthEndpointOptionsMonitor.CurrentValue;

            if (!string.IsNullOrEmpty(healthEndpointOptions.Path))
            {
                instanceOptions.HealthCheckUrlPath = $"{basePath}{healthEndpointOptions.Path?.TrimStart('/')}";
            }
        }

        if (instanceOptions is { StatusPageUrlPath: EurekaInstanceOptions.DefaultStatusPageUrlPath, StatusPageUrl: null })
        {
            var infoEndpointOptionsMonitor = _serviceProvider.GetRequiredService<IOptionsMonitor<InfoEndpointOptions>>();
            InfoEndpointOptions infoEndpointOptions = infoEndpointOptionsMonitor.CurrentValue;

            if (!string.IsNullOrEmpty(infoEndpointOptions.Path))
            {
                instanceOptions.StatusPageUrlPath = $"{basePath}{infoEndpointOptions.Path?.TrimStart('/')}";
            }
        }
    }
}
