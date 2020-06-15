// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using System;
using System.Collections.Generic;
using T = System.Threading.Tasks;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaDiscoveryClient : DiscoveryClient, IDiscoveryClient
    {
        private class EurekaHttpClientInternal : EurekaHttpClient
        {
            private IOptionsMonitor<EurekaClientOptions> _configOptions;

            protected override IEurekaClientConfig Config
            {
                get
                {
                    return _configOptions.CurrentValue;
                }
            }

            public EurekaHttpClientInternal(IOptionsMonitor<EurekaClientOptions> config, ILoggerFactory logFactory = null, IHttpClientHandlerProvider handlerProvider = null)
            {
                _config = null;
                _configOptions = config ?? throw new ArgumentNullException(nameof(config));
                _handlerProvider = handlerProvider;
                Initialize(new Dictionary<string, string>(), logFactory);
            }
        }

        private IOptionsMonitor<EurekaClientOptions> _configOptions;
        private IServiceInstance _thisInstance;

        public override IEurekaClientConfig ClientConfig
        {
            get
            {
                return _configOptions.CurrentValue;
            }
        }

        public EurekaDiscoveryClient(
            IOptionsMonitor<EurekaClientOptions> clientConfig,
            IOptionsMonitor<EurekaInstanceOptions> instConfig,
            EurekaApplicationInfoManager appInfoManager,
            IEurekaHttpClient httpClient = null,
            ILoggerFactory logFactory = null,
            IHttpClientHandlerProvider handlerProvider = null)
            : base(appInfoManager, logFactory)
        {
            _thisInstance = new ThisServiceInstance(instConfig);
            _configOptions = clientConfig;

            _httpClient = httpClient;
            if (_httpClient == null)
            {
                _httpClient = new EurekaHttpClientInternal(clientConfig, logFactory, handlerProvider);
            }

            Initialize();
        }

        public IList<string> Services
        {
            get
            {
                return GetServices();
            }
        }

        public string Description
        {
            get
            {
                return "Spring Cloud Eureka Client";
            }
        }

        public IList<string> GetServices()
        {
            Applications applications = Applications;
            if (applications == null)
            {
                return new List<string>();
            }

            IList<Application> registered = applications.GetRegisteredApplications();
            List<string> names = new List<string>();
            foreach (Application app in registered)
            {
                if (app.Instances.Count == 0)
                {
                    continue;
                }

                names.Add(app.Name.ToLowerInvariant());
            }

            return names;
        }

        public IList<IServiceInstance> GetInstances(string serviceId)
        {
            IList<InstanceInfo> infos = GetInstancesByVipAddress(serviceId, false);
            List<IServiceInstance> instances = new List<IServiceInstance>();
            foreach (InstanceInfo info in infos)
            {
                _logger?.LogDebug($"GetInstances returning: {info}");
                instances.Add(new EurekaServiceInstance(info));
            }

            return instances;
        }

        public IServiceInstance GetLocalServiceInstance()
        {
            return _thisInstance;
        }

        public override T.Task ShutdownAsync()
        {
            _appInfoManager.InstanceStatus = InstanceStatus.DOWN;
            return base.ShutdownAsync();
        }
    }
}
