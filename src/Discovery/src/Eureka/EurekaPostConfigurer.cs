// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connector.Services;
using Steeltoe.Discovery.Client;

namespace Steeltoe.Discovery.Eureka;

public static class EurekaPostConfigurer
{
    internal const string EurekaUriSuffix = "/eureka/";

    internal const string RouteRegistrationMethod = "route";
    internal const string DirectRegistrationMethod = "direct";
    internal const string HostRegistrationMethod = "hostname";

    internal const string CFAppGuid = "cfAppGuid";
    internal const string CFInstanceIndex = "cfInstanceIndex";
    internal const string SurgicalRoutingHeader = "X-CF-APP-INSTANCE";
    internal const string InstanceId = "instanceId";
    internal const string Zone = "zone";
    internal const string UnknownZone = "unknown";
    public const string SpringCloudDiscoveryRegistrationMethodKey = "spring:cloud:discovery:registrationMethod";

    /// <summary>
    /// Update <see cref="EurekaClientOptions" /> with information from the runtime environment.
    /// </summary>
    /// <param name="si">
    /// <see cref="EurekaServiceInfo" /> for bound Eureka server(s).
    /// </param>
    /// <param name="clientOptions">
    /// Eureka client configuration (for interacting with the Eureka Server).
    /// </param>
    public static void UpdateConfiguration(EurekaServiceInfo si, EurekaClientOptions clientOptions)
    {
        EurekaClientOptions clientOpts = clientOptions ?? new EurekaClientOptions();

        AssertValid(si, clientOpts);

        if (clientOptions == null || si == null)
        {
            return;
        }

        string uri = si.Uri;

        if (!uri.EndsWith(EurekaUriSuffix))
        {
            uri += EurekaUriSuffix;
        }

        clientOptions.EurekaServerServiceUrls = uri;
        clientOptions.AccessTokenUri = si.TokenUri;
        clientOptions.ClientId = si.ClientId;
        clientOptions.ClientSecret = si.ClientSecret;
    }

    /// <summary>
    /// Update <see cref="EurekaInstanceOptions" /> with information from the runtime environment.
    /// </summary>
    /// <param name="config">
    /// Application Configuration.
    /// </param>
    /// <param name="options">
    /// Eureka instance information (for identifying the application).
    /// </param>
    /// <param name="instanceInfo">
    /// Information about this application instance.
    /// </param>
    public static void UpdateConfiguration(IConfiguration config, EurekaInstanceOptions options, IApplicationInstanceInfo instanceInfo)
    {
        string defaultIdEnding = $":{EurekaInstanceConfig.DefaultAppName}:{EurekaInstanceConfig.DefaultNonSecurePort}";

        if (EurekaInstanceConfig.DefaultAppName.Equals(options.AppName))
        {
            string springAppName = instanceInfo?.ApplicationNameInContext(SteeltoeComponent.Discovery);

            // this is a bit of a hack, but depending on how we got here, ApplicationNameInContext may or may not know about VCAP
            if (Platform.IsCloudFoundry && springAppName == Assembly.GetEntryAssembly().GetName().Name && !string.IsNullOrEmpty(instanceInfo?.ApplicationName))
            {
                options.AppName = instanceInfo.ApplicationName;
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
            string springRegMethod = config.GetValue<string>(SpringCloudDiscoveryRegistrationMethodKey);

            if (!string.IsNullOrEmpty(springRegMethod))
            {
                options.RegistrationMethod = springRegMethod;
            }
        }

        options.ApplyConfigUrls(config.GetAspNetCoreUrls(), ConfigurationUrlHelpers.WildcardHost);

        if (options.InstanceId.EndsWith(defaultIdEnding))
        {
            string springInstanceId = instanceInfo?.InstanceId;

            if (!string.IsNullOrEmpty(springInstanceId))
            {
                options.InstanceId = springInstanceId;
            }
            else
            {
                options.InstanceId = options.SecurePortEnabled
                    ? $"{options.GetHostName(false)}:{options.AppName}:{options.SecurePort}"
                    : $"{options.GetHostName(false)}:{options.AppName}:{options.NonSecurePort}";
            }
        }
    }

    /// <summary>
    /// Update <see cref="EurekaInstanceOptions" /> with information from the runtime environment.
    /// </summary>
    /// <param name="config">
    /// Application Configuration.
    /// </param>
    /// <param name="si">
    /// <see cref="EurekaServiceInfo" /> for bound Eureka server(s).
    /// </param>
    /// <param name="instOptions">
    /// Eureka instance information (for identifying the application).
    /// </param>
    /// <param name="appInfo">
    /// Information about this application instance.
    /// </param>
    public static void UpdateConfiguration(IConfiguration config, EurekaServiceInfo si, EurekaInstanceOptions instOptions, IApplicationInstanceInfo appInfo)
    {
        if (instOptions == null)
        {
            return;
        }

        UpdateConfiguration(config, instOptions, appInfo);

        if (si == null)
        {
            return;
        }

        if (EurekaInstanceConfig.DefaultAppName.Equals(instOptions.AppName))
        {
            instOptions.AppName = si.ApplicationInfo.ApplicationName;
        }

        if (string.IsNullOrEmpty(instOptions.RegistrationMethod) ||
            RouteRegistrationMethod.Equals(instOptions.RegistrationMethod, StringComparison.OrdinalIgnoreCase))
        {
            UpdateWithDefaultsForRoute(si, instOptions);
            return;
        }

        if (DirectRegistrationMethod.Equals(instOptions.RegistrationMethod, StringComparison.OrdinalIgnoreCase))
        {
            UpdateWithDefaultsForDirect(si, instOptions);
            return;
        }

        if (HostRegistrationMethod.Equals(instOptions.RegistrationMethod, StringComparison.OrdinalIgnoreCase))
        {
            UpdateWithDefaultsForHost(si, instOptions, instOptions.HostName);
        }
    }

    private static void AssertValid(EurekaServiceInfo serviceInfo, EurekaClientOptions clientOptions)
    {
        if (!clientOptions.Enabled)
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

        if (!clientOptions.EurekaServerServiceUrls.Contains(EurekaClientConfig.DefaultServerServiceUrl.TrimEnd('/')))
        {
            return;
        }

        if (!clientOptions.ShouldRegisterWithEureka && !clientOptions.ShouldFetchRegistry)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Eureka URL {EurekaClientConfig.DefaultServerServiceUrl} is not valid in containerized or cloud environments. Please configure Eureka:Client:ServiceUrl with a non-localhost address or add a service binding.");
    }

    private static void UpdateWithDefaultsForHost(EurekaServiceInfo si, EurekaInstanceOptions instOptions, string hostName)
    {
        UpdateWithDefaults(si, instOptions);
        instOptions.HostName = hostName;
        instOptions.InstanceId = $"{hostName}:{si.ApplicationInfo.InstanceId}";
    }

    private static void UpdateWithDefaultsForDirect(EurekaServiceInfo si, EurekaInstanceOptions instOptions)
    {
        UpdateWithDefaults(si, instOptions);
        instOptions.PreferIpAddress = true;
        instOptions.NonSecurePort = si.ApplicationInfo.Port;
        instOptions.SecurePort = si.ApplicationInfo.Port;
        instOptions.InstanceId = $"{si.ApplicationInfo.InternalIp}:{si.ApplicationInfo.InstanceId}";
    }

    private static void UpdateWithDefaultsForRoute(EurekaServiceInfo si, EurekaInstanceOptions instOptions)
    {
        UpdateWithDefaults(si, instOptions);
        instOptions.NonSecurePort = EurekaInstanceConfig.DefaultNonSecurePort;
        instOptions.SecurePort = EurekaInstanceConfig.DefaultSecurePort;

        if (si.ApplicationInfo.Uris?.Any() == true)
        {
            instOptions.InstanceId = $"{si.ApplicationInfo.Uris.First()}:{si.ApplicationInfo.InstanceId}";
        }
    }

    private static void UpdateWithDefaults(EurekaServiceInfo si, EurekaInstanceOptions instOptions)
    {
        if (si.ApplicationInfo.Uris != null && si.ApplicationInfo.Uris.Any())
        {
            instOptions.HostName = si.ApplicationInfo.Uris.First();
        }

        instOptions.IpAddress = si.ApplicationInfo.InternalIp;

        IDictionary<string, string> map = instOptions.MetadataMap;
        map[CFAppGuid] = si.ApplicationInfo.ApplicationId;
        map[CFInstanceIndex] = si.ApplicationInfo.InstanceIndex.ToString();
        map[InstanceId] = si.ApplicationInfo.InstanceId;
        map[Zone] = UnknownZone;
    }
}
