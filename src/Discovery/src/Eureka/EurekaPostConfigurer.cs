// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Discovery.Eureka;

internal static class EurekaPostConfigurer
{
    private const string EurekaUriSuffix = "/eureka/";

    private const string RouteRegistrationMethod = "route";
    private const string DirectRegistrationMethod = "direct";
    private const string HostRegistrationMethod = "hostname";
    private const string SpringCloudDiscoveryRegistrationMethodKey = "spring:cloud:discovery:registrationMethod";

    internal const string CFAppGuid = "cfAppGuid";
    internal const string CFInstanceIndex = "cfInstanceIndex";
    internal const string InstanceId = "instanceId";
    internal const string Zone = "zone";
    internal const string UnknownZone = "unknown";

    /// <summary>
    /// Update <see cref="EurekaClientOptions" /> with information from the runtime environment.
    /// </summary>
    /// <param name="serviceInfo">
    /// <see cref="EurekaServiceInfo" /> for bound Eureka server(s).
    /// </param>
    /// <param name="clientOptions">
    /// Eureka client configuration (for interacting with the Eureka Server).
    /// </param>
    public static void UpdateConfiguration(EurekaServiceInfo? serviceInfo, EurekaClientOptions? clientOptions)
    {
        AssertValid(serviceInfo, clientOptions);

        if (clientOptions == null || serviceInfo == null)
        {
            return;
        }

        string uri = serviceInfo.Uri;

        if (!uri.EndsWith(EurekaUriSuffix, StringComparison.Ordinal))
        {
            uri += EurekaUriSuffix;
        }

        clientOptions.EurekaServerServiceUrls = uri;
        clientOptions.AccessTokenUri = serviceInfo.TokenUri;
        clientOptions.ClientId = serviceInfo.ClientId;
        clientOptions.ClientSecret = serviceInfo.ClientSecret;
    }

    /// <summary>
    /// Update <see cref="EurekaInstanceOptions" /> with information from the runtime environment.
    /// </summary>
    /// <param name="configuration">
    /// Application Configuration.
    /// </param>
    /// <param name="options">
    /// Eureka instance information (for identifying the application).
    /// </param>
    /// <param name="appInfo">
    /// Information about this application instance.
    /// </param>
    public static void UpdateConfiguration(IConfiguration configuration, EurekaInstanceOptions options, IApplicationInstanceInfo? appInfo)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(options);

        string defaultIdEnding = $":{EurekaInstanceOptions.DefaultAppName}:{EurekaInstanceOptions.DefaultNonSecurePort}";

        if (options.AppName == EurekaInstanceOptions.DefaultAppName)
        {
            string? springAppName = appInfo?.GetApplicationNameInContext(SteeltoeComponent.Discovery);

            // this is a bit of a hack, but depending on how we got here, GetApplicationNameInContext may or may not know about VCAP
            if (Platform.IsCloudFoundry && springAppName == Assembly.GetEntryAssembly()!.GetName().Name && !string.IsNullOrEmpty(appInfo?.ApplicationName))
            {
                options.AppName = appInfo.ApplicationName;
            }
            else if (!string.IsNullOrEmpty(springAppName))
            {
                options.AppName = springAppName;
            }
        }

        if (string.IsNullOrEmpty(options.VirtualHostName))
        {
            options.VirtualHostName = options.AppName;
        }

        if (string.IsNullOrEmpty(options.SecureVirtualHostName))
        {
            options.SecureVirtualHostName = options.AppName;
        }

        if (string.IsNullOrEmpty(options.RegistrationMethod))
        {
            string? springRegistrationMethod = configuration.GetValue<string>(SpringCloudDiscoveryRegistrationMethodKey);

            if (!string.IsNullOrEmpty(springRegistrationMethod))
            {
                options.RegistrationMethod = springRegistrationMethod;
            }
        }

        options.ApplyConfigUrls(configuration.GetAspNetCoreUrls());

        if (options.InstanceId != null && options.InstanceId.EndsWith(defaultIdEnding, StringComparison.Ordinal))
        {
            string? springInstanceId = appInfo?.InstanceId;

            if (!string.IsNullOrEmpty(springInstanceId))
            {
                options.InstanceId = springInstanceId;
            }
            else
            {
                options.InstanceId = options.IsSecurePortEnabled
                    ? $"{options.ResolveHostName(false)}:{options.AppName}:{options.SecurePort}"
                    : $"{options.ResolveHostName(false)}:{options.AppName}:{options.NonSecurePort}";
            }
        }
    }

    /// <summary>
    /// Update <see cref="EurekaInstanceOptions" /> with information from the runtime environment.
    /// </summary>
    /// <param name="configuration">
    /// Application Configuration.
    /// </param>
    /// <param name="serviceInfo">
    /// <see cref="EurekaServiceInfo" /> for bound Eureka server(s).
    /// </param>
    /// <param name="instanceOptions">
    /// Eureka instance information (for identifying the application).
    /// </param>
    /// <param name="appInfo">
    /// Information about this application instance.
    /// </param>
    public static void UpdateConfiguration(IConfiguration configuration, EurekaServiceInfo? serviceInfo, EurekaInstanceOptions? instanceOptions,
        IApplicationInstanceInfo? appInfo)
    {
        ArgumentGuard.NotNull(configuration);

        if (instanceOptions == null)
        {
            return;
        }

        UpdateConfiguration(configuration, instanceOptions, appInfo);

        if (serviceInfo == null)
        {
            return;
        }

        if (instanceOptions.AppName == EurekaInstanceOptions.DefaultAppName)
        {
            instanceOptions.AppName = serviceInfo.ApplicationInfo.ApplicationName;
        }

        if (string.IsNullOrEmpty(instanceOptions.RegistrationMethod) ||
            RouteRegistrationMethod.Equals(instanceOptions.RegistrationMethod, StringComparison.OrdinalIgnoreCase))
        {
            UpdateWithDefaultsForRoute(serviceInfo, instanceOptions);
            return;
        }

        if (DirectRegistrationMethod.Equals(instanceOptions.RegistrationMethod, StringComparison.OrdinalIgnoreCase))
        {
            UpdateWithDefaultsForDirect(serviceInfo, instanceOptions);
            return;
        }

        if (HostRegistrationMethod.Equals(instanceOptions.RegistrationMethod, StringComparison.OrdinalIgnoreCase))
        {
            UpdateWithDefaultsForHost(serviceInfo, instanceOptions, instanceOptions.HostName);
        }
    }

    private static void AssertValid(EurekaServiceInfo? serviceInfo, EurekaClientOptions? clientOptions)
    {
        if (clientOptions is not { Enabled: true })
        {
            return;
        }

        if (!Platform.IsContainerized && !Platform.IsCloudHosted)
        {
            return;
        }

        if (serviceInfo != null)
        {
            return;
        }

        if (!clientOptions.EurekaServerServiceUrls.Contains(EurekaClientOptions.DefaultServerServiceUrl.TrimEnd('/'), StringComparison.Ordinal))
        {
            return;
        }

        if (clientOptions is { ShouldRegisterWithEureka: false, ShouldFetchRegistry: false })
        {
            return;
        }

        throw new InvalidOperationException(
            $"Eureka URL {EurekaClientOptions.DefaultServerServiceUrl} is not valid in containerized or cloud environments. Please configure Eureka:Client:ServiceUrl with a non-localhost address or add a service binding.");
    }

    private static void UpdateWithDefaultsForHost(EurekaServiceInfo serviceInfo, EurekaInstanceOptions instanceOptions, string? hostName)
    {
        UpdateWithDefaults(serviceInfo, instanceOptions);
        instanceOptions.HostName = hostName;
        instanceOptions.InstanceId = $"{hostName}:{serviceInfo.ApplicationInfo.InstanceId}";
    }

    private static void UpdateWithDefaultsForDirect(EurekaServiceInfo serviceInfo, EurekaInstanceOptions instanceOptions)
    {
        UpdateWithDefaults(serviceInfo, instanceOptions);
        instanceOptions.PreferIPAddress = true;
        instanceOptions.NonSecurePort = serviceInfo.ApplicationInfo.Port;
        instanceOptions.SecurePort = serviceInfo.ApplicationInfo.Port;
        instanceOptions.InstanceId = $"{serviceInfo.ApplicationInfo.InternalIP}:{serviceInfo.ApplicationInfo.InstanceId}";
    }

    private static void UpdateWithDefaultsForRoute(EurekaServiceInfo serviceInfo, EurekaInstanceOptions instanceOptions)
    {
        UpdateWithDefaults(serviceInfo, instanceOptions);
        instanceOptions.NonSecurePort = EurekaInstanceOptions.DefaultNonSecurePort;
        instanceOptions.SecurePort = EurekaInstanceOptions.DefaultSecurePort;

        if (serviceInfo.ApplicationInfo.Uris?.Any() == true)
        {
            instanceOptions.InstanceId = $"{serviceInfo.ApplicationInfo.Uris.First()}:{serviceInfo.ApplicationInfo.InstanceId}";
        }
    }

    private static void UpdateWithDefaults(EurekaServiceInfo serviceInfo, EurekaInstanceOptions instanceOptions)
    {
        if (serviceInfo.ApplicationInfo.Uris != null && serviceInfo.ApplicationInfo.Uris.Any())
        {
            instanceOptions.HostName = serviceInfo.ApplicationInfo.Uris.First();
        }

        instanceOptions.IPAddress = serviceInfo.ApplicationInfo.InternalIP;

        IDictionary<string, string> map = instanceOptions.MetadataMap;
        map[CFAppGuid] = serviceInfo.ApplicationInfo.ApplicationId;
        map[CFInstanceIndex] = serviceInfo.ApplicationInfo.InstanceIndex.ToString(CultureInfo.InvariantCulture);
        map[InstanceId] = serviceInfo.ApplicationInfo.InstanceId;
        map[Zone] = UnknownZone;
    }
}
