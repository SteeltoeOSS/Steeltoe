//
// Copyright 2015 the original author or authors.
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
//

using Microsoft.Extensions.Logging;
using Steeltoe.Discovery.Eureka.Transport;
using System;

namespace Steeltoe.Discovery.Eureka
{
    public class DiscoveryManager
    {
        private DiscoveryManager() { }
        private static readonly DiscoveryManager _instance = new DiscoveryManager();
        public static DiscoveryManager Instance
        {
            get
            {
                return _instance;
            }
        }
        public DiscoveryClient Client { get; internal set; }
        public IEurekaClientConfig ClientConfig { get; internal set; }
        public IEurekaInstanceConfig InstanceConfig { get; internal set; }
        public ILookupService LookupService
        {
            get
            {
                return Client;
            }
        }

        private ILogger _logger;

        public void Initialize(IEurekaClientConfig clientConfig, ILoggerFactory logFactory = null)
        {
            Initialize(clientConfig, (IEurekaHttpClient)null, logFactory);
        }

        public void Initialize(IEurekaClientConfig clientConfig, IEurekaInstanceConfig instanceConfig, ILoggerFactory logFactory = null)
        {
            Initialize(clientConfig, instanceConfig, null, logFactory);

        }

        public void Initialize(IEurekaClientConfig clientConfig, IEurekaHttpClient httpClient, ILoggerFactory logFactory = null)
        {
            if (clientConfig == null)
            {
                throw new ArgumentNullException(nameof(clientConfig));
            }
            _logger = logFactory?.CreateLogger<DiscoveryManager>();
            ClientConfig = clientConfig;
            Client = new DiscoveryClient(clientConfig, httpClient, logFactory);
        }

        public void Initialize(IEurekaClientConfig clientConfig, IEurekaInstanceConfig instanceConfig, IEurekaHttpClient httpClient, ILoggerFactory logFactory = null)
        {

            if (clientConfig == null)
            {
                throw new ArgumentNullException(nameof(clientConfig));
            }

            if (instanceConfig == null)
            {
                throw new ArgumentNullException(nameof(instanceConfig));
            }

            _logger = logFactory?.CreateLogger<DiscoveryManager>();
            ClientConfig = clientConfig;
            InstanceConfig = instanceConfig;

            if (ApplicationInfoManager.Instance.InstanceInfo == null)
            {
                ApplicationInfoManager.Instance.Initialize(instanceConfig, logFactory);
            }

            Client = new DiscoveryClient(clientConfig, httpClient, logFactory);
        }


    }
}
