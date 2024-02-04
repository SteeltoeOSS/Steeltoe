// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        if (!uri.EndsWith(EurekaUriSuffix, StringComparison.Ordinal))
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
    /// <param name="configuration">
    /// Application Configuration.
    /// </param>
    /// <param name="options">
    /// Eureka instance information (for identifying the application).
    /// </param>
    /// <param name="instanceInfo">
    /// Information about this application instance.
    /// </param>
#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high
    public static void UpdateConfiguration(IConfiguration configuration, EurekaInstanceOptions options, IApplicationInstanceInfo instanceInfo)
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high
    {
        string defaultIdEnding = $":{EurekaInstanceConfiguration.DefaultAppName}:{EurekaInstanceConfiguration.DefaultNonSecurePort}";

        if (options.AppName == EurekaInstanceConfiguration.DefaultAppName)
        {
            string springAppName = instanceInfo?.GetApplicationNameInContext(SteeltoeComponent.Discovery);

            // this is a bit of a hack, but depending on how we got here, GetApplicationNameInContext may or may not know about VCAP
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
            string springRegMethod = configuration.GetValue<string>(SpringCloudDiscoveryRegistrationMethodKey);

            if (!string.IsNullOrEmpty(springRegMethod))
            {
                options.RegistrationMethod = springRegMethod;
            }
        }

        options.ApplyConfigUrls(configuration.GetAspNetCoreUrls());

        if (options.InstanceId.EndsWith(defaultIdEnding, StringComparison.Ordinal))
        {
            string springInstanceId = instanceInfo?.InstanceId;

            if (!string.IsNullOrEmpty(springInstanceId))
            {
                options.InstanceId = springInstanceId;
            }
            else
            {
                options.InstanceId = options.SecurePortEnabled
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
    /// <param name="si">
    /// <see cref="EurekaServiceInfo" /> for bound Eureka server(s).
    /// </param>
    /// <param name="instOptions">
    /// Eureka instance information (for identifying the application).
    /// </param>
    /// <param name="appInfo">
    /// Information about this application instance.
    /// </param>
    public static void UpdateConfiguration(IConfiguration configuration, EurekaServiceInfo si, EurekaInstanceOptions instOptions,
        IApplicationInstanceInfo appInfo)
    {
        if (instOptions == null)
        {
            return;
        }

        UpdateConfiguration(configuration, instOptions, appInfo);

        if (si == null)
        {
            return;
        }

        if (instOptions.AppName == EurekaInstanceConfiguration.DefaultAppName)
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

        if (!clientOptions.EurekaServerServiceUrls.Contains(EurekaClientConfiguration.DefaultServerServiceUrl.TrimEnd('/'), StringComparison.Ordinal))
        {
            return;
        }

        if (!clientOptions.ShouldRegisterWithEureka && !clientOptions.ShouldFetchRegistry)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Eureka URL {EurekaClientConfiguration.DefaultServerServiceUrl} is not valid in containerized or cloud environments. Please configure Eureka:Client:ServiceUrl with a non-localhost address or add a service binding.");
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
        instOptions.PreferIPAddress = true;
        instOptions.NonSecurePort = si.ApplicationInfo.Port;
        instOptions.SecurePort = si.ApplicationInfo.Port;
        instOptions.InstanceId = $"{si.ApplicationInfo.InternalIP}:{si.ApplicationInfo.InstanceId}";
    }

    private static void UpdateWithDefaultsForRoute(EurekaServiceInfo si, EurekaInstanceOptions instOptions)
    {
        UpdateWithDefaults(si, instOptions);
        instOptions.NonSecurePort = EurekaInstanceConfiguration.DefaultNonSecurePort;
        instOptions.SecurePort = EurekaInstanceConfiguration.DefaultSecurePort;

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

        instOptions.IPAddress = si.ApplicationInfo.InternalIP;

        IDictionary<string, string> map = instOptions.MetadataMap;
        map[CFAppGuid] = si.ApplicationInfo.ApplicationId;
        map[CFInstanceIndex] = si.ApplicationInfo.InstanceIndex.ToString(CultureInfo.InvariantCulture);
        map[InstanceId] = si.ApplicationInfo.InstanceId;
        map[Zone] = UnknownZone;
    }
}
