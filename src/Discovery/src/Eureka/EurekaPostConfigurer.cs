// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connector.Services;
using System;
using System.Linq;

namespace Steeltoe.Discovery.Eureka
{
    public static class EurekaPostConfigurer
    {
        public const string SPRING_CLOUD_DISCOVERY_REGISTRATIONMETHOD_KEY = "spring:cloud:discovery:registrationMethod";

        internal const string EUREKA_URI_SUFFIX = "/eureka/";

        internal const string ROUTE_REGISTRATIONMETHOD = "route";
        internal const string DIRECT_REGISTRATIONMETHOD = "direct";
        internal const string HOST_REGISTRATIONMETHOD = "hostname";

        internal const string CF_APP_GUID = "cfAppGuid";
        internal const string CF_INSTANCE_INDEX = "cfInstanceIndex";
        internal const string SURGICAL_ROUTING_HEADER = "X-CF-APP-INSTANCE";
        internal const string INSTANCE_ID = "instanceId";
        internal const string ZONE = "zone";
        internal const string UNKNOWN_ZONE = "unknown";
        internal const int DEFAULT_NONSECUREPORT = 80;
        internal const int DEFAULT_SECUREPORT = 443;

        public static void UpdateConfiguration(IConfiguration config, EurekaServiceInfo si, EurekaClientOptions clientOptions)
        {
            if (clientOptions == null || si == null)
            {
                return;
            }

            var uri = si.Uri;

            if (!uri.EndsWith(EUREKA_URI_SUFFIX))
            {
                uri += EUREKA_URI_SUFFIX;
            }

            clientOptions.EurekaServerServiceUrls = uri;
            clientOptions.AccessTokenUri = si.TokenUri;
            clientOptions.ClientId = si.ClientId;
            clientOptions.ClientSecret = si.ClientSecret;
        }

        public static void UpdateConfiguration(IConfiguration config, EurekaInstanceOptions options, IApplicationInstanceInfo instanceInfo)
        {
            var defaultId = options.GetHostName(false) + ":" + EurekaInstanceOptions.Default_Appname + ":" + EurekaInstanceOptions.Default_NonSecurePort;

            if (EurekaInstanceOptions.Default_Appname.Equals(options.AppName))
            {
                var springAppName = instanceInfo?.ApplicationNameInContext(SteeltoeComponent.Discovery);

                // this is a bit of a hack, but depending on how we got here, ApplicationNameInContext may or may not know about VCAP
                if (Platform.IsCloudFoundry && springAppName == System.Reflection.Assembly.GetEntryAssembly().GetName().Name && !string.IsNullOrEmpty(instanceInfo?.ApplicationName))
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
                var springRegMethod = config.GetValue<string>(SPRING_CLOUD_DISCOVERY_REGISTRATIONMETHOD_KEY);
                if (!string.IsNullOrEmpty(springRegMethod))
                {
                    options.RegistrationMethod = springRegMethod;
                }
            }

            // try to pull some values out of server config to override defaults, but only if registration method hasn't been set
            // if registration method has been set, the user probably wants to define their own behavior
            var urls = config["urls"];
            if (!string.IsNullOrEmpty(urls) && string.IsNullOrEmpty(options.RegistrationMethod))
            {
                Console.WriteLine(urls);
                var addresses = urls.Split(";");
                foreach (var address in addresses)
                {
                    SetOptionsFromUrls(options, new Uri(address));
                }
            }

            if (defaultId.Equals(options.InstanceId))
            {
                var springInstanceId = instanceInfo?.InstanceId;
                if (!string.IsNullOrEmpty(springInstanceId))
                {
                    options.InstanceId = springInstanceId;
                }
                else
                {
                    if (options.SecurePortEnabled)
                    {
                        options.InstanceId = options.GetHostName(false) + ":" + options.AppName + ":" + options.SecurePort;
                    }
                    else
                    {
                        options.InstanceId = options.GetHostName(false) + ":" + options.AppName + ":" + options.NonSecurePort;
                    }
                }
            }
        }

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

            if (EurekaInstanceOptions.Default_Appname.Equals(instOptions.AppName))
            {
                instOptions.AppName = si.ApplicationInfo.ApplicationName;
            }

            if (string.IsNullOrEmpty(instOptions.RegistrationMethod) ||
                ROUTE_REGISTRATIONMETHOD.Equals(instOptions.RegistrationMethod, StringComparison.OrdinalIgnoreCase))
            {
                UpdateWithDefaultsForRoute(si, instOptions);
                return;
            }

            if (DIRECT_REGISTRATIONMETHOD.Equals(instOptions.RegistrationMethod, StringComparison.OrdinalIgnoreCase))
            {
                UpdateWithDefaultsForDirect(si, instOptions);
                return;
            }

            if (HOST_REGISTRATIONMETHOD.Equals(instOptions.RegistrationMethod, StringComparison.OrdinalIgnoreCase))
            {
                UpdateWithDefaultsForHost(si, instOptions, instOptions.HostName);
            }
        }

        private static void SetOptionsFromUrls(EurekaInstanceOptions options, Uri uri)
        {
            if (uri.Scheme == "http")
            {
                if (options.Port == DEFAULT_NONSECUREPORT && uri.Port != DEFAULT_NONSECUREPORT)
                {
                    options.Port = uri.Port;
                    options.HostName = uri.Host;
                }
            }
            else if (uri.Scheme == "https" && options.SecurePort == DEFAULT_SECUREPORT && uri.Port != DEFAULT_SECUREPORT)
            {
                options.SecurePort = uri.Port;
                options.HostName = uri.Host;
            }
        }

        private static void UpdateWithDefaultsForHost(EurekaServiceInfo si, EurekaInstanceOptions instOptions, string hostName)
        {
            UpdateWithDefaults(si, instOptions);
            instOptions.HostName = hostName;
            instOptions.InstanceId = hostName + ":" + si.ApplicationInfo.InstanceId;
        }

        private static void UpdateWithDefaultsForDirect(EurekaServiceInfo si, EurekaInstanceOptions instOptions)
        {
            UpdateWithDefaults(si, instOptions);
            instOptions.PreferIpAddress = true;
            if (int.TryParse(si.ApplicationInfo.Port, out var port))
            {
                instOptions.NonSecurePort = port;
                instOptions.SecurePort = port;
            }
            else
            {
                if (si.ApplicationInfo.Port.Contains(";"))
                {
                    var ports = si.ApplicationInfo.Port.Split(";");
                    instOptions.Port = int.Parse(ports[0]);
                    instOptions.SecurePort = int.Parse(ports[1]);
                }
            }

            instOptions.InstanceId = si.ApplicationInfo.InternalIP + ":" + si.ApplicationInfo.InstanceId;
        }

        private static void UpdateWithDefaultsForRoute(EurekaServiceInfo si, EurekaInstanceOptions instOptions)
        {
            UpdateWithDefaults(si, instOptions);
            instOptions.NonSecurePort = DEFAULT_NONSECUREPORT;
            instOptions.SecurePort = DEFAULT_SECUREPORT;

            if (si.ApplicationInfo.Uris.Any())
            {
                instOptions.InstanceId = si.ApplicationInfo.Uris.First() + ":" + si.ApplicationInfo.InstanceId;
            }
        }

        private static void UpdateWithDefaults(EurekaServiceInfo si, EurekaInstanceOptions instOptions)
        {
            if (si.ApplicationInfo.Uris != null && si.ApplicationInfo.Uris.Any())
            {
                instOptions.HostName = si.ApplicationInfo.Uris.First();
            }

            instOptions.IpAddress = si.ApplicationInfo.InternalIP;

            var map = instOptions.MetadataMap;
            map[CF_APP_GUID] = si.ApplicationInfo.ApplicationId;
            map[CF_INSTANCE_INDEX] = si.ApplicationInfo.InstanceIndex.ToString();
            map[INSTANCE_ID] = si.ApplicationInfo.InstanceId;
            map[ZONE] = UNKNOWN_ZONE;
        }
    }
}
