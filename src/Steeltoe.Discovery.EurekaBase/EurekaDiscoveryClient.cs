// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using System;
using System.Collections.Generic;
using T=System.Threading.Tasks;

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

            public EurekaHttpClientInternal(IOptionsMonitor<EurekaClientOptions> config, ILoggerFactory logFactory = null, IEurekaDiscoveryClientHandlerProvider handlerProvider = null)
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
            IEurekaDiscoveryClientHandlerProvider handlerProvider = null)
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
