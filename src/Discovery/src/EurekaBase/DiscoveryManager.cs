﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Discovery.Eureka.Transport;
using System;

namespace Steeltoe.Discovery.Eureka
{
    public class DiscoveryManager
    {
        protected DiscoveryManager()
        {
        }

        protected static readonly DiscoveryManager _instance = new DiscoveryManager();

        public static DiscoveryManager Instance
        {
            get
            {
                return _instance;
            }
        }

        public virtual DiscoveryClient Client { get; protected internal set; }

        public virtual IEurekaClientConfig ClientConfig { get; protected internal set; }

        public virtual IEurekaInstanceConfig InstanceConfig { get; protected internal set; }

        public virtual ILookupService LookupService
        {
            get
            {
                return Client;
            }
        }

        protected ILogger _logger;

        public virtual void Initialize(IEurekaClientConfig clientConfig, ILoggerFactory logFactory = null)
        {
            Initialize(clientConfig, (IEurekaHttpClient)null, logFactory);
        }

        public virtual void Initialize(IEurekaClientConfig clientConfig, IEurekaInstanceConfig instanceConfig, ILoggerFactory logFactory = null)
        {
            Initialize(clientConfig, instanceConfig, null, logFactory);
        }

        public virtual void Initialize(IEurekaClientConfig clientConfig, IEurekaHttpClient httpClient, ILoggerFactory logFactory = null)
        {
            _logger = logFactory?.CreateLogger<DiscoveryManager>();
            ClientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
            Client = new DiscoveryClient(clientConfig, httpClient, logFactory);
        }

        public virtual void Initialize(IEurekaClientConfig clientConfig, IEurekaInstanceConfig instanceConfig, IEurekaHttpClient httpClient, ILoggerFactory logFactory = null)
        {
            _logger = logFactory?.CreateLogger<DiscoveryManager>();
            ClientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
            InstanceConfig = instanceConfig ?? throw new ArgumentNullException(nameof(instanceConfig));

            if (ApplicationInfoManager.Instance.InstanceInfo == null)
            {
                ApplicationInfoManager.Instance.Initialize(instanceConfig, logFactory);
            }

            Client = new DiscoveryClient(clientConfig, httpClient, logFactory);
        }
    }
}
