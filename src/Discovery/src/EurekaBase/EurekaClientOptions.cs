﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaClientOptions : EurekaClientConfig, IDiscoveryClientOptions
    {
        public const string EUREKA_CLIENT_CONFIGURATION_PREFIX = "eureka:client";

        public new const int Default_InstanceInfoReplicationIntervalSeconds = 30;

        public EurekaClientOptions()
        {
            EurekaServer = new EurekaServerConfig(this);
            Health = new EurekaHealthConfig(this);
        }

        // Configuration property: eureka:client:accessTokenUri
        public string AccessTokenUri { get; set; }

        // Configuration property: eureka:client:clientSecret
        public string ClientSecret { get; set; }

        // Configuration property: eureka:client:clientId
        public string ClientId { get; set; }

        // Configuration property: eureka:client:serviceUrl
        public string ServiceUrl
        {
            get => EurekaServerServiceUrls;

            set => EurekaServerServiceUrls = value;
        }

        // Configuration property: eureka:client:validate_certificates
        public bool Validate_Certificates
        {
            get => ValidateCertificates;

            set => ValidateCertificates = value;
        }

        // Configuration property: eureka:client:eurekaServer
        public EurekaServerConfig EurekaServer { get; set; }

        // Configuration property: eureka:client:health
        public EurekaHealthConfig Health { get; set; }

        public class EurekaHealthConfig
        {
            private EurekaClientOptions _options;

            public EurekaHealthConfig(EurekaClientOptions options)
            {
                _options = options;
            }

            // Configuration property: eureka:client:health:enabled
            public bool Enabled
            {
                get => _options.HealthContribEnabled;

                set => _options.HealthContribEnabled = value;
            }

            // Configuration property: eureka:client:health:monitoredApps
            public string MonitoredApps
            {
                get => _options.HealthMonitoredApps;

                set => _options.HealthMonitoredApps = value;
            }

            // Configuration property: eureka:client:health:checkEnabled
            public bool CheckEnabled
            {
                get => _options.HealthCheckEnabled;

                set => _options.HealthCheckEnabled = value;
            }
        }

        public class EurekaServerConfig
        {
            private EurekaClientOptions _options;

            public EurekaServerConfig(EurekaClientOptions options)
            {
                _options = options;
            }

            /// <summary>
            /// Gets or sets configuration property: eureka:client:eurekaServer:proxyHost
            /// </summary>
            public string ProxyHost
            {
                get => _options.ProxyHost;

                set => _options.ProxyHost = value;
            }

            /// <summary>
            /// Gets or sets configuration property: eureka:client:eurekaServer:proxyPort
            /// </summary>
            public int ProxyPort
            {
                get => _options.ProxyPort;

                set => _options.ProxyPort = value;
            }

            /// <summary>
            ///  Gets or sets configuration property: eureka:client:eurekaServer:proxyUserName
            /// </summary>
            public string ProxyUserName
            {
                get => _options.ProxyUserName;

                set => _options.ProxyUserName = value;
            }

            /// <summary>
            ///  Gets or sets configuration property: eureka:client:eurekaServer:proxyPassword
            /// </summary>
            public string ProxyPassword
            {
                get => _options.ProxyPassword;

                set => _options.ProxyPassword = value;
            }

            /// <summary>
            ///  Gets or sets a value indicating whether configuration property: eureka:client:eurekaServer:shouldGZipContent
            /// </summary>
            public bool ShouldGZipContent
            {
                get => _options.ShouldGZipContent;

                set => _options.ShouldGZipContent = value;
            }

            /// <summary>
            ///  Gets or sets configuration property: eureka:client:eurekaServer:connectTimeoutSeconds
            /// </summary>
            public int ConnectTimeoutSeconds
            {
                get => _options.EurekaServerConnectTimeoutSeconds;

                set => _options.EurekaServerConnectTimeoutSeconds = value;
            }

            /// <summary>
            /// Gets or sets configuration property: eureka:client:eurekaServer:retryCount
            /// </summary>
            public int RetryCount
            {
                get => _options.EurekaServerRetryCount;

                set => _options.EurekaServerRetryCount = value;
            }
        }
    }
}
